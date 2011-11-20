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
using System.Diagnostics;

namespace Client
{
    public partial class ConnectForm : Form
    {
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
                    TcpListener tcpListener = new TcpListener(Dns.GetHostAddresses("localhost")[0],port);
                    tcpListener.Start();
                    tcplistener = tcpListener;
                    portHasGot = true;
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
                NetCommand whoCmd = new NetCommand()
                {
                    Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                    Port = MyPort,
                    sender = "client",//пока что безымянный
                    cmd = "!who",
                    parameters = ""
                };
                nsrw.WriteCmd(whoCmd);
                NetCommand ansWhoCmd = nsrw.ReadCmd();
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
                    NetCommand ansServerAddress = nsrw.ReadCmd();//ждем ответ
                    if (ansServerAddress.cmd == "!hasnotserver")
                    {
                        MessageBox.Show("А нету серверов!");
                        return;
                    }
                    if (ansServerAddress.cmd != "!msgserver")
                    {
                        MessageBox.Show("Вместо адреса сервера получена шняга", "Какая-то фигня");
                        return;
                    }
                    String[] adr = ansServerAddress.parameters.Split(new char[]{' '});
                    tcpClient.Connect(adr[0], Convert.ToInt32( adr[1])); //коннектимся к серверу
                    nsrw = new NetStreamReaderWriter(tcpClient.GetStream());
                    nsrw.WriteCmd(whoCmd);
                    ansWhoCmd = nsrw.ReadCmd();
                }
                if (ansWhoCmd.cmd != "!messageserver")
                    throw new Exception("Не найден сервер обмена сообщениями");

                this.tcpClient = tcpClient;
                
                this.DialogResult=DialogResult.OK;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnRecieveBroadcastAddr_Click(object sender, EventArgs e)
        {
            try
            {
                UdpClient udpClient = new UdpClient(11000);
                using (udpClient)
                {
                    IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 11000);
                    udpClient.Client.ReceiveTimeout = 3000;
                    Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                    string returnData = Encoding.ASCII.GetString(receiveBytes);
                    tbIp.Text = returnData;
                    udpClient.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ну удалось автоматически обнаружить адрес!");
                Debug.WriteLine("Client. ConnectForm "+ex.Message);
            }
        }
    }
}
