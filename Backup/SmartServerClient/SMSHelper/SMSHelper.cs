using System.Collections.Generic;
using System.Collections;
using System.Data;
using System;
using System.Collections.Concurrent;
using SmartServerClient.Properties;
using SmartServerClient.Connection;
namespace Aramis.SMSHelperNamespace
{
    public delegate void OnReceivingMessageDelegate(Message message);
    public delegate void OnSendingMessageDelegate(Message message, bool sendingResult, string errorDescription);

    public abstract class SMSHelper
    {
        public event OnReceivingMessageDelegate OnReceivingMessage;
        public event OnSendingMessageDelegate OnSendingMessage;
        public event SetConnectionStatusDelegate OnRefreshConnectionStatus;

        public static SMSHelper SmsHelper
            {
            get
                {
                if ( smsHelper == null )
                    {
                    smsHelper = GetSMSHelper();
                    }
                return smsHelper;
                }
            }
        private static SMSHelper smsHelper;

        private static SMSHelper GetSMSHelper(){
            Type type = Type.GetType(String.Format("{0}.{1}", Settings.Default.HelpersClassesNamespace, Settings.Default.BaseHelperClassName));
            object smsHelper = System.Activator.CreateInstance(type);
            return (SMSHelper)smsHelper;
            }

        public abstract Message GetSMS();
        public abstract bool SendMessage(Message message);

        protected void NotifyOnReceivingMessage(Message message)
            {
            if ( OnReceivingMessage != null )
                {
                OnReceivingMessage(message);
                }
            }

        protected void NotifyOnSendingMessage(Message message, bool sendingResult, string errorDescription = null)
            {
            if ( OnSendingMessage != null )
                {
                OnSendingMessage(message, sendingResult, errorDescription);
                }
            }

        public void NotifySetConnectionStatus(bool isOnline)
            {
            if ( OnRefreshConnectionStatus != null)
                {
                OnRefreshConnectionStatus(isOnline);
                }
            }

        public abstract void Close();
        //public DataTable ReceivedMessageTable
        //    {
        //    get;
        //    private set;
        //    }

        //public void AddReceivedMessage(DateTime date, string senderNo, string messageBody)
        //    {
        //        DataRow newRow = ReceivedMessageTable.NewRow();
        //        newRow["Date"] = date;
        //        newRow["SenderNumber"] = senderNo;
        //        newRow["Message"] = messageBody;
        //        ReceivedMessageTable.Rows.Add(newRow);
        //    }

        //public void  SendMessage(string recipientNumber, string message, long taskid = 0)
        //    {
        //    SendMessage(new Message(recipientNumber, message, taskid));
        //    }

        //public virtual void SendMessage(Message message)
        //    {
        //    if ( OnSendMessage != null )
        //        {
        //        OnSendMessage(message);
        //        }
        //    }

        ////public abstract decimal GetAccountBalance();

        //public void NotifySMSSended(bool result, long taskId)
        //    {
        //    if ( OnSMSSended != null )
        //        {
        //        OnSMSSended(result, taskId);
        //        }
        //    }

        //private SMSHelper()
        //    {
        //    ReceivedMessageTable = new DataTable();
        //    ReceivedMessageTable.Columns.Add("Date", typeof(DateTime));
        //    ReceivedMessageTable.Columns.Add("SenderNumber", typeof(string));
        //    ReceivedMessageTable.Columns.Add("Message", typeof(string));
        //    }

        //internal void NotifySMSSendingRepeat(int repeatCount)
        //    {
        //    if ( OnSMSSendingRepeate != null )
        //        {
        //        OnSMSSendingRepeate(repeatCount);
        //        }
        //    }
    }
}