using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.IO;
using StorekeeperManagementServer;
using System.Windows.Forms;
using WMS_client;

namespace SmartServerClient.Connection
    {
    public delegate void SetConnectionStatusDelegate(bool IsOnline);

    public class ServerAgent
        {
        #region Public fields

        public PackageViaWireless Package;
        public string WaitingPackageID = "";
        public bool RequestReady = false;
        public bool OnLine
            {
            get { return ConnectionEstablished; }
            }

        #endregion

        #region Private fields
        private const long SERVER_DOWN_TIME = 3000;
        // #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   # 
        private TcpClient TCPClient;
        private NetworkStream TCPStream;
        private bool ConnectionEstablished = false;
        private string IPAddress;
        private int PortNumber;
        private SmartServerClient Client;
        private bool PingSent = false;
        private int SendKeyCode;
        private string SendBarcode;
        public bool Executed = false;
        public event SetConnectionStatusDelegate OnRefreshConnectionStatus;
        private string PingValue;
        private Thread InformationThread;
        private long lastPackageResived = DateTime.Now.Ticks;
        public bool NeedAbortThread
            {
            get
                {
                lock ( this )
                    {
                    return needAbortThread;
                    }
                }
            private set
                {
                lock ( this )
                    {
                    needAbortThread = value;
                    }
                }
            }
        private bool needAbortThread;
        #endregion

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region Public methods

        public ServerAgent(string IPAddress, int PortNumber, SmartServerClient Client)
            {
            this.Client = Client;
            this.IPAddress = IPAddress;
            this.PortNumber = PortNumber;
            NeedAbortThread = false;
            }

        public void Start()
            {
            Console.WriteLine("Запуск агента");
            while ( !NeedAbortThread )
                {
                while ( !Connect() ) ;
                ReadPackages();
                }
            CloseAll();
            }

        public bool SendPackage(Byte[] Package)
            {
            lock ( this )
                {
                #region Sending first time
                bool repeat = false;
                try
                    {
                    TCPStream.Write(Package, 0, Package.Length);
                    Console.WriteLine("Запись пакета ");
                    return true;
                    }
                catch
                    {
                    repeat = Connect();
                    }
                #endregion

                #region Sending repeat if error occurred
                if ( repeat )
                    {
                    try
                        {
                        TCPStream.Write(Package, 0, Package.Length);
                        Console.WriteLine("Запись пакета ");
                        return true;
                        }
                    catch
                        {
                        Console.WriteLine(" Writing error [" + Encoding.GetEncoding(1251).GetString(Package, 0, Package.Length) + "]");
                        //Console.WriteLine("Can't send data: " + exp.Message);
                        }
                    }
                #endregion
                return false;
                }
            }

        public void CloseAll()
            {

            try { TCPStream.Close(); }
            catch ( Exception exp )
                {
                exp.ToString();
                }

            try { TCPClient.Close(); }
            catch ( Exception exp )
                {
                exp.ToString();
                }

            }

        #endregion

        #region Private methods
        // #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   #   

        private bool Connect()
            {

            try
                {
                TCPClient = new TcpClient(IPAddress, PortNumber);
                }
            catch ( Exception exp )
                {
                Console.WriteLine("Can't connect: " + exp.Message);

                return false;
                }

            String ConnResult = "";

            try
                {
                TCPStream = TCPClient.GetStream();
                byte[] StreamTest = new byte[10];

                IAsyncResult NetStreamReadRes = TCPStream.BeginRead(StreamTest, 0, StreamTest.Length, null, null);

                if ( NetStreamReadRes.AsyncWaitHandle.WaitOne(3000, false) )
                    {
                    int streamLength = TCPStream.EndRead(NetStreamReadRes);
                    NetStreamReadRes = null;
                    ConnResult = Encoding.GetEncoding(1251).GetString(StreamTest, 0, streamLength);
                    }
                else
                    {
                    System.Diagnostics.Trace.WriteLine("Не получил ответа о сервере");
                    }
                }
            catch ( Exception exp )
                {
                Console.WriteLine("Can't create the network stream: " + exp.Message);
                return false;
                }
            if ( ConnResult != "$M$_$ERVER" ) return false;

            SetConnectionStatus(true);

            // Запуск пинга сервера 

            //PingAgent = new CallTimer(PingServer, 500);
            Console.WriteLine("Соединение установлено");
            return true;
            }

        private bool isPinging()
            {
            lock ( this )
                {
                return PingSent;
                }
            }

        private void PingSend(bool value)
            {
            lock ( this )
                {
                PingSent = value;
                }
            }

        private void ReadPackages()
            {
            #region Define local variables

            string StorekeeperQuery = "", StorekeeperQueryHead = "";
            lastPackageResived = DateTime.Now.Ticks;
            //Byte[] emptyData = System.Text.Encoding.GetEncoding(1251).GetBytes("");

            //int streamLength;

            #endregion

            while ( !NeedAbortThread )
                {
                #region Getting package

                if ( PackageViaWireless.isCompletelyPackage(StorekeeperQueryHead) )
                    {
                    StorekeeperQuery = StorekeeperQueryHead;
                    StorekeeperQueryHead = "";
                    }
                else
                    {
                    StorekeeperQuery = ReadStream();
                    }
                Console.WriteLine("Прочитано " + StorekeeperQuery);
                Package = null;
                if ( StorekeeperQuery == null )
                    {
                    return;
                    }

                StorekeeperQuery = StorekeeperQueryHead + StorekeeperQuery;

                if ( !PackageViaWireless.isCompletelyPackage(StorekeeperQuery) ) continue;

                Package = new PackageViaWireless(StorekeeperQuery, out StorekeeperQueryHead);

                StorekeeperQuery = "";

                #endregion

                switch ( Package.QueryName )
                    {
                    case "Ping":
                        Package.QueryName = "PingReply";
                        SendPackage(Package.GetPackage());
                        continue;

                    case "PingReply":
                        PingValue = ( PackageConvertation.GetPatametersFromStr(Package.Parameters)[0] as string );
                        continue;

                    case "Message":
                        object[] Parameters = PackageConvertation.GetPatametersFromStr(Package.Parameters);
                        System.Windows.Forms.MessageBox.Show(Parameters[0] as string);

                        // В параметры записывается только ID, текс сообщения уже не нужен
                        Package.Parameters = ( ( int ) Parameters[1] ).ToString();
                        SendPackage(Package.GetPackage());
                        continue;
                    }

                #region PackageHandling
                if ( Package.PackageID != WaitingPackageID ) continue;
                Executed = true;
                RequestReady = true;

                while ( RequestReady )
                    {
                    Thread.Sleep(100);
                    }
                #endregion
                }
            }

        private string ReadStream()
            {

            IAsyncResult NetStreamReadRes;
            Byte[] recivedData = new Byte[512];
            int streamLength = 0;
            string ResultString = "";
            StringBuilder SB = new StringBuilder();

            do
                {
                try
                    {
                    NetStreamReadRes = TCPStream.BeginRead(recivedData, 0, recivedData.Length, null, null);

                    while ( !NetStreamReadRes.AsyncWaitHandle.WaitOne(100, false) ) ;

                    streamLength = TCPStream.EndRead(NetStreamReadRes);
                    NetStreamReadRes = null;
                    ResultString = Encoding.GetEncoding(1251).GetString(recivedData, 0, streamLength);
                    SB.Append(ResultString);
                    }
                catch ( Exception exp )
                    {
                    Console.WriteLine(exp.Message);
                    SetConnectionStatus(false);
                    return null;
                    }
                } while ( streamLength == 512 && ResultString.Substring(507) != "#END>" );


            return SB.ToString();

            }

        private void SetConnectionStatus(bool IsOnline)
            {
            Console.WriteLine("Состояние " + IsOnline.ToString());
            ConnectionEstablished = IsOnline;
            }

        private void ShowingInformation()
            {
            InformationThread = new Thread(() =>
                {
                    bool? connectionStatus = null;
                    while ( !NeedAbortThread )
                        {
                        //if ( PingValue != null )
                        //    {
                        //    ShowPingResult(PingValue);
                        //    PingValue = null;
                        //    }
                        if ( OnRefreshConnectionStatus != null && connectionStatus != OnLine )
                            {
                            OnRefreshConnectionStatus(OnLine);
                            connectionStatus = OnLine;
                            }

                        Thread.Sleep(1000);
                        }
                });
            InformationThread.Name = "InformationThread";
            InformationThread.IsBackground = true;
            InformationThread.Start();
            }

        #endregion


        internal void Stop()
            {
                NeedAbortThread = true;
                CloseAll();
            }
        }
    }
