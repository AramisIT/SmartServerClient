﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using SmartServerClient.Properties;
using System.Threading;
using SmartServerClient;

namespace SmartServerClient.Connection
    {
    public class GSMTerminalAgent
        {
        SerialPort ComPort;
        public string ErrorMessage
            {
            get;
            private set;
            }

        public GSMTerminalAgent()
            {
            ComPort = new SerialPort(String.Format("COM{0}", Settings.Default.ComPortNumber));
            ComPort.BaudRate = 9600; // Bits per second
            ComPort.DataBits = 8;
            ComPort.Parity = Parity.None;
            ComPort.StopBits = StopBits.One;

            ComPort.ReadTimeout = 300;

            ComPort.Handshake = Handshake.None;
            }

        public bool Autorize()
            {
            string answer = SendCommandAndReceiveAnswer("+CPIN?");
            if ( IsError(answer) )
                {
                return false;
                }
            if ( answer.ToLower().IndexOf("ready") == -1 )
                {
                answer = SendCommandAndReceiveAnswer("+CPIN", Settings.Default.PinCode);
                if ( IsError(answer) )
                    {
                    return false;
                    }
                }
            return true;
            }

        public bool SendSMS(string recepientNumber, string message)
            {
            bool result;
            try
                {
                ComPort.Open();
                if ( message.Length > 70 )
                    {
                    ErrorMessage = "Error: Длина сообщения больше 70 символов! Отправка не возможна!";

                    if ( Autorize() )
                        {
                        string answer = SendCommandAndReceiveAnswer("+cmgf", 0);
                        if ( !IsError(answer) )
                            {

                            string mess = GetPDU(recepientNumber, message);
                            string len = ( mess.Length / 2 - 1 ).ToString();

                            answer = SendCommandAndReceiveAnswer("+cmgs", len);
                            if ( !IsError(answer) )
                                {
                                result = WriteMessage(mess);
                                }
                            }
                        }
                    
                    //answer = SendCommandAndReceiveAnswer("+cmgw", len);
                    //if ( IsError(answer) )
                    //    {
                    //    ComPort.Close();
                    //    return false;
                    //    }
                    //int messageIndex = WriteMessage(mess);
                    //if ( messageIndex != -1 )
                    //    {
                    //    answer = SendCommandAndReceiveAnswer("+cmss", messageIndex);
                    //    if ( !IsError(answer) )
                    //        {
                    //        DeleteMessages();
                    //        ComPort.Close();
                    //        return true;
                    //        }
                    //    }
                    //answer = SendCommandAndReceiveAnswer();
                    }
                ComPort.Close();
                return false;
                }
            catch ( Exception exp )
                {
                if ( ComPort.IsOpen )
                    {
                    ComPort.Close();
                    }
                ErrorMessage = "Error: " + exp.Message;
                return false;
                }
            }

        private string GetPDU(string recepientNumber, string message)
            {
            string mess = cp1251_2ucs2(message);
            StringBuilder ret = new StringBuilder("00");//it is only an indicator of the length of the SMSC information supplied (0)
            ret.Append("11"); //First octet of the SMS-SUBMIT message.
            ret.Append("00"); //TP-Message-Reference. The "00" value here lets the phone set the message reference number itself.
            ret.Append("0C"); // Address-Length. Length of phone number (12)
            ret.Append("91"); //Type-of-Address. (91 indicates international format of the phone number).
            // Начало кодирования номера мобильного
            if ( recepientNumber.Length % 2 == 1 )
                {
                recepientNumber += "F";
                }
            for ( int i = 0; i < recepientNumber.Length; i += 2 )
                {
                ret.AppendFormat("{0}{1}", recepientNumber[i + 1], recepientNumber[i]);
                }
            // Закончили взрывать мозг  
            ret.Append("00"); //TP-PID. Protocol identifier
            ret.Append("08"); //TP-DCS. Data coding scheme. 18 - don't save at history, 08 - save
            ret.Append("C1"); //TP-Validity-Period. C1 means 1 week
            ret.AppendFormat("{0:X2}", mess.Length / 2); //TP-User-Data-Length. Length of message.
            ret.Append(mess); //TP-User-Data ret +=chr(26); //end of TP-User-Data                     
            return ret.ToString();
            }

        private string cp1251_2ucs2(string str)
            {
            string results;
            StringBuilder ucs2 = new StringBuilder();
            for ( int i = 0; i < str.Length; i++ )
                {
                byte asciiCode = ord(str[i]);
                if ( asciiCode < 127 )
                    {
                    results = asciiCode.ToString("X4");
                    }
                else if ( asciiCode == 184 )
                    { //ё
                    results = "0451";
                    }
                else if ( asciiCode == 168 )
                    { //Ё
                    results = "0401";
                    }
                else
                    {
                    results = ( asciiCode - 192 + 1040 ).ToString("X4");
                    }
                ucs2.Append(results);
                }
            return ucs2.ToString();
            }

        private byte ord(char symbol)
            {
            return Encoding.GetEncoding(1251).GetBytes(new char[] { symbol })[0];
            }

        private bool DeleteMessages()
            {
            string answer = SendCommandAndReceiveAnswer("+cmgd", 1, 3);
            if ( IsError(answer) )
                {
                ErrorMessage = answer;
                return false;
                }
            return true;
            }

        /// <summary>
        /// Пишет в ком порт комманду и получает ответ. 
        /// Если при отправке запроса произошла ошибка - строка будет начинаться с "Error: " после чего будет следовать текст исключения 
        /// </summary>
        /// <param name="command">Комманда без символов AT (тоесть для комманды AT+CPIN => +CPIN; AT+CPIN? => +CPIN?)</param>
        /// <param name="parameters">Параметры в очередности их следования (Например для AT+CMGF=0 => 0; AT+CPIN=? => ?)</param>
        /// <returns></returns>
        private string SendCommandAndReceiveAnswer(string command, params object[] parameters)
            {
            string result = "";
            try
                {
                StringBuilder query = new StringBuilder(String.Format("AT{0}", command));
                if ( parameters.Length > 0 )
                    {
                    query.AppendFormat("={0}", parameters[0]);
                    for ( int i = 1; i < parameters.Length; i++ )
                        {
                        query.AppendFormat(",{0}", parameters[i]);
                        }
                    }
                query.Append("\r");
                ComPort.Write(query.ToString());
                
                while ( result == "" || ComPort.BytesToRead > 0)
                    {
                    Thread.Sleep(1000);
                    //char[] buff = new char[ ComPort.BytesToRead ];
                    //ComPort.Read(buff, 0, buff.Length);
                    //result += buff.ArrayToString();
                    result = ComPort.ReadExisting();
                    }
                }
            catch ( Exception exp )
                {
                result = String.Format("Error: {0}", exp.Message);
                }
            return result;
            }

        private bool WriteMessage(string message)
            {
            string result = null;
            try
                {
                ComPort.Write(String.Format("{0}{1}\r", message, ( char ) 26));
                while ( result == null || result == "" )
                    {
                    Thread.Sleep(100);
                    result = ComPort.ReadExisting();
                    }
                }
            catch ( Exception exp )
                {
                result = String.Format("Error: {0}", exp.Message);
                }

            if ( IsError(result) )
                {
                ErrorMessage = result;
                return false;
                }
            return true;
            }

        private bool IsError(string answer)
            {
            if ( answer.ToLower().IndexOf("error") != -1 )
                {
                ErrorMessage = answer;
                return true;
                }
            ErrorMessage = null;
            return false;
            }
        }
    }
