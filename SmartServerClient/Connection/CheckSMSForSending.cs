using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using SmartServerClient.Properties;
using System.Threading;
using Aramis.SMSHelper;

namespace SmartServerClient.Connection
    {
    public class CheckSMSForSending
        {
        private  const int CHECKING_DELAY = 1000;

        public CheckSMSForSending()
            {
            //SMSHelper.SmsHelper.OnSMSSended += new SMSSendedDelegate(SmsHelper_OnSMSSended);
            }

        void SmsHelper_OnSMSSended(bool result, long taskId)
            {
            if ( result && taskId > 0 )
                {
                try
                    {
                    using ( var conn = new System.Data.SqlClient.SqlConnection(Settings.Default.ConnectionString) )
                        {
                        conn.Open();

                        using ( SqlCommand cmd = new SqlCommand("update top(1) [SMSJournal] set [Sended] = @sended where [Id] = @taskId", conn) )
                            {
                            cmd.Parameters.AddWithValue("@taskId", taskId);
                            cmd.Parameters.AddWithValue("@sended", true);
                            int changedRows = cmd.ExecuteNonQuery();
                            }
                        }
                    }
                catch
                    {
                    }
                }
            }

        public void Start()
            {
            while ( true )
                {
                List<Message> MessagesForSending = new List<Message>();
                try
                    {
                    using ( SqlConnection conn = new SqlConnection(Settings.Default.ConnectionString) )
                        {
                        conn.Open();

                        using ( SqlCommand cmd = new SqlCommand("select top 1 [Id] TaskId, [Description] MessageText, [MobilePhone] MobilePhone from [SMSJournal] where Sended = @NotSended order by CreationDate", conn) )
                            {
                            cmd.Parameters.AddWithValue("@NotSended", false);

                            using ( SqlDataReader dataReader = cmd.ExecuteReader() )
                                {
                                
                                while ( dataReader.Read() )
                                    {
                                    long sendingTaskId = ( long ) dataReader["TaskId"];
                                    
                                    MessagesForSending.Add(new Message("+" + dataReader["MobilePhone"].ToString(), ( dataReader["MessageText"] as string ).Trim()) { TaskId = sendingTaskId });                                   
                                    }
                                }
                            }
                        }
                    }
                catch
                    {
                    }

                MessagesForSending.ForEach(message => SMSHelper.SmsHelper.SendMessage(message));

                Thread.Sleep(CHECKING_DELAY);
                }
            }
        }
    }
