using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using StorekeeperManagementServer;
using System.IO;
using System.Data;
using SmartServerClient;
using SmartServerClient.Properties;
using WMS_client;
using Aramis.SMSHelperNamespace;

namespace SmartServerClient.Connection
    {

    #region Common NameSpace types

    public enum ControlsStyle
        {
        LabelNormal,
        LabelRedRightAllign,
        LabelLarge,
        LabelSmall,
        LabelH2,
        LabelH2Red,
        LabelMultiline,
        TextBoxNormal
        }

    public delegate void OnEventHandlingDelegate(object obj, EventArgs e);

    #endregion

    public class SmartServerClient
        {

        #region Public fields
        public static int SERVER_PORT = 8609;

        public bool OnLine
            {
            get { return ConnectionAgent.OnLine; }
            }

        public ServerAgent ConnectionAgent;
        public bool NeedAbortThread
            {
            get;
            set;
            }
        #endregion

        #region Private fields

        private Thread AgentThread;

        private string serverIP;
        public SetConnectionStatusDelegate RefreshConnectionStatus
            {
            get;
            set;
            }
        #endregion // Private fields

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region Public Methods

        #region Service methods

        public SmartServerClient()
            {
            #region Чтение IP-адреса сервера
            Settings settings = new Settings();
            serverIP = settings.ServerIPAddress;
            #endregion

            StartConnectionAgent();
            }

        private void StartConnectionAgent()
            {
            try
                {
                ConnectionAgent = new ServerAgent(serverIP, SERVER_PORT, this);
                ConnectionAgent.OnRefreshConnectionStatus += new SetConnectionStatusDelegate(ConnectionAgent_OnRefreshConnectionStatus);
                }
            catch ( Exception exp )
                {
                Console.WriteLine(string.Format("Ошибка создания агента сервера. Server IP = {0}\r\nОписание ошибки:\r\n{1}\r\nПриложение будет закрыто!", serverIP, exp.Message));
                Application.Exit();
                }

            AgentThread = new Thread(new ThreadStart(ConnectionAgent.Start));
            AgentThread.Name = "Server Agent";
            AgentThread.IsBackground = false;
            AgentThread.Start();
            }

        void ConnectionAgent_OnRefreshConnectionStatus(bool IsOnline)
            {
            if ( RefreshConnectionStatus != null && !NeedAbortThread)
                {
                RefreshConnectionStatus(IsOnline);
                }
            }

        private void RestartConnectionAgent()
            {
            AgentThread.Abort();
            StartConnectionAgent();
            }

        public object[] PerformQuery(string QueryName, params object[] Parameters)
            {

            PackageViaWireless Package = new PackageViaWireless(0, true);
            Package.DefineQueryAndParams(QueryName, PackageConvertation.GetStrPatametersFromArray(Parameters));
            ConnectionAgent.WaitingPackageID = Package.PackageID;
            ConnectionAgent.Executed = false;

            if ( ConnectionAgent.OnLine && ConnectionAgent.SendPackage(Package.GetPackage()) )
                {
                var startTime = DateTime.Now;
                while ( !ConnectionAgent.RequestReady && ConnectionAgent.OnLine )
                    {
                    Thread.Sleep(300);
                    DateTime currentTime = DateTime.Now;
                    double totalSec = ( ( TimeSpan ) ( currentTime.Subtract(startTime) ) ).TotalSeconds;

                    if ( totalSec > 60 )
                        {
                        RestartConnectionAgent();
                        return null;
                        }

                    }

                if ( ConnectionAgent.Package == null || !ConnectionAgent.Executed )
                    {

                    Console.WriteLine("Пропала связь с сервером!");
                    return null;
                    }
                if ( ConnectionAgent.Package.Parameters == "#ERROR:1C_AGENT_DISABLE#" )
                    {
                    ConnectionAgent.Package.Parameters = "";
                    ConnectionAgent.RequestReady = false;
                    return null;
                    }
                object[] result = PackageConvertation.GetPatametersFromStr(ConnectionAgent.Package.Parameters);

                ConnectionAgent.RequestReady = false;

                if ( result.Length == 0 )
                    {
                    return null;
                    }
                else
                    {
                    return result;
                    }
                }
            return null;

            }

        public void SendToServer(string QueryName, params object[] Parameters)
            {
            PackageViaWireless Package = new PackageViaWireless(0, true);
            Package.DefineQueryAndParams(QueryName, PackageConvertation.GetStrPatametersFromArray(Parameters));
            ConnectionAgent.WaitingPackageID = Package.PackageID;
            ConnectionAgent.SendPackage(Package.GetPackage());
            }

        #endregion
        #endregion

        internal void Stop()
            {
            ConnectionAgent.Stop();
            }
        }
    }
