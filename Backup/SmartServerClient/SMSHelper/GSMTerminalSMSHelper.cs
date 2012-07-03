using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartServerClient.Connection;

namespace Aramis.SMSHelperNamespace
    {
    public class GSMTerminalSMSHelper : SMSHelper
        {
        public GSMTerminalAgent TerminalAgent = new GSMTerminalAgent();

        public override bool SendMessage(Message message)
            {
            bool result = TerminalAgent.SendSMS(message.Number, message.MessageBody);
            NotifyOnSendingMessage(message, result, TerminalAgent.ErrorMessage);
            return result;
            }

        public override Message GetSMS()
            {
            Message message = TerminalAgent.GetSMS();
            if ( message != null )
                {
                NotifyOnReceivingMessage(message);
                }
            return message;
            }

        public override void Close()
            {
            
            }
        }
    }
