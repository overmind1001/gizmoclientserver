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
            //запустить поток проверки доступности подключения
            Thread availableCheckThred = new Thread(availableCheck);
            availableCheckThred.Start();


        }
        void availableCheck()
        {
            List<ServerInfo> unregisteredServers = new List<ServerInfo>();
            while (Running)
            {
                unregisteredServers.Clear();
                foreach (ServerInfo s in MsgServers)
                {
                    if (s.tcpClient.Connected == false)//сервер отключен
                    {
                        unregisteredServers.Add(s);
                    }
                }

                foreach (ServerInfo s in unregisteredServers)
                {
                    unregisterServer(s.Ip, s.Port);
                    SendTextToAllServers(String.Format( "!serverunregistered {0} {1}",s.Ip,s.Port));
                }

                Thread.Sleep(1000);
            }
        }
        void unregisterServer(string ip, int port)
        {
            for (int i = 0; i < MsgServers.Count; i++)
            {
                if (MsgServers[i].Ip == ip && MsgServers[i].Port==port)
                {
                    MsgServers.RemoveAt(i);
                    lbMsgServers.DataSource = null;
                    lbMsgServers.DataSource = MsgServers;
                    break;
                }
            }
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
                string []parameters;
                string cmd;
                string line;
                string param;       //если параметр 1

                while (tcpClient.Connected)
                {
                    //TODO Взаимодействие с серверами
                    try
                    {
                        line = netStream.ReadLine();
                        parameters = line.Split(new char[] { ' ' });
                        cmd = parameters[0];
                        param = line.Substring(cmd.Length);

                        switch (cmd)
                        {
                            case "!who":        //к кому я подключился?
                                netStream.WriteLine("dispatcher");
                                break;
                            case "!register":   //зарегистрируй меня
                                if (AddServer(parameters[1], parameters[2], Convert.ToInt32(parameters[3])))
                                    netStream.WriteLine("registered");
                                else
                                    netStream.WriteLine("notregistered");
                                break;
                            case "!getserverlist":  //дай мне список серверв сообщений
                                SendServerList(ns);
                                break;
                            case "!getclientlist":  //дай мне список клиентов
                                SendClientList(ns);
                                break;
                            case "!clientregistered":   //появился новый клиент
                                if( RegisterClient(param))
                                    SendTextToAllServers(line);
                                break;
                            case "!clientunregistered":
                                UnregisterClient(param);
                                SendTextToAllServers(line);
                                break;
                            case "!getfilelist":
                                SendFileList(ns);
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

            this.lbMsgServers.DataSource = null;//отображаем в листбоксе
            this.lbMsgServers.DataSource = MsgServers;

            return true;
        }

//наверное надо поставить блокировки на коллекции
        void SendServerList(Stream stream)
        {
            NetStreamReaderWriter ns = new NetStreamReaderWriter(stream);
            string line = "";
            for(int i=0;i<MsgServers.Count;i++)
            {
                line += MsgServers[i].Ip + " " + MsgServers[i].Port.ToString();
                if (i != MsgServers.Count - 1)
                    line += "|";
            }
            ns.WriteLine(line);
        }
        void SendClientList(Stream stream)
        {
            NetStreamReaderWriter ns = new NetStreamReaderWriter(stream);
            string line = "";
            for (int i = 0; i < Clients.Count; i++)
            {
                line += Clients[i].ClientName;
                if (i != Clients.Count - 1)
                    line += "|";
            }
            ns.WriteLine(line);
        }
        void SendFileList(Stream stream)
        {
            NetStreamReaderWriter ns = new NetStreamReaderWriter(stream);
            string line = "";
            for (int i = 0; i < Files.Count; i++)
            {
                line += Files[i].Filename;
                if (i != Files.Count - 1)
                    line += "|";
            }
            ns.WriteLine(line);
        }
        void SendTextToAllServers(string line)
        {//блокировки?
            for (int i = 0; i < MsgServers.Count;i++ )
            {
                try
                {
                    NetStreamReaderWriter ns = new NetStreamReaderWriter(MsgServers[i].tcpClient.GetStream());
                    ns.WriteLine(line);
                }
                catch(IOException ioex)
                {
                    System.Diagnostics.Debug.AutoFlush = true;
                    System.Diagnostics.Debug.WriteLine("Dispatcher:"+ioex.Message+"похоже что один из сереров отключился или поломался!");
                }
            }
        }
        bool RegisterClient(string name)
        {//блокировки??
            ClientInfo exsisting = Clients.Find((x) => {return x.ClientName == name; });
            if (exsisting != null)
                return false;

            ClientInfo newClient = new ClientInfo();
            newClient.ClientName = name;

            Clients.Add(newClient);

            lbClients.DataSource = null;
            lbClients.DataSource = Clients;

            return true;
        }
        void UnregisterClient(string name)
        {
            for (int i = 0; i < Clients.Count; i++)
            {
                if (Clients[i].ClientName == name)
                {
                    Clients.RemoveAt(i);
                    lbClients.DataSource = null;
                    lbClients.DataSource = Clients;
                    break;
                }
            }
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
