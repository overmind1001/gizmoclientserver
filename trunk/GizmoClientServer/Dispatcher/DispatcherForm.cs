using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace Dispatcher
{
    public partial class DispatcherForm : Form
    {
        List<ServerInfo> MsgServers;
        List<ServerInfo> FileServers;
        List<ClientInfo> Clients;
        List<FileInfo> Files;

        bool Running = true;
        

        public DispatcherForm()
        {
            InitializeComponent();

            MsgServers = new List<ServerInfo>();
            FileServers = new List<ServerInfo>();
            Clients = new List<ClientInfo>();
            Files = new List<FileInfo>();
            
            //запустить поток слушающий порт для клиентов
            Thread clientListenerThread = new Thread(clientTcpListenerProc);
            clientListenerThread.Start();
            //запустить поток, слушающий порт для серверов
            Thread serverListenerThread = new Thread(serverTcpListenerProc);
            serverListenerThread.Start();
            //запустить поток для посылки широковещательных сообщений
            Thread broadcastThread = new Thread(broadcastSelfInfo);
            broadcastThread.Start();

        }

        //взаимодействие с клиентами
        void clientTcpListenerProc()
        {
            TcpListener clientTcpListener = new TcpListener(500);
            clientTcpListener.Start();
            while (Running)
            {
                TcpClient client = clientTcpListener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(DoCommunicateWithClient, client);
            }
        }
        void DoCommunicateWithClient(object tcpclient)
        {
            using (TcpClient tcpClient = (TcpClient) tcpclient)
            using (NetworkStream ns = tcpClient.GetStream())
            {
                StreamReader sr = new StreamReader(ns);
                sr.BaseStream.ReadTimeout = 20000;//ожидание 20 сек.
                StreamWriter sw = new StreamWriter(ns);
                sw.AutoFlush = true;
                string cmd;
                
                while (tcpClient.Connected)
                {
                    //TODO Взаимодействие с клиентами
                    try
                    {
                        cmd = sr.ReadLine();
                        switch (cmd)
                        {
                            case "!who":
                                sw.WriteLine("dispatcher");
                                break;
                            case "!getserver":
                                ServerInfo msgServ;
                                lock (MsgServers)
                                {
                                    Random r = new Random();
                                    int i=r.Next(0, MsgServers.Count-1);
                                    msgServ = MsgServers[i];
                                }
                                sw.WriteLine(String.Format("{0} {1}",msgServ.Ip,msgServ.Port));
                                break;
                        }
                    }
                    catch (IOException ioex)
                    {
                        tbLog.Text +=Environment.NewLine+ ioex.Message ;
                    }
                }
            }
        }

        //Взаимодействие с серверами
        void serverTcpListenerProc()
        {
            TcpListener serverTcpListener = new TcpListener(501);
            serverTcpListener.Start();
            while (Running)
            {
                TcpClient client = serverTcpListener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(DoCommunicateWithsServer, client);
            }
        }
        void DoCommunicateWithsServer(object tcpclient)
        {
            using (TcpClient tcpClient = (TcpClient)tcpclient)
            using (NetworkStream ns = tcpClient.GetStream())
            {
                NetStreamReaderWriter netStream = new NetStreamReaderWriter(ns);
                string cmd;
                string line;
                string param;

                while (tcpClient.Connected)
                {
                    //TODO Взаимодействие с серверами
                    try
                    {
                        line = netStream.ReadLine();
                        cmd = line.Split(new char[] { ' ' })[0];
                        param = line.Substring(cmd.Length);

                        switch (cmd)
                        {
                            case "!who":
                                netStream.WriteLine("dispatcher");
                                break;
                            case "!registerme":
                                //if(AddServer(param,"ip",
                                netStream.WriteLine("registered");
                                break;
                            case "!getserverlist":
                                break;
                            case "!getclientlist":
                                break;
                            case "!clientregistered":
                                break;
                            case "!clientunregistered":
                                break;
                            case "!getfilelist":
                                break;
                            case "!getfreefileserver":
                                break;
                            case "!getfileserver":
                                break;

                            default:
                                MessageBox.Show("Dispatcher: Неизвестная команда. "+line);
                                break;
                        }
                    }
                    catch (IOException ioex)
                    {
                        tbLog.Text += Environment.NewLine + ioex.Message;
                    }
                }
            }
        }
        bool AddServer(string type, string ip, int port)
        {
            string serverRecord = type+"_"+ip.ToString()+"_"+port;
            ServerInfo serverInfo = new ServerInfo();
            serverInfo.Ip = ip;
            serverInfo.Port = port;
            MsgServers.Add(serverInfo);

            return true;
        }
        //Широковещание
        void broadcastSelfInfo()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPAddress broadcast = IPAddress.Parse("192.127.150.255");
           // IPHostEntry host=Dns.GetHostByName(Dns.GetHostName());
            byte[] sendbuf = Encoding.ASCII.GetBytes(Dns.GetHostName());
            IPEndPoint ep = new IPEndPoint(broadcast, 11000);
            while (Running)
            {
                s.SendTo(sendbuf, ep);
                Thread.Sleep(500);
            }
            
        }

        private void DispatcherForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Running = false;
            Process.GetCurrentProcess().Kill();
        }
    }
}
