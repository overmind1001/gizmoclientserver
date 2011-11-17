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
        //делегаты
        public delegate void MyListChangedHandler();
        public MyListChangedHandler MsgServerListChanged;
        public MyListChangedHandler FileServerListChanged;
        public MyListChangedHandler ClientsListChanged;
        public MyListChangedHandler FilesListChanged;
        

        List<ServerInfo> MsgServers;
        List<ServerInfo> FileServers;
        List<ClientInfo> Clients;
        List<FileInfo> Files;

        bool Running = true;
        

        public DispatcherForm()
        {
            InitializeComponent();
            MsgServerListChanged += UpdateMsgServerList;
            FileServerListChanged += UpdateFileServerList;
            ClientsListChanged += UpdateClientsList;
            FilesListChanged += UpdateFilesList;

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
                lock (MsgServers)
                {
                    unregisteredServers.Clear();
                    foreach (ServerInfo s in MsgServers)
                    {
                        if (s.tcpClient.Connected == false)//сервер отключен
                        {
                            unregisteredServers.Add(s);
                        }
                    }
                }
                

                foreach (ServerInfo s in unregisteredServers)
                {
                    unregisterServer(s.Ip, s.Port);
                    //SendTextToAllServers(String.Format( "!serverunregistered {0} {1}",s.Ip,s.Port));
                }

                List<ServerInfo> unregisteredFileServers = new List<ServerInfo>();
                lock (FileServers)
                {
                    unregisteredFileServers.Clear();
                    foreach (ServerInfo s in FileServers)
                    {
                        if (s.tcpClient.Connected == false)//сервер отключен
                        {
                            unregisteredFileServers.Add(s);
                        }
                    }
                }

                foreach (ServerInfo s in unregisteredFileServers)
                {
                    unregisterFileServer(s.Ip, s.Port);
                }
                Thread.Sleep(1000);
            }
        }
        void unregisterFileServer(string ip, int port)
        {
            lock (FileServers)
            {
                for (int i = 0; i < FileServers.Count; i++)
                {
                    if (FileServers[i].Ip == ip && FileServers[i].Port == port)//нашли файлсервер
                    {
                        ServerInfo fileServer = FileServers[i];
                        lock (Files)//удаляем файлы этого файл серера
                        {
                            List<FileInfo> filesToDelete = new List<FileInfo>();
                            foreach (FileInfo f in fileServer.fileInfos)
                            {
                                filesToDelete.Add(f);
                                SendTextToAllServers(String.Format("!deletefile {0}", f.Filename));
                            }
                            foreach (FileInfo f in filesToDelete)//удаляем из коллекции
                            {
                                Files.Remove(f);
                            }
                            FilesListChanged();
                        }
                        FileServers.RemoveAt(i);        //удаляем из коллекции. Сообщать никому не надо.
                        FileServerListChanged();
                        break;
                    }
                } 
            }
        }
        void unregisterServer(string ip, int port)
        {
            lock (MsgServers)
            {
                for (int i = 0; i < MsgServers.Count; i++)
                {
                    if (MsgServers[i].Ip == ip && MsgServers[i].Port == port)
                    {
                        ServerInfo msgServer = MsgServers[i];
                        MsgServers.RemoveAt(i);
                        MsgServerListChanged();
                        SendServerUnregistered(ip, port);
                        unregisterClientsOfServer(msgServer);
                        break;
                    }
                }
            }
        }
        void unregisterClientsOfServer(ServerInfo serv)
        {
            foreach (ClientInfo c in serv.clientInfos)
            {
                lock (Clients)
                {
                    Clients.Remove(c);
                    ClientsListChanged();
                }
                SendTextToAllServers(String.Format("!clienturegistered {0}",c.ClientName));
            }
        }
        void SendServerUnregistered(string ip, int port)
        {
            SendTextToAllServers(string.Format("!serverunregistered {0} {1}",ip,port));
        }

        void UpdateMsgServerList()
        {
            lbMsgServers.DataSource = null;
            lbMsgServers.DataSource = MsgServers;
        }
        void UpdateFileServerList()
        {
            lbFileServers.DataSource = null;
            lbFileServers.DataSource = FileServers;
        }
        void UpdateClientsList()
        {
            lbClients.DataSource = null;
            lbClients.DataSource = Clients;
        }
        void UpdateFilesList()
        {
            lbFiles.DataSource = null;
            lbFiles.DataSource = Files;
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
                    
                    try
                    {
                        cmd = sr.ReadLine();
                        switch (cmd)
                        {
                            case "!who":
                                lock (tcpclient)
                                {
                                    sw.WriteLine("dispatcher");
                                }
                                break;
                            case "!getserver":
                                ServerInfo msgServ;
                                lock (MsgServers)
                                {
                                    Random r = new Random();
                                    int i=r.Next(0, MsgServers.Count-1);
                                    msgServ = MsgServers[i];
                                }
                                lock (tcpclient)
                                {
                                    sw.WriteLine(String.Format("{0} {1}", msgServ.Ip, msgServ.Port));
                                }
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
                ThreadPool.QueueUserWorkItem(DoCommunicateWithsServer_V2, client);
            }
        }
        //void DoCommunicateWithsServer(object tcpclient)
        //{
        //    using (TcpClient tcpClient = (TcpClient)tcpclient)
        //    using (NetworkStream ns = tcpClient.GetStream())
        //    {
        //        NetStreamReaderWriter netStream = new NetStreamReaderWriter(ns);
        //        string []parameters;
        //        string cmd;
        //        string line;
        //        string param;       //если параметр 1

        //        while (tcpClient.Connected)
        //        {
                    
        //            try
        //            {
        //                line = netStream.ReadLine();
        //                parameters = line.Split(new char[] { ' ' });
        //                cmd = parameters[0];
        //                param = line.Substring(cmd.Length);

        //                switch (cmd)
        //                {
        //                    case "!who":        //к кому я подключился?
        //                        lock (tcpclient)
        //                        {
        //                            netStream.WriteLine("dispatcher");
        //                        }
        //                        break;
        //                    case "!register":   //зарегистрируй меня
        //                        if (AddServer(parameters[1], parameters[2], Convert.ToInt32(parameters[3])))
        //                        {
        //                            lock (tcpclient)
        //                            {
        //                                netStream.WriteLine("registered");
        //                            }
        //                            //TODO
        //                            //getClientListFromServer(netStream);
        //                        }
        //                        else
        //                        {
        //                            lock (tcpclient)
        //                            {
        //                                netStream.WriteLine("notregistered");
        //                            }
        //                        }
        //                        break;
        //                    case "!getserverlist":  //дай мне список серверв сообщений
        //                        lock (tcpclient)
        //                        {
        //                            SendServerList(ns);
        //                        }
        //                        break;
        //                    case "!getclientlist":  //дай мне список клиентов
        //                        lock (tcpclient)
        //                        {
        //                            SendClientList(ns);
        //                        }
        //                        break;
        //                    case "!clientregistered":   //появился новый клиент
        //                        if( RegisterClient(param))
        //                            SendTextToAllServers(line);
        //                        break;
        //                    case "!clientunregistered":
        //                        UnregisterClient(param);
        //                        SendTextToAllServers(line);
        //                        break;
        //                    case "!getfilelist":
        //                        lock (tcpclient)
        //                        {
        //                            SendFileList(ns);
        //                        }
        //                        break;
        //                    case "!getfreefileserver":
        //                        lock (tcpclient)
        //                        {
        //                            if (FileServers.Count > 0)
        //                            {
        //                                ServerInfo fileServ;
        //                                lock (FileServers)
        //                                {
        //                                    Random r = new Random();
        //                                    int i = r.Next(0, FileServers.Count - 1);
        //                                    fileServ = FileServers[i];
        //                                }
        //                                netStream.WriteLine(String.Format("{0} {1}", fileServ.Ip, fileServ.Port));
        //                            }
        //                            else
        //                            {
        //                                netStream.WriteLine("error");
        //                            }
        //                        }
        //                        break;
        //                    case "!getfileserver":
        //                        //////////////////////////////////////TODO
        //                        break;

        //                    default:
        //                        MessageBox.Show("Dispatcher: Неизвестная команда. "+line);
        //                        break;
        //                }
        //            }
        //            catch (IOException ioex)
        //            {
        //                tbLog.Text += Environment.NewLine + ioex.Message;
        //                System.Diagnostics.Debug.WriteLine("Dispatcher :" + ioex.Message);
        //            }
        //        }
        //    }
        //}

        void DoCommunicateWithsServer_V2(object tcpclient)
        {
            using (TcpClient tcpClient = (TcpClient)tcpclient)
            using (NetworkStream ns = tcpClient.GetStream())
            {
                ServerInfo serverInfo=null;
                NetStreamReaderWriter netStream = new NetStreamReaderWriter(ns);
                
                string[] parameters;
                string cmd;
                string line;
                string param;       //если параметр 1

                while (tcpClient.Connected)
                {

                    try
                    {
                        while (tcpClient.Available < 1)//пока ничего не пришло ждем
                        {
                            Thread.Sleep(200);
                        }
                        
                        
                        line = netStream.ReadLine();
                        parameters = line.Split(new char[] { ' ' });
                        cmd = parameters[0];
                        param = line.Substring(cmd.Length);

                   
                        
                       
                        switch (cmd)
                        {
                            case "!who":        //к кому я подключился?
                                lock (tcpclient)
                                {
                                    netStream.WriteLine("dispatcher");
                                }
                                break;
                            case "!register":   //зарегистрируй меня
                                if (AddServer(parameters[1], parameters[2], Convert.ToInt32(parameters[3]),out serverInfo))
                                {
                                    lock (tcpclient)
                                    {
                                        netStream.WriteLine("registered");
                                    }
                                    //загрузка клиент-листа
                                    
                                    //beginLock
                                    //TODO
                                    //getClientListFromServer(netStream);
                                    
                                }
                                else
                                {
                                    lock (tcpclient)
                                    {
                                        netStream.WriteLine("notregistered");
                                    }
                                }
                                break;
                            case "!getserverlist":  //дай мне список серверв сообщений
                                lock (tcpclient)
                                {
                                    SendServerList(ns);
                                }
                                break;
                            case "!getclientlist":  //дай мне список клиентов
                                lock (tcpclient)
                                {
                                    SendClientList(ns);
                                }
                                break;
                            case "!clientregistered":   //появился новый клиент
                                if (RegisterClient(param))
                                    SendTextToAllServers(line);
                                break;
                            case "!clientunregistered":
                                UnregisterClient(param);
                                SendTextToAllServers(line);
                                break;
                            case "!getfilelist":
                                lock (tcpclient)
                                {
                                    SendFileList(ns);
                                }
                                break;
                            case "!getfreefileserver":
                                lock (tcpclient)
                                {
                                    if (FileServers.Count > 0)
                                    {
                                        ServerInfo fileServ;
                                        lock (FileServers)
                                        {
                                            Random r = new Random();
                                            int i = r.Next(0, FileServers.Count - 1);
                                            fileServ = FileServers[i];
                                        }
                                        netStream.WriteLine(String.Format("{0} {1}", fileServ.Ip, fileServ.Port));
                                    }
                                    else
                                    {
                                        netStream.WriteLine("error");
                                    }
                                }
                                break;
                            case "!getfileserver":
                                //////////////////////////////////////TODO
                                break;

                            default:
                                MessageBox.Show("Dispatcher: Неизвестная команда. " + line);
                                break;
                        }
                    }
                    catch (IOException ioex)
                    {
                        tbLog.Text += Environment.NewLine + ioex.Message;
                        System.Diagnostics.Debug.WriteLine("Dispatcher :" + ioex.Message);
                    }
                }
            }
        }
        bool AddServer(string type, string ip, int port,out ServerInfo serv)
        {
            string serverRecord = type+"_"+ip.ToString()+"_"+port;
            ServerInfo serverInfo = new ServerInfo();
            serv = serverInfo;
            serverInfo.Ip = ip;
            serverInfo.Port = port;

            if (type == "messageserver")
            {
                lock (MsgServers)
                {
                    MsgServers.Add(serverInfo);
                }
                MsgServerListChanged();//обновление списка серверов на форме
                SendTextToAllServers(string.Format("!serverregistered {0} {1}",ip,port));

                //TODO Запросить список клиентов
            }
            else
            {
                lock (FileServers)
                {
                    FileServers.Add(serverInfo);
                }
                FileServerListChanged();//обновление списка файлов на сервере


                //TODO Запросить список файлов
            }

            return true;
        }
        void SendServerList(Stream stream)
        {
            NetStreamReaderWriter ns = new NetStreamReaderWriter(stream);
            string line = "";
            lock (MsgServers)
            {    
                for (int i = 0; i < MsgServers.Count; i++)
                {
                    line += MsgServers[i].Ip + " " + MsgServers[i].Port.ToString();
                    if (i != MsgServers.Count - 1)
                        line += "|";
                }
            }
            ns.WriteLine(line);
        }
        void SendClientList(Stream stream)
        {
            NetStreamReaderWriter ns = new NetStreamReaderWriter(stream);
            string line = "";
            lock (Clients)
            {
                for (int i = 0; i < Clients.Count; i++)
                {
                    line += Clients[i].ClientName;
                    if (i != Clients.Count - 1)
                        line += "|";
                }
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
        {
            for (int i = 0; i < MsgServers.Count;i++ )
            {
                try
                {
                    lock (MsgServers[i].tcpClient)
                    {
                        NetStreamReaderWriter ns = new NetStreamReaderWriter(MsgServers[i].tcpClient.GetStream());
                        ns.WriteLine(line);
                    }
                }
                catch(IOException ioex)
                {
                    System.Diagnostics.Debug.AutoFlush = true;
                    System.Diagnostics.Debug.WriteLine("Dispatcher:"+ioex.Message+"похоже что один из сереров отключился или поломался!");
                }
            }
        }
        bool RegisterClient(string name)
        {
            lock (Clients)
            {
                ClientInfo exsisting = Clients.Find((x) => {return x.ClientName == name; });
                if (exsisting != null)
                    return false;
                ClientInfo newClient = new ClientInfo();
                newClient.ClientName = name;
                Clients.Add(newClient);
                ClientsListChanged();//обновление списка клиентов на форме
      
            }
            return true;
        }
        void UnregisterClient(string name)
        {
            lock (Clients)
            {
                for (int i = 0; i < Clients.Count; i++)
                {
                    if (Clients[i].ClientName == name)
                    {
                        Clients.RemoveAt(i);
                        ClientsListChanged();//обновление списка клиентов на форме
                        break;
                    }
                }
            }
        }
        //Широковещание
        void broadcastSelfInfo()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //IPAddress broadcast = IPAddress.Parse("192.127.150.255");
            IPHostEntry host=Dns.GetHostByName(Dns.GetHostName());
            IPAddress broadcast =  host.AddressList[0];

            IPAddress tail = IPAddress.Parse("0.0.0.255");

            broadcast.Address = broadcast.Address | tail.Address;
            
            byte[] sendbuf = Encoding.ASCII.GetBytes(Dns.GetHostName());
            IPEndPoint ep = new IPEndPoint(broadcast, 11000);
            while (Running)
            {
                s.SendTo(sendbuf, ep);
                Thread.Sleep(1000);
            }
            
        }

        private void DispatcherForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Running = false;
            Process.GetCurrentProcess().Kill();
        }
    }
}
