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

namespace SmartServerClient
    {
    public delegate void AddReceivedMessageDelegate(DateTime date, string senderNo, string messageBody);
    public partial class MainForm : Form
        {
        private QueryExecutor queryExecutor;

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
            queryExecutor = new QueryExecutor(this);
            }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
            {
            queryExecutor.Stop();
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
