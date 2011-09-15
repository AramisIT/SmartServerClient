using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using SmartServerClient.Properties;
using System.Threading;

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
            ComPort = new SerialPort(String.Format("COM{0}" + Settings.Default.ComPortNumber));
            ComPort.BaudRate = 9600; // Bits per second
            ComPort.Parity = Parity.None;
            ComPort.DataBits = 8;
            ComPort.ReadTimeout = 300;
            ComPort.StopBits = StopBits.One;
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
            if ( message.Length > 70 )
                {
                ErrorMessage = "Error: Длина сообщения больше 70 символов! Отправка не возможна!";
                return false;
                }

            if ( Autorize() )
                {
                string answer = SendCommandAndReceiveAnswer("+cmgf", 0);
                if ( IsError(answer) )
                    {
                    return false;
                    }

                string mess = GetPDU(recepientNumber, message);
                string len = (( mess.Length - 3 ) / 2).ToString();

                answer = SendCommandAndReceiveAnswer("+cmgw", len);
                if ( IsError(answer) )
                    {
                    return false;
                    }
                int messageIndex = WriteMessage(mess);
                
                //answer = SendCommandAndReceiveAnswer();
                }
            return false;
            }

        private string GetPDU(string recepientNumber, string message)
            {
            string mess = cp1251_2ucs2(message);
            string ret = "00";//it is only an indicator of the length of the SMSC information supplied (0)
            ret += "11"; //First octet of the SMS-SUBMIT message.
            ret += "00"; //TP-Message-Reference. The "00" value here lets the phone set the message reference number itself.
            ret += "0B"; // Address-Length. Length of phone number (11)
            ret += "91"; //Type-of-Address. (91 indicates international format of the phone number).
            // Начало кодирования номера мобильного
            if ( ( recepientNumber.Length / 2 ) % 2 == 1 )
                {
                recepientNumber += "F";
                }
            for ( int i = 0; i < recepientNumber.Length; i += 2 )
                {
                ret += recepientNumber[i + 1] + recepientNumber[i];
                }
            // Закончили взрывать мозг  
            ret += "00"; //TP-PID. Protocol identifier
            ret += "18"; //TP-DCS. Data coding scheme. 18 - don't save at history, 08 - save
            ret += "C1"; //TP-Validity-Period. C1 means 1 week
            ret += ( message.Length / 2 ).ToString("X2"); //TP-User-Data-Length. Length of message.
            ret += mess; //TP-User-Data ret +=chr(26); //end of TP-User-Data                     
            return ret;
            }

        private string cp1251_2ucs2(string str)
            {
            string results;
            string ucs2 = "";
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
                ucs2 += results;
                }
            return ucs2;
            }

        private byte ord(char symbol)
            {
            return Encoding.GetEncoding(1251).GetBytes(new char[] { symbol })[0];
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
            string result = null;
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
                ComPort.WriteLine(query.ToString());

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
            return result;
            }

        private int WriteMessage(string message)
            {
            string result = null;
            int index = -1;
            try
                {
                ComPort.WriteLine(String.Format("{0}{1}", message, ( char ) 26));
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
                }
            else
                {
                if ( result.ToLower().IndexOf("+cmgw:") != -1 )
                    {
                    ErrorMessage = "";
                    index = Int32.Parse(result.Substring(7));
                    }
                else
                    {
                    ErrorMessage = result;
                    }
                }
            return index;
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
