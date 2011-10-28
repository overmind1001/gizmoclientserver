using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.IO;

namespace Client
{
    public partial class ConnectForm : Form
    {
        public TcpClient tcpClient;//подключение к серверу

        public ConnectForm()
        {
            InitializeComponent();
            this.DialogResult= DialogResult.Cancel;
        }
        private bool IpIsValid()
        {
            return true;
        }
        private void butConnect_Click(object sender, EventArgs e)
        {
            if(!IpIsValid())
            {
                return;
            }
            try
            {
                tcpClient=new TcpClient(tbIp.Text,Convert.ToInt32( numericUpDownPort.Value));
                NetworkStream nstr=tcpClient.GetStream();
                StreamWriter sw = new StreamWriter(nstr);
                sw.WriteLine("!who");

                StreamReader sr = new StreamReader(nstr);
                String who = sr.ReadLine();
                //если через диспетчера
                if (who == "dispatcher")
                {
                    sw.WriteLine("!getserver");
                    String servIP = sr.ReadLine();

                    tcpClient.Connect(servIP, 501);
                    nstr = tcpClient.GetStream();
                    sw = new StreamWriter(nstr);
                    sw.WriteLine("!Who");

                    sr = new StreamReader(nstr);
                    who = sr.ReadLine();
                }
                //дальше под
                if (who != "messageserver")
                    throw new Exception("Не найден сервер обмена сообщениями");

                this.DialogResult=DialogResult.OK;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                //this.DialogResult=DialogResult.Cancel;
            }
        }
    }
}
