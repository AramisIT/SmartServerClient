using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartServerClient.Connection;

namespace Aramis.SMSHelper
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
            return null;
            }

        public override void Close()
            {
            
            }
        }
    }
