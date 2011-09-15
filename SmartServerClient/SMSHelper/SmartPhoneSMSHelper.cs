
using System;
using System.Runtime.InteropServices;
using System.Data;
namespace Aramis.SMSHelper
{
    public class SmartPhoneSMSHelper : SMSHelper
        {
        private SmartServerClient.Connection.SmartServerClient Client = new SmartServerClient.Connection.SmartServerClient();

        public override Message GetSMS()
            {
            Message message = null;
            object[] parameters = Client.PerformQuery("GetSMS");
            if ( parameters != null && ( int ) parameters[0] != 0 )
                {
                message = new Message(parameters[2] as string, parameters[3] as string);
                NotifyOnReceivingMessage(message);
                }
            return message;
            }

        public override bool SendMessage(Message message)
            {
            object[] parameters = Client.PerformQuery("SendSMS", message.TaskId, message.Number, message.MessageBody);
            bool result = false;
            if ( parameters != null )
                {
                result = ( bool ) parameters[0];
                }
            NotifyOnSendingMessage(message, result);
            return result;            
            }

        //public class SmartPhoneSMSHelper : SMSHelper
        //{

        //    //[DllImport("cellcore.dll")]
        //    //private static extern int lineSendUSSD(
        //    //                                        IntPtr hLine,
        //    //                                        byte[] lpbUSSD,
        //    //                                        int dwUSSDSize,
        //    //                                        int dwFlags); 

        //    //public override decimal GetAccountBalance()
        //    //{
        //    //    //http://mobile-developer.ru/tapi/otpravka-ussd-zaprosa-v-windows-mobile/
        //    //    //http://www.4pda.ws/forum/index.php?showtopic=70067
        //    //    //http://social.msdn.microsoft.com/Forums/en-US/vssmartdevicesvbcs/thread/3194eb16-407e-44b8-bc1a-004e5be43f12
        //    //    return 0;
        //    //}
        //}
        }
}