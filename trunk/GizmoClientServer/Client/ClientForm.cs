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
using System.Diagnostics;

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

        //адрес сервера
        string serverIp;    
        int serverPort;

        TcpListener TcpListener=null;
        int ListenerPort;   //прослушиваемый порт

        string name;        //имя клиента


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
        //из другого потока напрямую не вызывать
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

            //регистрируемся на сервере
            if (!registerMe( cf.tbName.Text))
                return;
            //запускаем поток пинга
            AsyncStartPing();

            //эти операции асинхронные
            //загружаем список контактов
            AsyncGetContactListFromServer();
            //загружаем список файлов
            AsyncGetFileListFromServer();
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
            NetStreamReaderWriter nsrw = new NetStreamReaderWriter(tcpClient.GetStream());
            if (tcpClient.Connected)
            {
                try
                {
                    NetCommand command = nsrw.ReadCmd();
                    switch (command.cmd)
                    {
                        case "!message"://прием сообщения                            
                            string message = command.parameters;
                            nsrw.WriteCmd(CreateStandardAnswer());//ответ
                            lock (tbChat)
                            {
                                WriteMessageD(message);//обновление уи
                            }
                            break;
                        case "!clientregistered":
                            nsrw.WriteCmd(CreateStandardAnswer());//ответ
                            string cl = command.parameters;
                            lock (lbPeople)
                            {
                                AddManD(cl);
                            }
                            break;
                        case "!clientunregistered":
                            nsrw.WriteCmd(CreateStandardAnswer());//ответ
                            cl = command.parameters;
                            lock (lbPeople)
                            {
                                RemoveManD(cl);
                            }
                            break;
                        case "!addfile":
                            nsrw.WriteCmd(CreateStandardAnswer());//ответ
                            string file = command.parameters;
                            lock (lbFilesList)
                            {
                                AddFileD(file);
                            }
                            break;
                        case "!deletefile":
                            nsrw.WriteCmd(CreateStandardAnswer());//ответ
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

        private NetCommand CreateStandardCommand(string Cmd,string Sender,string Parameters)//получение стандартной команды
        {
            NetCommand cmd = new NetCommand()
            {
                Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = ListenerPort,
                sender = Sender,
                cmd = Cmd,
                parameters = Parameters
            };
            return cmd;
        }
        private NetCommand CreateStandardAnswer()//создает команду стандартного ответа для подтверждения команд, не требующих ответа
        {
            return CreateStandardCommand("!ok", name, String.Empty);
        }
        //регистрация на сервере (через диспетчер или напрямую)
        private bool registerMe(string name)
        {
            TcpClient tcpClient = new TcpClient(serverIp, serverPort);
            NetStreamReaderWriter nsrw = new NetStreamReaderWriter(tcpClient.GetStream());
            try
            {
                NetCommand registerCmd = new NetCommand()
                {
                    Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                    Port = ListenerPort,
                    sender = name,//пока что безымянный
                    cmd = "!register",
                    parameters = name
                };
                nsrw.WriteCmd(registerCmd);

                NetCommand ansRegisterCmd = nsrw.ReadCmd();

                return (ansRegisterCmd.cmd == "!registred");
            }
            catch(Exception ex)
            {
                MessageBox.Show("Не удалось зарегистрироваться на сервере, т.к. сервер не отвечает");
                Debug.WriteLine("Client. ClientForm. registerMe "+ex.Message);
                return false;
            }
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
                            Thread.Sleep(1000);//задержка 
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
                    try
                    {
                        getContactListFromServer(ns);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("При получении списка контактов произошла ошибка!");
                        Debug.WriteLine("Client. ClientForm. AsyncGetContactListFromServer "+ex.Message);
                    }
                }
            );
            t.Start();
        }
        private void getContactListFromServer(NetworkStream ns)
        {
            NetStreamReaderWriter nsrw = new NetStreamReaderWriter(ns);
            NetCommand getClientListCmd = new NetCommand()
            {
                Ip = Dns.GetHostAddresses(Dns.GetHostName())[0].ToString(),
                Port = ListenerPort,
                sender = name,
                cmd = "!getclientlist",
                parameters = ""
            };
            nsrw.WriteCmd(getClientListCmd);
            NetCommand ansGetClientList = nsrw.ReadCmd();//получение ответа
            string[] names = ansGetClientList.parameters.Split(new char[]{'|'});//получили массив имен
            lock (lbPeople)//работа с Ui
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
                try
                {
                    getFileListFromServer(ns);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("При получении списка файлов произошла ошибка!");
                    Debug.WriteLine("Client. ClientForm. AsyncGetFileListFromServer " + ex.Message);
                }
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
                sender = name,
                cmd = "!getfilelist",
                parameters = ""
            };
            nsrw.WriteCmd(getFileListCmd);
            NetCommand ansGetFileList = nsrw.ReadCmd();//ответ
            string[] files = ansGetFileList.parameters.Split(new char[]{'|'});//получили массив имен

            lock (lbFilesList)//Ui
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
                NetStreamReaderWriter nsrw = new NetStreamReaderWriter(tcp.GetStream());
                try
                {
                    sendMessage(nsrw, mes);
                    NetCommand ansMessage = nsrw.ReadCmd();
                    if (ansMessage.cmd != "!ok")
                        throw new Exception(" Вместо подтверждения получено что-то другое");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Client. ClientForm. AsyncSendMessage."+ex.Message);
                }
            }
            );
            t.Start();
        }
        private void sendMessage(NetStreamReaderWriter nsrw, string mes)
        {
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
