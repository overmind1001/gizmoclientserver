using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using Dispatcher;

namespace MsgServer
{
    public partial class MsgServerForm : Form
    {
        private TcpListener             m_Listener;                 // Tcp слушатель

        private int                     m_DispatcherPort;           // Порт диспетчера
        private string                  m_DispatcherIP;             // IP диспетчера
        private bool                    m_IsIsolated;               // Подключены ли к диспетчеру

        private int                     m_ServerPort;               // Порта сервера
        private IPAddress               m_ServerIP;                 // IP сервера

        private List<ServerItem>        m_ServersList;              // Список серверов
        private List<ClientItem>        m_ClientsList;              // Список клиентов
            
        private int                     m_MaxClientCount;           // Максимальное количество клиентов

        private Thread                  m_TcpListenThread;          // Поток прослушки TCP
        private Thread                  m_DispatcherPingThread;     // Поток пинга диспетчера 


        /// <summary>
        /// Конструктор
        /// </summary>
        public MsgServerForm()
        {
            InitializeComponent();
            InitDelegates();
            Init();
        }



        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Подключение и настройка

        /// <summary>
        /// Проводит начальную инициализацию
        /// </summary>
        private void Init()
        {
            Debug.AutoFlush         = true;//для отладки

            m_ServerIP              = IPAddress.Parse("178.47.93.187");

            m_ServersList           = new List<ServerItem>();
            m_ClientsList           = new List<ClientItem>();

            m_MaxClientCount        = 10;

            m_DispatcherPort        = 501;

            ThreadPool.SetMaxThreads(10, 10);
        }

        /// <summary>
        /// Получает свободный порт и создает для него TcpListener
        /// </summary>
        /// <param name="Listener">[out] создаваемый TcpListener</param>
        /// <param name="IPAddr">ip адрес хоста</param>
        /// <returns>номер порта</returns>
        private int GetFreeListenerPort(out TcpListener Listener, IPAddress IPAddr)
        {
            int     StartPort       = 49160;
            int     EndPort         = 65534;
            int     RandPort        = 0;
            bool    HasFreePort     = false;
            Random  Rand            = new Random();

            Listener = new TcpListener(0);

            UiWriteLog("Инициализация...");
            UiWriteLog("IP-адрес: " + m_ServerIP.ToString());
            
            while(!HasFreePort)
            {
                try
                {
                    RandPort = Rand.Next(StartPort, EndPort);
                    Listener = new TcpListener(IPAddr, RandPort);

                    Listener.Start();
                    Listener.Stop();

                    HasFreePort = true;

                    UiWriteLog("Порт: " + RandPort);
                    lblPort.Text = RandPort.ToString();
                }
                catch (Exception ex)
                {
                    UiWriteLog("Порт занят: " + RandPort);
                }
            }

            UiWriteLog("");

            return RandPort;
        }

        /// <summary>
        /// Запускает сервер
        /// </summary>
        private void StartServer()
        {
            ClearLog();

            m_ServerPort = GetFreeListenerPort(out m_Listener, m_ServerIP);
            m_IsIsolated = !ConnectToDispatcher();

            m_Listener.Start();
            Thread.Sleep(100);

            m_TcpListenThread = new Thread(TcpListenThreadFunc);
            m_TcpListenThread.Start();

            lblStatus.Text = m_IsIsolated ? "автономно" : "подключен";
            lblIP.Text = m_ServerIP.ToString();

            поднятьToolStripMenuItem.Enabled = false;
            уронитьToolStripMenuItem.Enabled = true;
        }

        /// <summary>
        /// Останавливает работу сервера
        /// </summary>
        private void StopServer()
        {
            m_Listener.Stop();
            Thread.Sleep(100);

            if (m_TcpListenThread != null)
                m_TcpListenThread.Abort();

            if (m_DispatcherPingThread != null)
                m_TcpListenThread.Abort();


            m_IsIsolated = DisconnectToDispatcher();
            
            lstClients.Items.Clear();
            lstServers.Items.Clear();

            lblStatus.Text = "отключен";
            lblPort.Text = "-";
            lblIP.Text = "-";

            поднятьToolStripMenuItem.Enabled = true;
            уронитьToolStripMenuItem.Enabled = false;

            ClearLog();
        }

        /// <summary>
        /// Подключиться к диспетчеру
        /// </summary>
        /// <returns>true - удачно</returns>
        private bool ConnectToDispatcher()
        {
            bool retn = false;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint endpoint = new IPEndPoint(m_ServerIP, 11000);
            byte[] buf = new byte[1000];

            EndPoint ep = (EndPoint)endpoint;
            try
            {
                socket.Bind(ep);
                socket.ReceiveTimeout = 3000;
                int recv = socket.ReceiveFrom(buf, ref ep);
                m_DispatcherIP = Encoding.ASCII.GetString(buf, 0, recv);

                if (SendCommand("!who", "Ты кто?", m_DispatcherIP, m_DispatcherPort).cmd != "!dispatcher")
                {
                    UiWriteLog("Диспетчер не отзывается на команду '!who'");
                    retn = false;
                }
                else if (SendCommand("!register", "Зарегай!", m_DispatcherIP, m_DispatcherPort).cmd != "!registered")
                {
                    UiWriteLog("Не удалась регистрация на диспетчере");
                    retn = false;
                }
                else
                {
                    m_DispatcherPingThread = new Thread(DispatcherPingThreadFunc);
                    m_DispatcherPingThread.Start();

                    string ServerList = 
                        SendCommand("!getserverlist", "Дай другие сервера!", m_DispatcherIP, m_DispatcherPort).parameters;
                    ParseServerList(ServerList);

                    retn = true;
                }
            }
            catch (Exception ex)
            {
                Debug.Write(" > Ошибка в ConnectToDispatcher: " + ex.Message);
            }

            if (!retn)
            {
                UiWriteLog(""); 
                UiWriteLog("Не удалось подключиться к диспетчеру");
                UiWriteLog("Сервер работает в автономном режиме");
                UiWriteLog(""); 
            }

            return retn;
        }

        /// <summary>
        /// Отключается от диспетчера
        /// </summary>
        /// <returns>true - удачно</returns>
        private bool DisconnectToDispatcher()
        {

            return true;
        }



        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Общие функции для работы с командами

        /// <summary>
        /// Создает команду
        /// </summary>
        /// <param name="cmd">текст команды</param>
        /// <param name="param">параметры команды</param>
        /// <returns>созданная команда</returns>
        private NetCommand CreateCommand(string cmd, string param)
        {
            NetCommand Cmd = new NetCommand()
            {
                Ip = m_ServerIP.ToString(),
                Port = m_ServerPort,
                sender = "msgserver",
                cmd = cmd,
                parameters = param
            };
            return Cmd;
        }

        /// <summary>
        /// Отослать команду
        /// </summary>
        /// <param name="cmd">команда</param>
        /// <param name="param">параметры</param>
        /// <returns>ответ</returns>
        private NetCommand SendCommand(string cmd, string param, string ip, int port)
        {
            NetCommand retn = null;

            try
            {
                TcpClient tcpclient = new TcpClient(ip, port);
                NetStreamReaderWriter Stream = new NetStreamReaderWriter(tcpclient.GetStream());
                Stream.WriteCmd(CreateCommand(cmd, param));
                retn = Stream.ReadCmd();
            }
            catch (Exception ex)
            {
                Debug.Write(" > Ошибка в SendCommandToDispatcher: " + ex.Message);
            }

            return retn;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Для работы со списком клиентов (тут реализуется синхронизация потоков при обращении к списку клиентов)

        /// <summary>
        /// Находит клиента по имени в списке
        /// </summary>
        /// <param name="name">имя клиента</param>
        /// <returns>экземпляр класса клиента, если не нашел - null</returns>
        private ClientItem FindClient(string name)
        {
            lock (m_ClientsList)
            {
                return m_ClientsList.Find((ClientItem it) => { return it.GetName() == name; });//так круче


                for (int i = 0; i < m_ClientsList.Count; i++)
                {
                    if (m_ClientsList[i].GetName() == name)
                        return m_ClientsList[i];
                }
                return null;
            }
        }

        /// <summary>
        /// Добавляет клиента
        /// </summary>
        /// <param name="name">имя</param>
        /// <param name="ip">адрес</param>
        /// <param name="port">порт</param>
        /// <returns>true - удачно</returns>
        private bool AddClient(string name, string ip, int port)
        {
            lock (m_ClientsList)
            {
                m_ClientsList.Add(new ClientItem(name, ip, port));
            }
            UiInsertClientInList(name);
            return true;
        }

        /// <summary>
        /// Удаляет клиента
        /// </summary>
        /// <param name="name">имя</param>
        /// <returns>true - удачно</returns>
        private bool DeleteClient(string name)
        {
            ClientItem Client = FindClient(name);
            lock (m_ClientsList)
            {
                m_ClientsList.Remove(Client);
            }
            UiRemoveClientFromList(name);
            return true;
        }

        /// <summary>
        /// Возвращает строку - список клиентов
        /// </summary>
        /// <returns>список клиентов</returns>
        private string GetClientList()
        {
            string Clients = "";
            lock (m_ClientsList)
            {
                if (m_ClientsList.Count == 0)
                    return Clients;

                Clients += m_ClientsList[0].GetName();

                for (int i = 1; i < m_ClientsList.Count; i++)
                {
                    Clients += "|" + m_ClientsList[i].GetName();
                }
            }
            return Clients;
        }

        /// <summary>
        /// Возвращает количество клиентов в списке
        /// </summary>
        /// <returns>количество клиентов</returns>
        private int GetClientCount()
        {
            int Count;
            lock (m_ClientsList)
            {
                Count = m_ClientsList.Count;
            }
            return Count;
        }

        /// <summary>
        /// Рассылает сообщение всем клиентам из списка
        /// </summary>
        /// <param name="Sender">отправитель сообщения</param>
        /// <param name="Msg">текст сообщения</param>
        private void SendMsgToAllClients(string Sender, string Msg)
        {
            lock (m_ClientsList)
            {
                for (int i = 0; i < m_ClientsList.Count; i++)
                {
                    string                  ClientIP    = m_ClientsList[i].GetIP();
                    int                     ClientPort  = m_ClientsList[i].GetPort();
                    TcpClient               Tcp         = new TcpClient(ClientIP, ClientPort);
                    NetStreamReaderWriter   Stream      = new NetStreamReaderWriter(Tcp.GetStream());
                    
                    Stream.WriteCmd(CreateCommand("!message", Sender + ": " + Msg));
                }
            }
            UiWriteLog(Sender + ": " + Msg);
        }



        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Для работы со списком серверов (тут реализуется синхронизация потоков при обращении к списку серверов)

        /// <summary>
        /// Находит сервер по ip и порту
        /// </summary>
        /// <param name="ip">адрес</param>
        /// <param name="port">порт</param>
        /// <returns>экземпляр класса сервера</returns>
        private ServerItem FindServer(string ip, int port)
        {
            lock (m_ServersList)
            {
                for (int i = 0; i < m_ServersList.Count; i++)
                {
                    if (m_ServersList[i].GetIP() == ip && 
                        m_ServersList[i].GetPort() == port)
                        return m_ServersList[i];
                }
                return null;
            }
        }

        /// <summary>
        /// Добавляет сервер
        /// </summary>
        /// <param name="ip">адрес</param>
        /// <param name="port">порт</param>
        /// <returns>true - удачно</returns>
        private bool AddServer(string ip, int port)
        {
            lock (m_ServersList)
            {
                m_ServersList.Add(new ServerItem(ip, port));
            }
            UiInsertServerInList(ip, port);
            return true;
        }

        /// <summary>
        /// Удаляет сервер
        /// </summary>
        /// <param name="name">имя</param>
        /// <returns>true - удачно</returns>
        private bool DeleteServer(string ip, int port)
        {
            ServerItem Server = FindServer(ip, port);
            lock (m_ServersList)
            {
                m_ServersList.Remove(Server);
            }
            UiRemoveServerFromList(ip, port);
            return true;
        }

        /// <summary>
        /// Парсит строку с серверами и добавляет их в список
        /// </summary>
        /// <param name="serverlist">строка серверов</param>
        private void ParseServerList(string serverlist)
        {
            if (serverlist == "")
                return;

            lock(m_ServersList)
            {
                string[] servers = serverlist.Split(new char[]{'|'});
                for(int i = 0; i < servers.Length; i++)
                {
                    string[] param = servers[i].Split(new char[] { ' ' });
                    if (param.Length < 2)
                        continue;

                    string ip = param[0];
                    string port = param[1];
                    ServerItem Item = m_ServersList.Find(
                        (ServerItem it) => { return (it.GetIP() == ip) && (it.GetPort().ToString() == port); });

                    if (Item == null)
                    {
                        m_ServersList.Add(new ServerItem(ip, int.Parse(port)));
                        UiInsertServerInList(ip, int.Parse(port));
                    }
                }
            }                
        }

        /// <summary>
        /// Рассылает сообщение всем серверам из списка
        /// </summary>
        /// <param name="Sender">отправитель сообщения</param>
        /// <param name="Msg">текст сообщения</param>
        private void SendMsgToAllServers(string Sender, string Msg)
        {
            lock (m_ServersList)
            {
                for (int i = 0; i < m_ServersList.Count; i++)
                {
                    string ServerIP = m_ServersList[i].GetIP();
                    int ServerPort = m_ServersList[i].GetPort();

                    if ((ServerIP == m_ServerIP.ToString()) && (ServerPort == m_ServerPort))
                        continue;

                    TcpClient Tcp = new TcpClient(ServerIP, ServerPort);
                    NetStreamReaderWriter Stream = new NetStreamReaderWriter(Tcp.GetStream());

                    Stream.WriteCmd(CreateCommand("!message", Sender + ": " + Msg));
                }
            }
        }



        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Для работы с формой (тут реализуется синхронизация потоков при обращении к контролам)

        public delegate void                WriteLogHandler(string msg);
        public delegate void                InsertClientInListHandler(string name);
        public delegate void                RemoveClientFromListHandler(string name);
        public delegate void                InsertServerInListHandler(string ip, int port);
        public delegate void                ClearLogHandler();
        public delegate void                RemoveServerFromListHandler(string ip, int port);

        public ClearLogHandler              ClearLogD;
        public WriteLogHandler              WriteLogD;
        public InsertClientInListHandler    InsertClientInListD;
        public RemoveClientFromListHandler  RemoveClientFromListD;
        public InsertServerInListHandler    InsertServerInListD;
        public RemoveServerFromListHandler  RemoveServerFromListD;

    
        /// <summary>
        /// Для делегата
        /// </summary>
        /// <param name="msg"></param>
        private void WriteLog(string msg)
        {
            lock (lstLog)
            {
                lstLog.Items.Add(" > " + msg);
            }
        }

        /// <summary>
        /// Пишет сообщение в лог
        /// </summary>
        /// <param name="msg">текст сообщения</param>
        private void UiWriteLog(string msg)
        {
            lstLog.Invoke(WriteLogD, new object[] { msg });
        }

        /// <summary>
        /// Для делегата
        /// </summary>
        private void ClearLog()
        {
            lock (lstLog)
            {
                lstLog.Items.Clear();
            }
        }

        /// <summary>
        /// Чистит лог
        /// </summary>
        private void UiClearLog()
        {
            lstLog.Invoke(ClearLogD);
        }

        /// <summary>
        /// Для делегата
        /// </summary>
        /// <param name="name"></param>
        private void InsertClientInList(string name)
        {
            lock (lstClients)
            {
                lstClients.Items.Add(name);
            }
        }

        /// <summary>
        /// Добавляет клиента в список
        /// </summary>
        /// <param name="name">имя</param>
        private void UiInsertClientInList(string name)
        {
            lstClients.Invoke(InsertClientInListD, new object[] { name });
        }

        /// <summary>
        /// Для делегата
        /// </summary>
        /// <param name="name"></param>
        private void RemoveClientFromList(string name)
        {
            lock (lstClients)
            {
                lstClients.Items.Remove(name);
            }
        }

        /// <summary>
        /// Удаляет клиента из списка
        /// </summary>
        /// <param name="name"></param>
        private void UiRemoveClientFromList(string name)
        {
            lstClients.Invoke(RemoveClientFromListD, new object[] { name });
        }

        /// <summary>
        /// Для делегата
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        private void InsertServerInList(string ip, int port)
        {
            lock (lstServers)
            {
                lstServers.Items.Add(ip + ":" + port);
            }
        }

        /// <summary>
        /// Добавляет сервер в список
        /// </summary>
        /// <param name="ip">адрес</param>
        /// <param name="port">порт</param>
        private void UiInsertServerInList(string ip, int port)
        {
            lstServers.Invoke(InsertServerInListD, new object[] { ip, port });
        }

        /// <summary>
        /// Для делегата
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        private void RemoveServerFromList(string ip, int port)
        {
            lock (lstServers)
            {
                lstServers.Items.Remove(ip + ":" + port);
            }
        }

        /// <summary>
        /// Удаляет сервер из списка
        /// </summary>
        /// <param name="ip">адрес</param>
        /// <param name="port">порт</param>
        private void UiRemoveServerFromList(string ip, int port)
        {
            lstServers.Invoke(RemoveServerFromListD, new object[] { ip, port });
        }

        /// <summary>
        /// Инициализирует все делегаты
        /// </summary>
        private void InitDelegates()
        {
            WriteLogD               += WriteLog;
            ClearLogD               += ClearLog;
            InsertClientInListD     += InsertClientInList;
            RemoveClientFromListD   += RemoveClientFromList;
            InsertServerInListD     += InsertServerInList;
            RemoveServerFromListD   += RemoveServerFromList;
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // События формы

        private void поднятьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartServer();
        }

        private void уронитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        private void MsgServerForm_Load(object sender, EventArgs e)
        {
            //StartServer();
        }

        private void MsgServerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopServer();
            Process.GetCurrentProcess().Kill();
        }

        private void задатьIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            IpConfig ipForm = new IpConfig();
            if (ipForm.ShowDialog() == DialogResult.OK)
            {
                StopServer();
                m_ServerIP = ipForm.Ip;
                StartServer();
            } 
        }
    }
}
