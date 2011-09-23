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
using System.Net.Sockets;

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
        private static int TIME_OUT = 30;
        public event OnErrorDelegate OnError;
        public bool NeedAbortThread
            {
            get;
            set;
            }
        #endregion

        #region Private fields
        private TcpClient client;
        private string serverIP;
        
        #endregion // Private fields

        // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        public SmartServerClient()
            {
            
            }

        internal object[] PerformQuery(string query, params object[] parameters)
            {
            PackageViaWireless package = new PackageViaWireless();
            try
                {
                client = new TcpClient(Settings.Default.ServerIPAddress, SERVER_PORT);
                NetworkStream stream = client.GetStream();

                if ( !Connected(stream))
                    {
                    return null;
                    }

                package.DefineQueryAndParams(query, PackageConvertation.GetStrPatametersFromArray(parameters));
                byte[] buffer = package.GetPackage();
                stream.Write(buffer, 0, buffer.Length);
                package = ReadData(stream);

                if ( package.QueryName == "GetSMS" )
                    {
                    PackageViaWireless okPackage = new PackageViaWireless();
                    okPackage.DefineQueryAndParams("OK", PackageConvertation.GetStrPatametersFromArray(new object[0]));
                    buffer = okPackage.GetPackage();
                    stream.Write(buffer, 0, buffer.Length);
                    }
                }
            catch (Exception exp)
                {
                if ( OnError != null )
                    {
                    OnError(exp.ToString());
                    }
                }
            finally
                {
                if ( client != null )
                    {
                    client.Close();
                    }
                }
            return package == null || package.Parameters == null ? null :PackageConvertation.GetPatametersFromStr(package.Parameters);
            }

        private bool Connected(NetworkStream stream)
            {
            byte[] buffer = new byte[10];
            stream.Read(buffer, 0, 10);
            string result = Encoding.GetEncoding(1251).GetString(buffer, 0, 10);
            if ( result == "$M$_$ERVER" )
                {
                return true;
                }
            return false;
            }

        private PackageViaWireless ReadData(NetworkStream stream)
            {
            StringBuilder data;
            byte[] buffer;
            string head;
            PackageViaWireless package = new PackageViaWireless();
            data = new StringBuilder();
            buffer = new byte[256];
            do
                {
                int streamLength = stream.Read(buffer, 0, buffer.Length);
                data.Append(Encoding.GetEncoding(1251).GetString(buffer, 0, streamLength));
                } while ( stream.DataAvailable );

            if ( !package.SetPackage(data.ToString(), out head) )
                {
                client.Close();
                package = null;
                }
            return package;
            }

        private bool DataAvaileble(NetworkStream stream)
            {
            long curreentTicks = DateTime.Now.Ticks;
            bool timeOut = false;
            while ( !stream.DataAvailable && !timeOut )
                {
                timeOut = TimeOut(curreentTicks);
                Thread.Sleep(100);
                }
            if ( timeOut )
                {
                client.Close();
                }
            return !timeOut;
            }

        private bool TimeOut(long curreentTicks)
            {
            return new TimeSpan(( DateTime.Now.Ticks - curreentTicks )).TotalSeconds > TIME_OUT;
            }

        internal void Stop()
            {
            client.Close();
            }
        }
    }
