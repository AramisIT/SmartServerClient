using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

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
        public MainForm MainForm
            {
            get;
            private set;
            }

        private Thread CheckingTaskThread;

        public Connection.SmartServerClient Client
            {
            get;
            private set;
            }

        //public SmartClient(MainForm mainForm)
        //    {
        //    M
        //    Client = new Connection.SmartServerClient();

        //    CheckingTaskThread = new Thread(CheckingTasks);
        //    CheckingTaskThread.IsBackground = false;
        //    CheckingTaskThread.Name = "Поток обработки входящих и исходящих заданий";
        //    CheckingTaskThread.Start();
        //    }

        private void CheckingTasks()
            {
            while ( !NeedAbortThread )
                {


                Thread.Sleep(SLEEP_BEFORE_CHECKING_AGAIN);
                }
            }

        public void Stop()
            {
            NeedAbortThread = true;
            Client.Stop();
            }
        }
    }
