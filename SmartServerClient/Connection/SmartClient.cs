using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Aramis.SMSHelperNamespace;
using System.Data.SqlClient;
using SmartServerClient.Properties;
using Aramis.Enums;

namespace Aramis.Enums
    {
    public enum TestResults
        {
        Ok,
        Error,
        HardwareError,
        NotEnded
        }
    }

namespace SmartServerClient.Connection
    {
    public delegate void OnTestStartedDelegate();
    public delegate void OnRemouteSMSServiceStatusChangedDelegate(bool isOnline);
    public delegate void OnTestEndedDelegate(TestResults result);
    public delegate void OnErrorDelegate(string error);

    public class SmartClient
        {
        public event OnTestStartedDelegate OnTestStarted;
        public event OnTestEndedDelegate OnTestEnded;
        public event OnRemouteSMSServiceStatusChangedDelegate OnRemouteSMSServiceStatusChanged;
        public event OnErrorDelegate OnError;

        public const int SLEEP_BEFORE_CHECKING_AGAIN = 3000;
        public SetConnectionStatusDelegate OnSmartServerConnectionStatusChanged;
        public SetConnectionStatusDelegate OnGSMTerminalConnectionStatusChanged;
        private long lastChecked = 0;
        private bool testStarted = false;
        private long testId = 0;
        private bool remouteServiceIsOnline = true;
        public bool NeedAbortThread
            {
            get;
            private set;
            }
        private MessagesForWritingToDBList MessageList;

        private Thread CheckingTaskThread;

        public SmartClient()
            {
            MessageList = new MessagesForWritingToDBList();
            CheckingTaskThread = new Thread(CheckingTasks);
            CheckingTaskThread.IsBackground = false;
            CheckingTaskThread.Name = "Поток обработки входящих и исходящих заданий";
            CheckingTaskThread.Start();
            }

        private void CheckingTasks()
            {
            while ( !NeedAbortThread )
                {
                UpdateServiceStatus();

                Message message;
                int i = 0;
                while ( i < MessageList.MessageList.Count )
                    {
                    if ( CreatePretaskBySMS(MessageList.MessageList[i]) )
                        {
                        MessageList.MessageList.RemoveAt(i);
                        }
                    else
                        {
                        i++;
                        }
                    }

                do
                    {
                    message = SMSHelper.SmsHelper.GetSMS();
                    if ( message != null )
                        {
                        if ( message.Number != Settings.Default.RemoutePhoneNumber )
                            {
                            if ( !CreatePretaskBySMS(message) )
                                {
                                MessageList.MessageList.Add(message);
                                }
                            }
                        else
                            {
                            PerformTest();
                            }
                        }
                    }
                while ( message != null );

                bool ErrorWhileSending = false;
                do
                    {
                    message = GetMessageForSending();
                    if ( message != null )
                        {
                        ErrorWhileSending = !SMSHelper.SmsHelper.SendMessage(message);
                        if ( !ErrorWhileSending )
                            {
                            MarkTaskAsSended(message.TaskId);
                            }
                        }
                    } while ( message != null && !ErrorWhileSending);

                if ( new TimeSpan(DateTime.Now.Ticks - lastChecked).Hours >= Settings.Default.HoursBetweenDeliveryServiceTest && !testStarted && CheckRemouteSMSServiceStatus())
                    {                    
                    StartTest();
                    }

                if ( testStarted && new TimeSpan(DateTime.Now.Ticks - lastChecked).TotalSeconds > Settings.Default.DelayBeforeTestErrorCalled )
                    {
                    PerformTest(TestResults.Error);
                    SendMessageToAdministrator(String.Format("Ошибка теста. Cервис {0} не прислал СМС", Settings.Default.BaseHelperClassName));
                    }

                CheckRemouteSMSServiceStatus();

                Thread.Sleep(SLEEP_BEFORE_CHECKING_AGAIN);
                }
            MessageList.Serialize();
            SMSHelper.SmsHelper.Close();
            }

        private void UpdateServiceStatus()
            {
            try
                {
                using ( SqlConnection conn = new SqlConnection(Settings.Default.ConnectionString) )
                    {
                    conn.Open();
                    using ( SqlCommand cmd = conn.CreateCommand() )
                        {
                        cmd.CommandText = "UpdateSMSServiceStatus";
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PhoneNumber", Settings.Default.NativePhoneNumber);
                        cmd.ExecuteNonQuery();
                        }
                    }
                }
            catch ( Exception exp )
                {
                NotifyOnError(exp);
                }
            }

        private bool CheckRemouteSMSServiceStatus()
            {
            bool result = false;
            try
                {
                using ( SqlConnection conn = new SqlConnection(Settings.Default.ConnectionString) )
                    {
                    conn.Open();
                    using ( SqlCommand cmd = conn.CreateCommand() )
                        {
                        cmd.CommandText = "GetSMSServiceStatus";
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PhoneNumber", Settings.Default.RemoutePhoneNumber);
                        cmd.Parameters.AddWithValue("@OfflineStatusDelay", Settings.Default.DelayBeforeTestErrorCalled);
                        result = Convert.ToBoolean( cmd.ExecuteScalar());
                        if ( !result && remouteServiceIsOnline)
                            {
                            remouteServiceIsOnline = false;
                            SendMessageToAdministrator(String.Format("Удаленный сервис {0} Offline",
                                Settings.Default.BaseHelperClassName == "GSMTerminalSMSHelper" ? "SmartPhoneSMSHelper" : "GSMTerminalSMSHelper"));
                            if ( OnRemouteSMSServiceStatusChanged != null )
                                {
                                OnRemouteSMSServiceStatusChanged(false);
                                }
                            }
                        else if ( result && !remouteServiceIsOnline )
                            {
                            remouteServiceIsOnline = true;
                            if ( OnRemouteSMSServiceStatusChanged != null )
                                {
                                OnRemouteSMSServiceStatusChanged(true);
                                }
                            }
                        }
                    }
                }
            catch ( Exception exp )
                {
                NotifyOnError(exp);
                }
            return result;
            }

        private void SendMessageToAdministrator(string messageText)
            {
            Message message = new Message(Settings.Default.AdminPhoneNumber, messageText);
            try
                {
                using ( SqlConnection conn = new SqlConnection(Settings.Default.ConnectionString) )
                    {
                    conn.Open();
                    using ( SqlCommand cmd = conn.CreateCommand() )
                        {
                        cmd.CommandText = "select Count(*) from SMSServiceTroublesLog where cast([Date] as date) = @Date and Processed = 0 and ServiceNumber = @ServicePhoneNumber";
                        cmd.Parameters.AddWithValue("@Date", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@ServicePhoneNumber", Settings.Default.RemoutePhoneNumber);
                        object result = cmd.ExecuteScalar();
                        if ( ( int ) result == 0 && SMSHelper.SmsHelper.SendMessage(message))
                            {
                            cmd.CommandText = "AddSMSDeliveryServiceTroubleInformation";
                            cmd.Parameters.Clear();
                            cmd.CommandType = System.Data.CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@AdminPhoneNumber", Settings.Default.AdminPhoneNumber);
                            cmd.Parameters.AddWithValue("@ServicePhoneNumber", Settings.Default.RemoutePhoneNumber);
                            cmd.Parameters.AddWithValue("@ErrorData", messageText);
                            cmd.ExecuteNonQuery();
                            testStarted = false;
                            testId = 0;
                            }
                        }
                    }
                }
            catch ( Exception exp )
                {
                NotifyOnError(exp);
                }
            }

        private void StartTest()
            {
            try
                {
                using ( SqlConnection conn = new SqlConnection(Settings.Default.ConnectionString) )
                    {
                    conn.Open();
                    using ( SqlCommand cmd = conn.CreateCommand() )
                        {
                        cmd.CommandText = "StartNewSMSDeliveryServiceTest";
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@PhoneNumber", Settings.Default.RemoutePhoneNumber);
                        cmd.Parameters.AddWithValue("@NativePhoneNumber", Settings.Default.NativePhoneNumber);
                        object result = cmd.ExecuteScalar();
                        testId = Convert.ToInt64( result);
                        testStarted = true;
                        lastChecked = DateTime.Now.Ticks;
                        if ( OnTestStarted != null )
                            {
                            OnTestStarted();
                            }
                        }
                    }
                }
            catch ( Exception exp )
                {
                NotifyOnError(exp);
                }
            }

        private void PerformTest(TestResults result = TestResults.Ok)
            {
            if ( testStarted )
                {
                try
                    {
                    using ( SqlConnection conn = new SqlConnection(Settings.Default.ConnectionString) )
                        {
                        conn.Open();
                        using ( SqlCommand cmd = conn.CreateCommand() )
                            {
                            cmd.CommandText = "update SMSDeliverServiceTest set DateEnd = GETDATE(), Result = @Status where Id = @Id";
                            cmd.Parameters.AddWithValue("@Id", testId);
                            cmd.Parameters.AddWithValue("@Status", (int)result);
                            cmd.ExecuteNonQuery();
                            testId = 0;
                            testStarted = false;
                            lastChecked = DateTime.Now.Ticks;
                            if ( OnTestEnded != null )
                                {
                                OnTestEnded(result);
                                }
                            }
                        }
                    }
                catch ( Exception exp )
                    {
                    NotifyOnError(exp);
                    }
                }

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
            catch (Exception exp)
                {
                NotifyOnError(exp);
                }
            }

        private Message GetMessageForSending()
            {
            try
                {
                using ( SqlConnection conn = new SqlConnection(Settings.Default.ConnectionString) )
                    {
                    conn.Open();

                    using ( SqlCommand cmd = new SqlCommand("select top 1 [Id] TaskId, [Description] MessageText, [MobilePhone] MobilePhone from [SMSJournal] where Sended = @NotSended and DevicePhoneNumber = @DevicePhoneNumber order by CreationDate", conn) )
                        {
                        cmd.Parameters.AddWithValue("@NotSended", false);
                        cmd.Parameters.AddWithValue("@DevicePhoneNumber", Settings.Default.NativePhoneNumber);

                        using ( SqlDataReader dataReader = cmd.ExecuteReader() )
                            {

                            if ( dataReader.Read() )
                                {
                                long sendingTaskId = ( long ) dataReader["TaskId"];

                                return new Message(dataReader["MobilePhone"].ToString(), ( dataReader["MessageText"] as string ).Trim())
                                {
                                    TaskId = sendingTaskId
                                };
                                }
                            }
                        }
                    }
                }
            catch (Exception exp)
                {
                NotifyOnError(exp);
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
                        object returnValue = cmd.Parameters[returnValueParameterName].Value;
                        return true;
                        }
                    }
                }
            catch ( Exception exp )
                {
                NotifyOnError(exp);                
                }
            return false;
            }

        public void Stop()
            {
            NeedAbortThread = true;
            }

        public void NotifyOnError(Exception exp)
            {
            NotifyOnError(exp.ToString());
            }

        public void NotifyOnError(string exceptionMessage)
            {
            if ( OnError != null )
                {
                OnError(exceptionMessage.ToString());
                }
            }
        }
    }
