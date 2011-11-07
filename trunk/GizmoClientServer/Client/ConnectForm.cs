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
            tbIp.Text = "localhost";
            tbName.Text = "Sanya";
            numericUpDownPort.Value = 500;

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
            if (tbName.Text.Trim() == String.Empty)
            {
                MessageBox.Show("Имя не должно быть пустым!");
                return;
            }
            try
            {
                tcpClient=new TcpClient(tbIp.Text,Convert.ToInt32( numericUpDownPort.Value));
                NetworkStream nstr=tcpClient.GetStream();
                StreamWriter sw = new StreamWriter(nstr);
                sw.AutoFlush = true;
                sw.WriteLine("!who");

                StreamReader sr = new StreamReader(nstr);
                String who = sr.ReadLine();
                //если через диспетчера
                if (who == "dispatcher")
                {
                    sw.WriteLine("!getserver");
                    String servIPport = sr.ReadLine();
                    
                    char [] sep = {' '};
                    String[] adr = servIPport.Split(sep);


                    tcpClient.Connect(adr[0], Convert.ToInt32( adr[1])); //коннектимся к серверу
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
                this.tcpClient = null;
                //this.DialogResult=DialogResult.Cancel;
            }
        }
    }
}
