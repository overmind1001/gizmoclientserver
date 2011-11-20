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

        IPAddress DispatcherIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];

        List<ServerInfo> MsgServers;
        List<ServerInfo> FileServers;
        List<ClientInfo> Clients;
        List<FileInfo> Files;

        bool Running = true;
        

        public DispatcherForm()
        {
            System.Diagnostics.Debug.AutoFlush = true;
            System.Diagnostics.Debug.WriteLine("Dispatcher. Started");
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
    
   
        void AsyncSendText(string ip,int port,string text)//асинхронная посылка текста
        {
            Thread t = new Thread(() => 
            {//потоковая процедура
                try
                {
                    TcpClient tcpClient = new TcpClient(ip, port);
                    NetStreamReaderWriter ns = new NetStreamReaderWriter(tcpClient.GetStream());
                    ns.WriteLine(text);
                    //тут надо что-то сделать чтобы не закрыть соединение, пока клиент не примет сообщение
                    //пока что костыль
                    Thread.Sleep(10000);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.AutoFlush = true;
                    System.Diagnostics.Debug.WriteLine("Dispatcher. AsyncSendText. "+ex.Message);
                }
            });
            t.Start();
        }
        void AsyncSendCmd(string ip, int port, NetCommand cmd)
        {
            AsyncSendText(ip, port, cmd.ToString());
        }
        void SendText(string ip, int port, string text)//синхронная посылка команды(в том же потоке)
        {
            try
            {
                TcpClient tcpClient = new TcpClient(ip, port);
                NetStreamReaderWriter ns = new NetStreamReaderWriter(tcpClient.GetStream());
                ns.WriteLine(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.AutoFlush = true;
                System.Diagnostics.Debug.WriteLine("Dispatcher. AsyncSendText. " + ex.Message);
            }
        }
        void SendCmd(string ip, int port, NetCommand cmd)
        {
            SendText(ip, port, cmd.ToString());
        }
            
        void availableCheck()//потоковая процедура проверки доступности серверов и файл серверов
        {
            List<ServerInfo> unregisteredServers = new List<ServerInfo>();
            while (Running)
            {
                lock (MsgServers)
                {
                    unregisteredServers.Clear();
                    foreach (ServerInfo s in MsgServers)
                    {
                        if ( ( DateTime.Now-s.lastPingTime).TotalSeconds>60)//сервер не пингует уже 30 сек
                        {
                            unregisteredServers.Add(s);
                        }
                    }
                }
                

                foreach (ServerInfo s in unregisteredServers)
                {
                    unregisterServer(s.Ip, s.Port);
                }

                List<ServerInfo> unregisteredFileServers = new List<ServerInfo>();
                lock (FileServers)
                {
                    unregisteredFileServers.Clear();
                    foreach (ServerInfo s in FileServers)
                    {
                        if ((DateTime.Now - s.lastPingTime).TotalSeconds > 30)//сервер отключен
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
                                NetCommand deletefileCmd = new NetCommand() 
                                {
                                    Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses( Dns.GetHostName())[0].ToString(),
                                    Port = 501,
                                    sender = "dispatcher",
                                    cmd ="!deletefile",
                                    parameters = f.Filename
                                };
                                SendCmdToAllServers(deletefileCmd);
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
            //foreach (ClientInfo c in serv.clientInfos)
            //{
            //    lock (Clients)
            //    {
            //        Clients.Remove(c);
            //        ClientsListChanged();
            //    }
            //    NetCommand clientUnregisteredCmd = new NetCommand()
            //    {
            //        Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
            //        Port = 501,
            //        sender = "dispatcher",
            //        cmd = "!clienturegistered",
            //        parameters = c.ClientName
            //    };
            //    SendCmdToAllServers(clientUnregisteredCmd);
            //}
        }
        void SendServerUnregistered(string ip, int port)
        {
            NetCommand serverUnregisteredCmd = new NetCommand()
            {
                Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = 501,
                sender = "dispatcher",
                cmd = "!serverunregistered",
                parameters = string.Format("{0} {1}", ip, port)
            };
            SendCmdToAllServers(serverUnregisteredCmd);
        }
        //методы для взаимодействия с формой. Вызываются через делегаты
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
            TcpListener clientTcpListener = new TcpListener(DispatcherIP, 500);
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
                NetStreamReaderWriter nsrw = new NetStreamReaderWriter(ns);
                while (tcpClient.Connected)//??
                {
                    try
                    {
                        NetCommand command = nsrw.ReadCmd();
                        
                        switch (command.cmd)
                        {
                            case "!who":
                                NetCommand dispatcherCmd = new NetCommand()
                                {
                                    Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                                    Port = 500,
                                    sender = "dispatcher",
                                    cmd = "!dispatcher"
                                };
                                nsrw.WriteCmd(dispatcherCmd);
                                break;
                            case "!getserver":
                                ServerInfo msgServ;
                                lock (MsgServers)
                                {
                                    Random r = new Random();
                                    if (MsgServers.Count != 0)
                                    {
                                        int i = r.Next(0, MsgServers.Count - 1);
                                        msgServ = MsgServers[i];
                                        NetCommand msgServerCmd = new NetCommand()
                                        {
                                            Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                                            Port = 500,
                                            sender = "dispatcher",
                                            cmd = "!msgserver",
                                            parameters = String.Format("{0} {1}", msgServ.Ip, msgServ.Port)
                                        };
                                        nsrw.WriteCmd(msgServerCmd);
                                    }
                                    else
                                    {
                                        NetCommand hasNotServerCmd = new NetCommand()
                                        {
                                            Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                                            Port = 500,
                                            sender = "dispatcher",
                                            cmd = "!hasnotserver",
                                            parameters = ""
                                        };
                                        nsrw.WriteCmd(hasNotServerCmd);
                                    }   
                                }
                                break;
                        }
                    }
                    catch (IOException ioex)
                    {
                        System.Diagnostics.Debug.AutoFlush = true;
                        System.Diagnostics.Debug.WriteLine("Client. " + ioex.Message);
                        //tbLog.Text +=Environment.NewLine+ ioex.Message ;
                    }
                }
            }
        }

        //Взаимодействие с серверами
        void serverTcpListenerProc()
        {
            TcpListener serverTcpListener = new TcpListener(DispatcherIP, 501);
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
                NetCommand command;

                if (tcpClient.Connected)
                {

                    try
                    {
                        while (tcpClient.Available < 1)//пока ничего не пришло ждем
                        {
                            Thread.Sleep(200);
                            if (!tcpClient.Connected)
                                return;
                        }
                        command = netStream.ReadCmd();
                        parameters = command.parameters.Split(new char[] { ' ' });
                       
                        switch (command.cmd)
                        {
                            case "!who":        //к кому я подключился?
                                NetCommand dispatcherCmd = new NetCommand()
                                {
                                    Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                                    Port = 501,
                                    sender = "dispatcher",
                                    cmd = "!dispatcher"
                                };
                                netStream.WriteCmd(dispatcherCmd);
                                break;
                            case "!register":   //зарегистрируй меня
                                if (AddServer(command.sender, command.Ip, command.Port ,out serverInfo))
                                {
                                    NetCommand registredCmd = new NetCommand()
                                    {
                                        Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                                        Port = 501,
                                        sender = "dispatcher",
                                        cmd = "!registered"
                                    };
                                    netStream.WriteCmd(registredCmd);
                                }
                                else
                                {
                                    NetCommand notRegistredCmd = new NetCommand()
                                    {
                                        Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                                        Port = 501,
                                        sender = "dispatcher",
                                        cmd = "!notregistered",
                                        parameters=""
                                    };
                                    netStream.WriteCmd(notRegistredCmd);
                                }
                                break;
                            case "!getserverlist":  //дай мне список серверв сообщений
                                SendServerList(ns);
                                break;
                            case "!getclientlist":  //дай мне список клиентов
                                SendClientList(ns);
                                break;
                            case "!clientregistered":   //появился новый клиент
                                if (RegisterClient(command.parameters))
                                {
                                    NetCommand c = command.Clone();//адрес отправителя уже другой
                                    c.Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString();
                                    SendCmdToAllServers(c);
                                }
                                break;
                            case "!clientunregistered":
                                UnregisterClient(command.parameters);
                                {
                                    NetCommand c = command.Clone();//адрес отправителя уже другой
                                    c.Ip = DispatcherIP.ToString();//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString();
                                    c.Port = 501;
                                    SendCmdToAllServers(c);
                                }
                                break;
                            case "!getfilelist":
                                SendFileList(ns);
                                break;
                            case "!getfreefileserver":
                                if (FileServers.Count > 0)
                                {
                                    ServerInfo fileServ;
                                    lock (FileServers)
                                    {
                                        Random r = new Random();
                                        int i = r.Next(0, FileServers.Count - 1);
                                        fileServ = FileServers[i];
                                    }
                                    NetCommand freeFileServerCmd = new NetCommand()
                                    {
                                        Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                                        Port = 501,
                                        sender = "dispatcher",
                                        cmd = "!freefileserver",
                                        parameters = String.Format("{0} {1}", fileServ.Ip, fileServ.Port)
                                    };
                                    netStream.WriteCmd(freeFileServerCmd);
                                }
                                else
                                {
                                    NetCommand errorCmd = new NetCommand()
                                    {
                                        Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                                        Port = 501,
                                        sender = "dispatcher",
                                        cmd = "!error",
                                        parameters = ""
                                    };
                                    netStream.WriteCmd(errorCmd);
                                }
                                break;
                            case "!ping":

                                if (command.sender == "msgserver")
                                {
                                    lock (MsgServers)
                                    {
                                        ServerInfo si = MsgServers.Find((ServerInfo s) => { return (s.Ip == command.Ip && s.Port == command.Port); });
                                        if (si != null)
                                        {
                                            si.lastPingTime = DateTime.Now;
                                        }
                                        else
                                        {
                                            MessageBox.Show("Пингующий сервер (СОС) не найден!", "Так быть не должно");
                                        }
                                    }
                                }
                                if (command.sender == "fileserver")
                                {
                                    lock (FileServers)
                                    {
                                        ServerInfo si = FileServers.Find((ServerInfo s) => { return (s.Ip == command.Ip && s.Port == command.Port); });
                                        if (si != null)
                                        {
                                            si.lastPingTime = DateTime.Now;
                                        }
                                        else
                                        {
                                            MessageBox.Show("Пингующий файл сервер  не найден!", "Так быть не должно");
                                        }
                                    }
                                }
                                if ((command.sender == "msgserver") || (command.sender == "fileserver"))
                                {
                                    NetCommand pongCmd = new NetCommand()
                                    {
                                        Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                                        Port = 501,
                                        sender = "dispatcher",
                                        cmd = "!pong",
                                        parameters = ""
                                    };
                                    netStream.WriteCmd(pongCmd);
                                }
                                break;
                            case "!getfileserver":
                                //////////////////////////////////////TODO
                                break;

                            default:
                                MessageBox.Show("Dispatcher: Неизвестная команда. " + command.ToString());
                                break;
                        }
                    }
                    catch (IOException ioex)
                    {
                        //tbLog.Text += Environment.NewLine + ioex.Message;
                        System.Diagnostics.Debug.WriteLine("Dispatcher :" + ioex.Message);
                    }
                }
            }
        }
        void AsyncGetFileList(string ip,int port)//асинхронное получение списка файлов
        {
            Thread t = new Thread(() =>
            {
                try
                {
                    TcpClient tcpClient = new TcpClient(ip, port);
                    NetStreamReaderWriter nsrw = new NetStreamReaderWriter(tcpClient.GetStream());
                    getFileList(nsrw);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Dispatcher. "+ex.Message);
                }

            });
            t.Start();
        }
        void getFileList(NetStreamReaderWriter stream)
        {
            NetCommand getFileListCmd = new NetCommand()
            {
                Ip = DispatcherIP.ToString(),
                //Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = 501,
                sender = "dispatcher",
                cmd = "!getfilelist",
                parameters = ""
            };
            stream.WriteCmd(getFileListCmd);

            NetCommand ansGetFilelist = stream.ReadCmd();

            if (ansGetFilelist.cmd != "!filelist")
                return;
            string filesStr = ansGetFilelist.parameters;

            string[] filesList = filesStr.Split(new char[]{'|'});
            lock (Files)
            {
                foreach (string f in filesList)
                {
                    Files.Add(new FileInfo() {Filename=f });
                }
                FilesListChanged();
            }
        }
        bool AddServer(string type, string ip, int port,out ServerInfo serv)//TODO добавление информации о меилслоте
        {
            string serverRecord = type+"_"+ip.ToString()+"_"+port;
            ServerInfo serverInfo = new ServerInfo();
            serv = serverInfo;
            serverInfo.Ip = ip;
            serverInfo.Port = port;
            TimeSpan fora = new TimeSpan(0, 1, 0);
            serverInfo.lastPingTime = DateTime.Now + fora;

            if (type == "msgserver")
            {
                lock (MsgServers)
                {
                    MsgServers.Add(serverInfo);
                }
                MsgServerListChanged();//обновление списка серверов на форме

                NetCommand serverregisteredCmd = new NetCommand()
                {
                    Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                    Port = 501,
                    sender = "dispatcher",
                    cmd = "!serverregistered",
                    parameters = string.Format("{0} {1}", ip, port)
                };
                SendCmdToAllServers(serverregisteredCmd);
                //TODO Запросить список клиентов, хотя не надо, т.к. на сервере еще нет клиентов
            }
            else if (type == "fileserver")
            {
                lock (FileServers)
                {
                    FileServers.Add(serverInfo);
                }
                FileServerListChanged();//обновление списка файлов на сервере
                AsyncGetFileList(ip, port);//получить список файлов
            }
            else
            {
                return false;
            }
            return true;
        }
        void SendServerList(Stream stream)//в ответ
        {
            NetStreamReaderWriter ns = new NetStreamReaderWriter(stream);
            string line = "";
            lock (MsgServers)
            {    
                for (int i = 0; i < MsgServers.Count; i++)
                {
                    line += MsgServers[i].Ip + " " + MsgServers[i].Port.ToString();//TODO посылать список каналов
                    if (i != MsgServers.Count - 1)
                        line += "|";
                }
            }
            NetCommand serverListCmd = new NetCommand()
            {
                Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = 501,
                sender = "dispatcher",
                cmd = "!serverlist",
                parameters = line
            };
            ns.WriteCmd(serverListCmd);
        }
        void SendClientList(Stream stream)//в ответ
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
            NetCommand clientListCmd = new NetCommand()
            {
                Ip = DispatcherIP.ToString(),
                //Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = 501,
                sender = "dispatcher",
                cmd = "!clientlist",
                parameters = line
            };
            ns.WriteCmd(clientListCmd);
        }
        void SendFileList(Stream stream)//в ответ
        {
            NetStreamReaderWriter ns = new NetStreamReaderWriter(stream);
            string line = "";
            for (int i = 0; i < Files.Count; i++)
            {
                line += Files[i].Filename;
                if (i != Files.Count - 1)
                    line += "|";
            }
            NetCommand fileListCmd = new NetCommand()
            {
                Ip = DispatcherIP.ToString(),//Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = 501,
                sender = "dispatcher",
                cmd = "!filelist",
                parameters = line
            };
            ns.WriteCmd(fileListCmd);
        }
        void SendCmdToAllServers(NetCommand cmd)
        {
            SendTextToAllServers(cmd.ToString());
        }
        void SendTextToAllServers(string line)//асинхронно
        {
            for (int i = 0; i < MsgServers.Count; i++)
            {
                try
                {
                    AsyncSendText(MsgServers[i].Ip, MsgServers[i].Port, line);
                }
                catch (IOException ioex)
                {
                    System.Diagnostics.Debug.AutoFlush = true;
                    System.Diagnostics.Debug.WriteLine("Dispatcher:" + ioex.Message + "похоже что один из сереров отключился или поломался!");
                }
            }
        }
        //void SendTextToAllServers(string line)
        //{
        //    for (int i = 0; i < MsgServers.Count;i++ )
        //    {
        //        try
        //        {
        //            lock (MsgServers[i].tcpClient)
        //            {
        //                NetStreamReaderWriter ns = new NetStreamReaderWriter(MsgServers[i].tcpClient.GetStream());
        //                ns.WriteLine(line);
        //            }
        //        }
        //        catch(IOException ioex)
        //        {
        //            System.Diagnostics.Debug.AutoFlush = true;
        //            System.Diagnostics.Debug.WriteLine("Dispatcher:"+ioex.Message+"похоже что один из сереров отключился или поломался!");
        //        }
        //    }
        //}
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
        void broadcastSelfInfo()//потоковая процедура для организации циклической посылки широковещательных сообщений
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.EnableBroadcast = true;

            IPAddress DispatcherIP = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];

            //IPAddress broadcast = IPAddress.Parse("192.127.150.255");
            //IPHostEntry host=Dns.GetHostByName(Dns.GetHostName());
            //IPAddress broadcast =  host.AddressList[0];

            //IPAddress tail = IPAddress.Parse("0.0.0.255");

            //broadcast.Address = broadcast.Address | tail.Address;
            //broadcast.Address = IPAddress.Loopback.Address;///

            byte[] sendbuf = Encoding.ASCII.GetBytes(DispatcherIP.ToString());
            IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, 11000);
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
