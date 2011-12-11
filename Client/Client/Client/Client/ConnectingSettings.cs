using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Client
{
    public partial class ConnectingSettings : Form
    {
        public ConnectingSettings()
        {
            InitializeComponent();
        }

        private void Confirm(object sender, EventArgs e)
        {
            Stream stream = File.Open("config.info", FileMode.Create);
            BinaryFormatter bformatter = new BinaryFormatter();

            string retrievedIP = textBox1.Text;
            string retrievedPort = textBox2.Text;

            //aici am ip si portul disponibil pt conectare

            ConnectingSettingsData info = new ConnectingSettingsData(retrievedIP,retrievedPort);
             
            bformatter.Serialize(stream, info);
            stream.Close();
            MainForm pointerToMainForm = (MainForm)Application.OpenForms[0];
            pointerToMainForm.ShowMessageNotifyIcon(" s-a schimbat Ip/portul ");

            this.Close();

            //Form1.notifyIcon1.ShowBalloonTip(1500, "info", "client nou", ToolTipIcon.Info);
        }

        private void Get_Default_Settings(object sender, EventArgs e)
        {
            this.textBox1.Text = "127.0.0.1";
            this.textBox2.Text = "8001";
        }

        private void Get_IP_Port_extern(object sender, EventArgs e)
        {
            Stream stream = File.Open("config.info", FileMode.Open);
            BinaryFormatter bformatter = new BinaryFormatter();
            bformatter = new BinaryFormatter();

            ConnectingSettingsData info = (ConnectingSettingsData)bformatter.Deserialize(stream);
            stream.Close();

            this.textBox1.Text = info.ip;
            this.textBox2.Text = info.port;

        }
        public void LoadConnectingSettingsOnStart()
        {
            Stream stream = File.Open("config.info", FileMode.Open);
            BinaryFormatter bformatter = new BinaryFormatter();
            bformatter = new BinaryFormatter();

            ConnectingSettingsData info = (ConnectingSettingsData)bformatter.Deserialize(stream);
            stream.Close();

            this.textBox1.Text = info.ip;
            this.textBox2.Text = info.port;

            //MessageBox.Show("Am incarcat Ip si portul curent ");
            MainForm pointerToMainForm = (MainForm)Application.OpenForms[0];
            pointerToMainForm.ShowMessageNotifyIcon(" IP/port loaded ... ");

            //aici le folosesti pentru conectare

        }
    }
}
