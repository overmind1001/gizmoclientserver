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
using System.IO;
using System.IO.Pipes;
using Dispatcher;
using System.Net;

namespace Client
{
    public partial class ClientForm : Form
    {
        //делегаты
        public delegate void WriteMesssageHandler(string mes);
        public WriteMesssageHandler WriteMessageD;

        public delegate void AddRemoveStringHandler(string str);
        public AddRemoveStringHandler AddManD;
        public AddRemoveStringHandler RemoveManD;
        public AddRemoveStringHandler AddFileD;
        public AddRemoveStringHandler RemoveFileD;

        public delegate void ClearHandler();
        public ClearHandler ClearPeopleD;
        public ClearHandler ClearFilesD;

        //TcpClient tcpClient;
        string serverIp;
        int serverPort;

        TcpListener TcpListener=null;
        int ListenerPort;

        string name;


        public ClientForm()
        {
            InitializeComponent();

            WriteMessageD += WriteMessage;
            AddManD += AddMan;
            RemoveManD += RemoveMan;
            AddFileD += AddFile;
            RemoveFileD += RemoveFile;
            ClearPeopleD += ClearPeople;
            ClearFilesD += ClearFiles;
        }

#region Вывод информации на GUI
        void WriteMessage(string message)
        {
            this.tbChat.Text += message + Environment.NewLine;
        }
        void AddMan(string man)
        {
            this.lbPeople.Items.Add(man);
        }
        void RemoveMan(string man)
        {
            this.lbPeople.Items.Remove(man);
        }
        void AddFile(string file)
        {
            lbFilesList.Items.Add(file);
        }
        void RemoveFile(string file)
        {
            lbFilesList.Items.Remove(file);
        }
        void ClearPeople()
        {
            lbPeople.Items.Clear();
        }
        void ClearFiles()
        {
            lbFilesList.Items.Clear();
        }
#endregion

        private void ПодключитьсяtoolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConnectForm cf = new ConnectForm();
            DialogResult dr = cf.ShowDialog();
            if (dr != DialogResult.OK)
            {//не подключились
                return;
            }
            ////////////тут загружаем список контактов и т.д.
            
            this.TcpListener= cf.tcpListener;
            ListenerPort = cf.MyPort;
            serverIp = cf.tbIp.Text;
            serverPort = Convert.ToInt32( cf.numericUpDownPort.Value);
            name = cf.tbName.Text;

            //запускаем поток для TcpListenera
            Thread tcpListenerThread = new Thread(TcpListenerThread);
            tcpListenerThread.Start();

            TcpClient tcpClient = cf.tcpClient;
            NetStreamReaderWriter nsrw = new NetStreamReaderWriter(tcpClient.GetStream());
            //NetworkStream ns = tcpClient.GetStream();

            //регистрируемся на сервере
            if (!registerMe(nsrw, cf.tbName.Text))
                return;
            //запускаем поток пинга
            AsyncStartPing();

            //загружаем список контактов

            //эти операции асинхронные
            AsyncGetContactListFromServer();
            //getContactListFromServer(ns);
            //загружаем список файлов
            AsyncGetFileListFromServer();
            //getFileListFromServer(ns);


            ////запускаем поток прослушки
            //Thread t = new Thread(Listen);
            //t.Start(tcpClient);
        }

        /// <summary>
        /// Поток для приема входящих соединений
        /// </summary>
        void TcpListenerThread()
        {
            while (true)
            {
                TcpClient tcpClient = TcpListener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(Listen,tcpClient);
            }
        }

        private void Listen(Object tcpCliento)//обработка команды
        {
            TcpClient tcpClient =(TcpClient) tcpCliento;
            //NetworkStream ns = tcpClient.GetStream();
            //StreamReader sr = new StreamReader(ns);
            NetStreamReaderWriter nsrw = new NetStreamReaderWriter(tcpClient.GetStream());

            // Тут косяк. По истечению срока соединение закрывается. И при дальнейших попытках получить стрим, падение

            //sr.BaseStream.ReadTimeout = 2000000;//20 сек ждем, затем снова пытаемся читать
            char[] sep = { ' ' };
            if (tcpClient.Connected)
            {
                try
                {
                    NetCommand command = nsrw.ReadCmd();
                    //string cmd = sr.ReadLine();
                    //string[] splited = cmd.Split(sep);

                    switch (command.cmd)
                    {
                        case "!message"://прием сообщения                            
                            //string message = cmd.Substring(splited[0].Length);
                            string message = command.parameters;
                            WriteMessageD(message);
                            break;
                        case "!clientregistered":
                            //string cl = splited[1];
                            string cl = command.parameters;
                            lock (lbPeople)
                            {
                                AddManD(cl);
                            }
                            break;
                        case "!clientunregistered":
                            //cl = splited[1];
                            cl = command.parameters;
                            lock (lbPeople)
                            {
                                RemoveManD(cl);
                            }
                            break;
                        case "!addfile":
                            //string file = splited[1];
                            string file = command.parameters;
                            lock (lbFilesList)
                            {
                                AddFileD(file);
                            }
                            break;
                        case "!deletefile":
                            //file = splited[1];
                            file = command.parameters;
                            lock (lbFilesList)
                            {
                                RemoveFileD(file);
                            }
                            break;
                        default:
                            MessageBox.Show("неизвестная команда!");
                            break;
                    }
                }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine("Client. за 20 сек сервер ничего не сказал");
                }                
            }
        }

        //регистрация на сервере (через диспетчер или напрямую)
        private bool registerMe(NetStreamReaderWriter ns,string name)
        {
            //StreamWriter sw = new StreamWriter(ns);
            //sw.AutoFlush = true;

            NetCommand registerCmd = new NetCommand()
            {
                Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = ListenerPort,
                sender = "client",//пока что безымянный
                cmd = "!register",
                parameters = name
            };
            //sw.WriteLine("!register "+name);
            ns.WriteCmd(registerCmd);

            //StreamReader sr = new StreamReader(ns);
            NetCommand ansRegisterCmd = ns.ReadCmd();
            //string answer = sr.ReadLine();

            return (ansRegisterCmd.cmd == "!registred");
        }
        //Пингование
        void AsyncStartPing()
        {
            Thread t = new Thread(() =>
                {
                    try
                    {
                        while (true)
                        {
                            TcpClient tcpClient = new TcpClient(serverIp, serverPort);
                            NetStreamReaderWriter nsrw = new NetStreamReaderWriter(tcpClient.GetStream());
                            nsrw.ReadTimeout = 5000;
                            NetCommand pingCmd = new NetCommand()
                            {
                                Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                                Port = ListenerPort,
                                sender = name,//пока что безымянный
                                cmd = "!ping",
                                parameters = ""
                            };
                            nsrw.WriteCmd(pingCmd);
                            NetCommand ansPing = nsrw.ReadCmd();
                            if (ansPing.cmd != "!pong")
                                MessageBox.Show("Client. В ответ на пинг пришла хрень");
                            tcpClient.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Client. Сервер не отвечает на пинг");
                    }
                });
            t.Start();
        }
        //получение контакт листа
        private void AsyncGetContactListFromServer()
        {
            Thread t = new Thread(() =>
                {
                    TcpClient tcp = new TcpClient(serverIp, serverPort);
                    NetworkStream ns = tcp.GetStream();
                    getContactListFromServer(ns);
                }
            );
            t.Start();
        }
        private void getContactListFromServer(NetworkStream ns)
        {
            //StreamWriter sw = new StreamWriter(ns);
            //sw.AutoFlush = true;
            NetStreamReaderWriter nsrw = new NetStreamReaderWriter(ns);
            NetCommand getClientListCmd = new NetCommand()
            {
                Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = ListenerPort,
                sender = name,//пока что безымянный
                cmd = "!getclientlist",
                parameters = name
            };
            nsrw.WriteCmd(getClientListCmd);

            //sw.WriteLine("!getclientlist");         //посылка команды

            //StreamReader sr = new StreamReader(ns);
            //string answer = sr.ReadLine();          //получение ответа
            NetCommand ansGetClientList = nsrw.ReadCmd();

            char[] sep = { '|' };
            string[] names = ansGetClientList.parameters.Split(sep);//получили массив имен

            
            lock (lbPeople)
            {
                ClearPeopleD();
                foreach (String s in names)
                {
                    AddManD(s);
                }
            }
        }
        //получение файл листа
        private void AsyncGetFileListFromServer()
        {
            Thread t = new Thread(() =>
            {
                TcpClient tcp = new TcpClient(serverIp, serverPort);
                NetworkStream ns = tcp.GetStream();
                getFileListFromServer(ns);
            }
            );
            t.Start();
        }
        private void getFileListFromServer(NetworkStream ns)
        {

            NetStreamReaderWriter nsrw = new NetStreamReaderWriter(ns);
            NetCommand getFileListCmd = new NetCommand()
            {
                Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = ListenerPort,
                sender = name,//пока что безымянный
                cmd = "!getfilelist",
                parameters = ""
            };
            nsrw.WriteCmd(getFileListCmd);

            //StreamWriter sw = new StreamWriter(ns);
            //sw.AutoFlush = true;
            //sw.WriteLine("!getfilelist");         //посылка команды

            //StreamReader sr = new StreamReader(ns);
            //string answer = sr.ReadLine();          //получение ответа
            NetCommand ansGetFileList = nsrw.ReadCmd();

            char[] sep = { '|' };
            string[] files = ansGetFileList.parameters.Split(sep);//получили массив имен

            lock (lbFilesList)
            {
                ClearFilesD();
                foreach (String s in files)
                {
                    AddFileD(s);
                }
            }
        }
        //получение адреса файл-сервера для заливки файла
        private string getFreeFileServer(NetworkStream ns)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
            sw.WriteLine("!getfreefileserver");         //посылка команды

            StreamReader sr = new StreamReader(ns);
            string answer = sr.ReadLine();          //получение ответа

            //char[] sep = { ' ' };
            //string[] addrOfFS = answer.Split(sep);//получили адрес свободного файл-сервера

            //return addrOfFS[1] + " " + addrOfFS[2];//ip port
            return answer;
        }
        //получение адреса файл-сервера, с которого можно скачать данный файл
        private string getFileServer(NetworkStream ns,string file)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
            sw.WriteLine("!getfileserver "+file);         //посылка команды

            StreamReader sr = new StreamReader(ns);
            string answer = sr.ReadLine();          //получение ответа

            //char[] sep = { ' ' };
            //string[] addrOfFS = answer.Split(sep);//получили адрес свободного файл-сервера

            //return addrOfFS[1] + " " + addrOfFS[2];//ip port

            return answer;
        }
        //выгрузка файла на сервер
        private void uploadFile(string ip,int port,string filePath,string fileName)
        {
            TcpClient cl = new TcpClient(ip, port);
            NetworkStream ns = cl.GetStream();
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
            sw.WriteLine("!uploadfile "+fileName+"10000");

            StreamReader sr = new StreamReader(ns);
            string pipeAddr = sr.ReadLine();            //получаем адрес пайпа

            NamedPipeClientStream nps = new NamedPipeClientStream(pipeAddr);
            nps.Connect();


//это надо будет запилить в отделный поток

            FileStream fs = new FileStream(filePath,FileMode.Open);
            
            //читаем файл и пишем в канал
            byte [] buf = new byte[10];
            int bytesCount;
            while((bytesCount=fs.Read(buf,0,buf.Length))>0)
            {
                nps.WaitForPipeDrain();
                nps.Write(buf, 0, bytesCount);
            }
            nps.Close();
            fs.Close();
        }
        //загрузка файла с сервера
        //это надо тоже запилить в отдельный поток
        private void downloadFile(string ip, int port, string Path, string fileName)
        {
            string name = Guid.NewGuid().ToString(); //может так пойдёт
            NamedPipeServerStream nps = new NamedPipeServerStream(name);            

            TcpClient cl = new TcpClient(ip, port);
            NetworkStream ns = cl.GetStream();
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
            sw.WriteLine("!downloadfile " + fileName +" "+ nps.GetImpersonationUserName());
            sw.Close();
            ns.Close();
            cl.Close();

            nps.WaitForConnection();

            FileStream fs = new FileStream(Path, FileMode.Create);

            byte[] buf = new byte[10];
            int byteCount;

            while ((byteCount = nps.Read(buf, 0, buf.Length)) > 0)
            {
                fs.Write(buf, 0, byteCount);
            }
            fs.Close();
            nps.Close();
        }
        //посылка сообщения
        private void AsyncSendMessage(string mes)
        {
            Thread t = new Thread(() =>
            {
                TcpClient tcp = new TcpClient(serverIp, serverPort);
                NetworkStream ns = tcp.GetStream();
                sendMessage(ns,mes);
            }
            );
            t.Start();
        }
        private void sendMessage(NetworkStream ns,string mes)
        {
            NetStreamReaderWriter nsrw = new NetStreamReaderWriter(ns);
            NetCommand messageCmd = new NetCommand()
            {
                Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = ListenerPort,
                sender = name,//пока что безымянный
                cmd = "!message",
                parameters = mes
            };
            nsrw.WriteCmd(messageCmd);
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (tbMessage.Text.Trim() == string.Empty)
                return;
            if (name == string.Empty)
                return;
            AsyncSendMessage(tbMessage.Text);
        }
    }
}
