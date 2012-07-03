using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Aramis.SMSHelperNamespace
{
    [Serializable]
    public class Message
    {
        public DateTime Date
        {
            get;
            set;
        }

        public string Number
        {
            get;
            set;
        }
        
        public string MessageBody
        {
            get;
            set;
        }

        public long TaskId
            {
            get;
            set;
            }

        public Message(string number, string messageBody, long taskId = 0)
        {
            Number = number;
            MessageBody = messageBody;
            this.TaskId = taskId;
        }
    }
}
