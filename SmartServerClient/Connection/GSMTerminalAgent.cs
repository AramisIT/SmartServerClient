using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using SmartServerClient.Properties;
using System.Threading;
using SmartServerClient;
using Aramis.Enums;
using Aramis.SMSHelperNamespace;

namespace Aramis.Enums
    {
    public enum SMSStatus
        {
        Unread,
        Read,
        Unsent,
        Sent,
        All
        }
    }

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
            DeleteAllExceptUnreaded();
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
            answer = SendCommandAndReceiveAnswer("+cmgf", 0);
            if ( IsError(answer) )
                {
                return false;
                }
            return true;
            }

        public bool SendSMS(string recepientNumber, string message)
            {
            bool result;
            try
                {
                if ( message.Length > 70 )
                    {
                    ErrorMessage = "Error: Длина сообщения больше 70 символов! Отправка не возможна!";
                    }
                else
                    {
                    if ( Autorize() )
                        {
                        string mess = GetPDU(recepientNumber, message);
                        string len = ( mess.Length / 2 - 1 ).ToString();

                        string answer = SendCommandAndReceiveAnswer("+cmgs", len);
                        if ( !IsError(answer) )
                            {
                            result = WriteMessage(mess);
                            return true;
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
            string mess = Cp1251ToUcs2(message);
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

        private Message GetMessage(string pdu)
            {
            // http://dreamfabric.com/sms/
            int indexNextRead = 0;
            // Read SMSC information lenght (1 byte)
            int serviceNumberLenght = Convert.ToInt32(pdu.Substring(indexNextRead * 2, 2), 16);
            // 1 byte                       | Length of the SMSC information (in this case 7 octets)
            // 1 byte                       | Type-of-address of the SMSC. (91 means international format of the phone number)  http://dreamfabric.com/sms/type_of_address.html
            // serviceNumberLenght-1  bytes | Service center number(in decimal semi-octets). The length of the phone number is odd (11), so a trailing F has been added to form proper octets. The phone number of this service center is "+27381000015". See below.
            // 1 byte                       | First octet of this SMS-DELIVER message. http://dreamfabric.com/sms/deliver_fo.html
            indexNextRead = indexNextRead + 1 + serviceNumberLenght + 1;
            // Read sender number lenght (1 byte)
            int numberLenght = Convert.ToInt32(pdu.Substring(indexNextRead * 2, 2), 16);
            bool numberLenghtIsOdd = numberLenght % 2 == 0;
            // 1 byte                       | Address-Length. Length of the sender number (0B hex = 11 dec)
            // 1 byte                       | Type-of-address of the sender number. http://dreamfabric.com/sms/type_of_address.html
            indexNextRead = indexNextRead + 1 + 1;
            // Read sender's number (( numberLenght + 1 ) / 2 bytes)
            string codedNumber = pdu.Substring(indexNextRead * 2, numberLenghtIsOdd ? numberLenght : numberLenght + 1); //Номер всегда закодирован в четном количестве октетов. Обычно добивается до 12 строчных символов буквами F
            // Парсим номер
            StringBuilder number = new StringBuilder();
            for ( int i = 0; i < codedNumber.Length; i += 2 )
                {
                number.AppendFormat("{0}{1}", codedNumber[i + 1], codedNumber[i]);
                }
            // 1 byte                       | TP-PID. Protocol identifier. http://dreamfabric.com/sms/pid.html
            indexNextRead = indexNextRead + ( numberLenght + 1 ) / 2 + 1;
            // Read coding format : 0 - 7 bit; 8 - 16 bit; null - illegalFormat
            int codingFormat = Convert.ToInt32(pdu.Substring(indexNextRead * 2, 2), 16);
            switch ( codingFormat )
                {
                case 0:
                case 8:
                    break;
                default:
                    return null;
                }
            // 1 byte                       | TP-DCS Data coding scheme. http://dreamfabric.com/sms/dcs.html  
            indexNextRead = indexNextRead + 1;
            // Read message date
            string messageDateStr = pdu.Substring(indexNextRead * 2, 14);
            DateTime messageDate = new DateTime(Convert.ToInt32(String.Format("20{0}{1}", messageDateStr[1], messageDateStr[0]), 10),
                Convert.ToInt32(String.Format("{0}{1}", messageDateStr[3], messageDateStr[2]), 10),
                Convert.ToInt32(String.Format("{0}{1}", messageDateStr[5], messageDateStr[4]), 10),
                Convert.ToInt32(String.Format("{0}{1}", messageDateStr[7], messageDateStr[6]), 10),
                Convert.ToInt32(String.Format("{0}{1}", messageDateStr[9], messageDateStr[8]), 10),
                Convert.ToInt32(String.Format("{0}{1}", messageDateStr[11], messageDateStr[10]), 10));
            // 7 bytes                      | TP-SCTS. Time stamp (semi-octets). http://dreamfabric.com/sms/scts.html
            indexNextRead = indexNextRead + 7;
            // Read UserDataLenght (1 byte)
            int userDataLenght = Convert.ToInt32(pdu.Substring(indexNextRead * 2, 2), 16);
            // 1 byte                       | TP-UDL. User data length, length of message. The TP-DCS field indicated 7-bit data, so the length here is the number of septets (10). If the TP-DCS field were set to indicate 8-bit data or Unicode, the length would be the number of octets (9).
            indexNextRead = indexNextRead + 1;
            // read message PDU
            string message = pdu.Substring(indexNextRead * 2);
            return new Message(number.ToString().Substring(0, numberLenght), Ucs2ToCp1251(message, codingFormat, userDataLenght)) { Date = messageDate };
            }

        private string Cp1251ToUcs2(string str)
            {
            StringBuilder ucs2 = new StringBuilder();
            for ( int i = 0; i < str.Length; i++ )
                {
                ucs2.Append(Convert.ToInt32(str[i]).ToString("X4"));
                }
            return ucs2.ToString();
            }

        private string Ucs2ToCp1251(string str, int codingFormat, int lenght)
            {
            StringBuilder result = new StringBuilder();
            if ( codingFormat == 0 )
                {
                StringBuilder tempString = new StringBuilder();
                #region 7 bit coding
                for ( int i = 0; i < str.Length; i += 2 )
                    {
                    string code = Convert.ToInt32(Convert.ToString(Convert.ToByte(str.Substring(i, 2), 16), 2)).ToString("00000000");
                    tempString.Append(code.Invert());
                    }
                string codeStr = tempString.ToString();
                for ( int i = 0; i < lenght; i++ )
                    {
                    result.Append(Convert.ToChar(Convert.ToInt16(codeStr.Substring(i*7, 7).Invert(), 2)));
                    }
                #endregion
                }
            else if ( codingFormat == 8 )
                {
                #region 16 bit coding
                for ( int i = 0; i < str.Length; i += 4 )
                    {
                    string symbolCode = str.Substring(i, 4);
                    int code = Convert.ToInt32(symbolCode, 16);
                    result.Append(Convert.ToChar(code));
                    }
                #endregion
                }
            return result.ToString();
            }

        private byte ord(char symbol)
            {
            return Encoding.GetEncoding(1251).GetBytes(new char[] { symbol })[0];
            }

        //private char chr(int code)
        //    {
        //    return Encoding.GetEncoding(1251).Get(new byte[] { code })[0];
        //    }

        private bool DeleteMessage(int messageId)
            {
            try
                {
                string answer = SendCommandAndReceiveAnswer("+cmgd", messageId);
                if ( IsError(answer) )
                    {
                    ErrorMessage = answer;
                    return false;
                    }
                }
            catch ( Exception exp )
                {
                ErrorMessage = "Error: " + exp.Message;
                return false;
                }
            return true;
            }

        private bool DeleteAllExceptUnreaded()
            {
            List<int> messagesList = GetMessages(SMSStatus.Read);
            foreach ( int index in messagesList )
                {
                if ( !DeleteMessage(index) )
                    {
                    return false;
                    }
                }
            messagesList = GetMessages(SMSStatus.Sent);
            foreach ( int index in messagesList )
                {
                if ( !DeleteMessage(index) )
                    {
                    return false;
                    }
                }
            messagesList = GetMessages(SMSStatus.Unsent);
            foreach ( int index in messagesList )
                {
                if ( !DeleteMessage(index) )
                    {
                    return false;
                    }
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
                bool isPortOpened = ComPort.IsOpen;
                if ( !isPortOpened )
                    {
                    ComPort.Open();
                    }
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

                while ( result == "" || ( result.IndexOf("OK") == -1 && result.IndexOf("ERROR") == -1 && result.IndexOf(">") == -1 ) )
                    {
                    Thread.Sleep(1000);
                    //char[] buff = new char[ ComPort.BytesToRead ];
                    //ComPort.Read(buff, 0, buff.Length);
                    //result += buff.ArrayToString();
                    result += ComPort.ReadExisting();
                    }
                if ( !isPortOpened )
                    {
                    ComPort.Close();
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
                bool isPortOpened = ComPort.IsOpen;
                if ( !isPortOpened )
                    {
                    ComPort.Open();
                    }
                ComPort.Write(String.Format("{0}{1}\r", message, ( char ) 26));
                while ( result == null || result == "" )
                    {
                    Thread.Sleep(100);
                    result = ComPort.ReadExisting();
                    }
                if ( !isPortOpened )
                    {
                    ComPort.Close();
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

        private List<int> GetMessages(SMSStatus status)
            {
            List<int> result = new List<int>();
            try
                {
                string[] Messages;
                if ( Autorize() )
                    {
                    string answer = SendCommandAndReceiveAnswer("+cmgl", ( int ) status);
                    Messages = answer.Split(new string[] { "\r\n+CMGL: " }, StringSplitOptions.RemoveEmptyEntries);
                    for ( int i = 1; i < Messages.Length; i++ )
                        {
                        result.Add(Convert.ToInt32(Messages[i].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)[0]));
                        }
                    }
                }
            catch ( Exception exp )
                {
                ErrorMessage = "Error: " + exp.Message;
                }
            return result;
            }

        public Message GetSMS()
            {
            Message result = null;
            List<int> messages = GetMessages(SMSStatus.Unread);
            if ( messages.Count > 0 )
                {
                string answer = SendCommandAndReceiveAnswer("+cmgr", messages[0]);
                if ( IsError(answer) )
                    {
                    return result;
                    }
                string pdu = answer.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)[2];
                result = GetMessage(pdu);
                }
            return result;
            }
        }
    }
