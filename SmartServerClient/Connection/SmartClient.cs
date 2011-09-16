using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Aramis.SMSHelper;
using System.Data.SqlClient;
using SmartServerClient.Properties;

namespace SmartServerClient.Connection
    {
    public class SmartClient
        {
        public const int SLEEP_BEFORE_CHECKING_AGAIN = 3000;
        public SetConnectionStatusDelegate OnSmartServerConnectionStatusChanged;
        public SetConnectionStatusDelegate OnGSMTerminalConnectionStatusChanged;
        public bool NeedAbortThread
            {
            get;
            private set;
            }

        private Thread CheckingTaskThread;

        public SmartClient()
            {
            CheckingTaskThread = new Thread(CheckingTasks);
            CheckingTaskThread.IsBackground = false;
            CheckingTaskThread.Name = "Поток обработки входящих и исходящих заданий";
            CheckingTaskThread.Start();
            }

        private void CheckingTasks()
            {
            while ( !NeedAbortThread )
                {
                Message message;
                do
                    {
                    message = SMSHelper.SmsHelper.GetSMS();
                    if ( message != null )
                        {
                        CreatePretaskBySMS(message);
                        }
                    }
                while ( message != null );

                do
                    {
                    message = GetMessageForSending();
                    if ( message != null )
                        {
                        if ( SMSHelper.SmsHelper.SendMessage(message) )
                            {
                            MarkTaskAsSended(message.TaskId);
                            }
                        }
                    } while ( message != null );

                Thread.Sleep(SLEEP_BEFORE_CHECKING_AGAIN);
                }
            SMSHelper.SmsHelper.Close();
            }

        private void MarkTaskAsSended(long taskId)
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

        private Message GetMessageForSending()
            {
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

                            if ( dataReader.Read() )
                                {
                                long sendingTaskId = ( long ) dataReader[ "TaskId" ];

                                return new Message(dataReader[ "MobilePhone" ].ToString(), ( dataReader[ "MessageText" ] as string ).Trim())
                                {
                                    TaskId = sendingTaskId
                                };
                                }
                            }
                        }
                    }
                }
            catch
                {
                }
            return null;
            }

        private bool CreatePretaskBySMS(Message message)
            {
            string returnValueParameterName = "@ReturnValueParameter";
            try
                {
                using ( SqlConnection conn = new SqlConnection(Settings.Default.ConnectionString) )
                    {
                    conn.Open();

                    using ( SqlCommand cmd = new SqlCommand("CreatePreTaskBySMS", conn) )
                        {
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@Text", message.MessageBody);
                        cmd.Parameters.AddWithValue("@PhoneNumber", message.Number);

                        SqlParameter param = new SqlParameter();
                        param.ParameterName = returnValueParameterName;
                        param.Direction = System.Data.ParameterDirection.ReturnValue;
                        cmd.Parameters.Add(param);

                        cmd.ExecuteScalar();
                        object returnValue = cmd.Parameters[ returnValueParameterName ].Value;
                        return true;
                        }
                    }
                }
            catch
                {
                return false;
                }
            }

        public void Stop()
            {
            NeedAbortThread = true;
            }
        }
    }
