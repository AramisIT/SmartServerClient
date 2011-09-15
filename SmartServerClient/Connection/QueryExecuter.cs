using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using StorekeeperManagementServer;
using System.Threading;
using System.Collections.Concurrent;
using WMS_client;
using Aramis.SMSHelper;

namespace SmartServerClient.Connection
    {
    public class QueryExecutor
        {
        private const int GET_MESSAGE_INTERVAL = 3000;
        private const int REPEATE_SENDING_ON_ERROR = 1;
        private ConcurrentQueue<KeyValuePair<string, object[]>> QueryQueue = new ConcurrentQueue<KeyValuePair<string, object[]>>();
        private List<long> SendedTasks = new List<long>();
        private Thread QueryExecuterThread;
        private Thread GetMessageThread;
        private Thread CheckSMSForSendingThread;
        private Connection.SmartServerClient Client;
        private MainForm mainForm;
        private CheckSMSForSending checkSMSForSending;

        public QueryExecutor(MainForm mainForm)
            {
            Client = new Connection.SmartServerClient();

            QueryExecuterThread = new Thread(Start);
            QueryExecuterThread.Name = "Query executer";
            QueryExecuterThread.IsBackground = true;
            QueryExecuterThread.Start();

            GetMessageThread = new Thread(AddGetSMSQueryToQueue);
            GetMessageThread.Name = "Add \"GetMessage\" query to queue";
            GetMessageThread.IsBackground = true;
            GetMessageThread.Start();

            checkSMSForSending = new CheckSMSForSending();
            CheckSMSForSendingThread = new Thread(checkSMSForSending.Start);
            CheckSMSForSendingThread.Name = "Check SMS For Sending Thread";
            CheckSMSForSendingThread.IsBackground = true;
            CheckSMSForSendingThread.Start();

            //SMSHelper.SmsHelper.OnSendMessage += new SendMessageDelegate(SMSHelper_OnSendMessage);
            }

        void SMSHelper_OnSendMessage(Message message)
            {
            if (( from x in QueryQueue.ToList() where x.Key == "SendSMS" && (long)x.Value[0] == message.TaskId select x).ToList().Count == 0)
                {
                Console.WriteLine("Добавлено в очередь на отправку (TaskId = {0})", message.TaskId);
                QueryQueue.Enqueue(new KeyValuePair<string, object[]>("SendSMS", new object[3] { message.TaskId, message.Number, message.MessageBody }));
                }
            }

        private void Start()
            {
            while ( true )
                {
                KeyValuePair<string, object[]> queryInfo;
                if ( QueryQueue.TryPeek(out queryInfo) )
                    {
                    object[] parameters;
                    switch ( queryInfo.Key )
                        {
                        case "GetSMS":
                            parameters = Client.PerformQuery(queryInfo.Key, queryInfo.Value);
                            if ( parameters != null && ( int ) parameters[0] != 0 )
                                {
                                //SMSHelper.SmsHelper.AddReceivedMessage(Convert.ToDateTime(parameters[1]), parameters[2] as string, parameters[3] as string);
                                }
                            QueryQueue.TryDequeue(out queryInfo);
                            continue;

                        case "SendSMS":
                            long taskId = ( long ) queryInfo.Value[0];
                            if ( !SendedTasks.Contains(taskId) )
                                {
                                int repeatCount = 0;
                                do
                                    {
                                    parameters = Client.PerformQuery(queryInfo.Key, queryInfo.Value);
                                    if ( parameters != null && ( bool ) parameters[0] )
                                        {
                                        repeatCount = -1;
                                        break;
                                        }
                                    else
                                        {
                                        repeatCount++;
                                        //SMSHelper.SmsHelper.NotifySMSSendingRepeat(repeatCount);
                                        }
                                    } while ( repeatCount >= 0 && repeatCount <= REPEATE_SENDING_ON_ERROR );
                                QueryQueue.TryDequeue(out queryInfo);
                                if ( repeatCount != -1 )
                                    {
                                    QueryQueue.Enqueue(queryInfo);
                                    }
                                else
                                    {
                                    SendedTasks.Add(taskId);
                                    }
                                //SMSHelper.SmsHelper.NotifySMSSended(repeatCount == -1, taskId);
                                }
                            continue;
                        }
                    }
                }
            }

        private void AddGetSMSQueryToQueue()
            {
            Thread.Sleep(5000);
            while ( true )
                {
                if ( ( from x in QueryQueue.ToList() where x.Key == "GetSMS" select x ).ToList().Count == 0 )
                    {
                    Console.WriteLine("Добавлено GetSMS в очередь");
                    QueryQueue.Enqueue(new KeyValuePair<string, object[]>("GetSMS", new object[0]));
                    Thread.Sleep(GET_MESSAGE_INTERVAL);
                    }
                }
            }

        public void Stop()
            {
            QueryExecuterThread.Abort();
            GetMessageThread.Abort();
            if ( Client != null )
                {
                Client.Stop();
                }
            }
        }
    }