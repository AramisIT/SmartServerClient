using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SmartServerClient.Properties;
using SmartServerClient.Connection;
using System.IO.Ports;
using System.Diagnostics;
using Aramis.SMSHelperNamespace;

namespace SmartServerClient
    {
    public delegate void AddReceivedMessageDelegate(DateTime date, string senderNo, string messageBody);
    public partial class MainForm : Form
        {
        private SmartClient Client;

        public MainForm()
            {
            InitializeComponent();
            
            //Settings.Default.ServerIP = (string)Settings.Default.Properties["ServerIP"].DefaultValue;
            //Settings.Default.Save();
            }

        //public void ShowPingResult(string str)
        //    {
        //    if ( InvokeRequired )
        //        {
        //        Invoke(new FVoid1StringDelegate(ShowPingResult), new object[] { str });
        //        }
        //    else
        //        {
        //        Text = str;
        //        }
        //    }

        private void MainForm_Load(object sender, EventArgs e)
            {
            SMSHelper.SmsHelper.OnRefreshConnectionStatus += new SetConnectionStatusDelegate(SmsHelper_OnRefreshConnectionStatus);
            SMSHelper.SmsHelper.OnReceivingMessage += new OnReceivingMessageDelegate(SmsHelper_OnReceivingMessage);
            SMSHelper.SmsHelper.OnSendingMessage += new OnSendingMessageDelegate(SmsHelper_OnSendingMessage);
            Client = new SmartClient();
            Client.OnRemouteSMSServiceOffline += new OnRemouteSMSServiceOfflineDelegate(Client_OnRemouteSMSServiceOffline);
            Client.OnTestStarted += new OnTestStartedDelegate(Client_OnTestStarted);
            Client.OnTestEnded += new OnTestEndedDelegate(Client_OnTestEnded);
            }

        void Client_OnTestEnded(Aramis.Enums.TestResults result)
            {
            if ( Log.InvokeRequired )
                {
                Log.Invoke(new OnTestEndedDelegate(Client_OnTestEnded), result);
                }
            else
                {
                if ( result == Aramis.Enums.TestResults.Ok )
                    {
                    Log.SelectionColor = Color.GreenYellow;
                    Log.AppendText(String.Format("Тест системы доставки СМС завершен успешно в {0}\r\n", DateTime.Now));
                    }
                else
                    {
                    Log.SelectionColor = Color.Red;
                    Log.AppendText(String.Format("Тест системы доставки СМС завершен неудачей в {0}\r\n", DateTime.Now));
                    }
                }
            }

        void Client_OnTestStarted()
            {
            if ( Log.InvokeRequired )
                {
                Log.Invoke(new OnTestStartedDelegate(Client_OnTestStarted));
                }
            else
                {
                Log.SelectionColor = Color.GreenYellow;
                Log.AppendText(String.Format("Тест системы доставки СМС начат в {0}\r\n", DateTime.Now));
                }
            }

        void Client_OnRemouteSMSServiceOffline()
            {
            if ( Log.InvokeRequired )
                {
                Log.Invoke(new OnRemouteSMSServiceOfflineDelegate(Client_OnRemouteSMSServiceOffline));
                }
            else
                {
                    Log.SelectionColor = Color.Red;
                    Log.AppendText(String.Format("Сервис доставки СМС с номером {0} не отвечает!\r\n", Settings.Default.RemoutePhoneNumber));
                }
            }

        void SmsHelper_OnSendingMessage(Aramis.SMSHelperNamespace.Message message, bool sendingResult, string errorDescription)
            {
            if ( Log.InvokeRequired )
                {
                Log.Invoke(new OnSendingMessageDelegate(SmsHelper_OnSendingMessage), message, sendingResult, errorDescription);
                }
            else
                {
                if ( sendingResult )
                    {
                    Log.SelectionColor = Color.Orange;
                    Log.AppendText(String.Format("Отправлено сообщение:\r\n\tОтправитель:{0}\r\n\tТекст сообщения:{1}\r\n", message.Number, message.MessageBody));
                    }
                else
                    {
                    Log.SelectionColor = Color.Red;
                    Log.AppendText(String.Format("Ошибка при отправке: {0}\r\n", errorDescription));
                    }
                }
            }

        void SmsHelper_OnReceivingMessage(Aramis.SMSHelperNamespace.Message message)
            {
            if ( Log.InvokeRequired )
                {
                Log.Invoke(new OnReceivingMessageDelegate(SmsHelper_OnReceivingMessage), message);
                }
            else
                {
                Log.SelectionColor = Color.Green;
                Log.AppendText(String.Format("Принято сообщение:\r\n\tОтправитель:{0}\r\n\tТекст сообщения:{1}\r\n", message.Number, message.MessageBody));
                }
            }

        void SmsHelper_OnRefreshConnectionStatus(bool IsOnline)
            {
            if ( InvokeRequired )
                {
                if ( !IsDisposed )
                    {
                    try
                        {
                        Invoke(new SetConnectionStatusDelegate(SmsHelper_OnRefreshConnectionStatus), IsOnline);
                        }
                    catch
                        {

                        }
                    }
                }
            else
                {
                Status.Text = IsOnline ? "Online" : "Offline";
                }
            }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
            {
            Client.Stop();
            }

        private void button1_Click(object sender, EventArgs e)
            {
            //using ( var comPort = new SerialPort(String.Format("COM{0}", Settings.Default.ComPortNumber)) )
            //    {
            //    comPort.BaudRate = 9600; // Bits per second
            //    comPort.DataBits = 8;
            //    comPort.Parity = Parity.None;
            //    comPort.StopBits = StopBits.One;

            //    comPort.ReadTimeout = 300;

            //    comPort.Handshake = Handshake.None;

            //    comPort.Open();

            //    string mess = "AT+CPIN?\r";
            //    comPort.Write(mess);
            //    //Line();

            //    System.Threading.Thread.Sleep(1200);

            //    char[] buff = new char[256];

            //    while ( true )
            //        {
            //        string count = comPort.ReadLine();//buff, 0, buff.Length);
            //        //Trace.WriteLine(str);
            //        Console.WriteLine(count);
            //        System.Threading.Thread.Sleep(800);
            //        }
            //    }
            //GSMTerminalAgent agent = new GSMTerminalAgent();
            //agent.SendSMS("380955627688", "Прикинь! РАБОТАЕТ ^_^");
            }


        //private void button1_Click(object sender, EventArgs e)
        //    {
        //    button1.Enabled = false;
        //    MessageText.Enabled = false;
        //    Number.Enabled = false;
        //    button1.Text = "Отправка...";
        //    label1.Text = "Отправка...";
        //    SMSHelper.SmsHelper.OnSMSSended += new SMSSendedDelegate(SMSHelper_OnSMSSended);
        //    SMSHelper.SmsHelper.OnSMSSendingRepeate += new SNSSendingRepeateDelegate(SmsHelper_OnSMSSendingRepeate);
        //    SMSHelper.SmsHelper.SendMessage(GetPhoneNumber(), MessageText.Text);

        //    }

        //private string GetPhoneNumber()
        //    {
        //    string code = Number.Text.Substring(5, 3);
        //    string number = Number.Text.Substring(10, 3) + Number.Text.Substring(14, 2) + Number.Text.Substring(17, 2);
        //    return code + number;            
            
        //    }
        }
    }
