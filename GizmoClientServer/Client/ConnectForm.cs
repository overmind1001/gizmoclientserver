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
using Dispatcher;
using System.Net;

namespace Client
{
    public partial class ConnectForm : Form
    {
        //public TcpClient tcpClient;//подключение к серверу
        public int MyPort;
        public TcpListener tcpListener;
        public TcpClient tcpClient;//будет использоваться в основной форме для регистрации на сервере


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
        int getFreeListenerPort(out TcpListener tcplistener)
        {
            bool portHasGot = false;
            Random r = new Random();
            int port=-1;
            tcplistener = null;

            while(!portHasGot)
            {
                try
                {

                    port = r.Next(100, 65000);
                    TcpListener tcpListener = new TcpListener(port);
                    tcpListener.Start();
                    tcplistener = tcpListener;
                }
                catch (Exception ex)
                { 
                    System.Diagnostics.Debug.AutoFlush=true;
                    System.Diagnostics.Debug.WriteLine("Client. Порт занят: "+port);
                }
            }
            return port;
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
            
            MyPort = getFreeListenerPort(out this.tcpListener);//включение листенера, получение порта
            try
            {
                TcpClient tcpClient=new TcpClient(tbIp.Text,Convert.ToInt32( numericUpDownPort.Value));

                NetStreamReaderWriter nsrw = new NetStreamReaderWriter(tcpClient.GetStream());
                //NetworkStream nstr=tcpClient.GetStream();
                //StreamWriter sw = new StreamWriter(nstr);
                //StreamReader sr = new StreamReader(nstr);
                //sw.AutoFlush = true;

                NetCommand whoCmd = new NetCommand()
                {
                    Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                    Port = MyPort,
                    sender = "client",//пока что безымянный
                    cmd = "!who",
                    parameters = ""
                };
                nsrw.WriteCmd(whoCmd);
                //sw.WriteLine("!who");


                NetCommand ansWhoCmd = nsrw.ReadCmd();

                //String who = sr.ReadLine();
                //если через диспетчера
                if (ansWhoCmd.cmd == "!dispatcher")
                {
                    NetCommand getserverCmd = new NetCommand()
                    {
                        Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                        Port = MyPort,
                        sender = "client",//пока что безымянный
                        cmd = "!getserver",
                        parameters = ""
                    };
                    nsrw.WriteCmd(getserverCmd);
                    //sw.WriteLine("!getserver");
                    NetCommand ansServerAddress = nsrw.ReadCmd();
                    //String servIPport = sr.ReadLine()
                    if (ansServerAddress.cmd != "!msgserver")
                    {
                        MessageBox.Show("Вместо адреса сервера получена шняга", "Какая-то фигня");
                        return;
                    }
                    String[] adr = ansServerAddress.parameters.Split(new char[]{' '});
                    tcpClient.Connect(adr[0], Convert.ToInt32( adr[1])); //коннектимся к серверу
                    nsrw = new NetStreamReaderWriter(tcpClient.GetStream());
                    //nstr = tcpClient.GetStream();
                    //sw = new StreamWriter(nstr);
                    nsrw.WriteCmd(whoCmd);
                    //sw.WriteLine("!Who");
                    //sr = new StreamReader(nstr);
                    ansWhoCmd = nsrw.ReadCmd();
                    //who = sr.ReadLine();
                }
                if (ansWhoCmd.cmd != "!messageserver")
                    throw new Exception("Не найден сервер обмена сообщениями");

                this.tcpClient = tcpClient;
                
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
