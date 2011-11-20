﻿using System;
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
        private int                     m_ServerPort;               // Порта сервера
        private IPAddress               m_ServerIP;                 // IP сервера
        private List<ServerItem>        m_ServersList;              // Список серверов
        private List<ClientItem>        m_ClientsList;              // Список клиентов
        private Thread                  m_MainListenThread;         // Главный цикл прослушки порта
        private int                     m_MaxClientCount;           // Максимальное количество клиентов


        /// <summary>
        /// Конструктор
        /// </summary>
        public MsgServerForm()
        {
            InitializeComponent();
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

            m_ServerIP              = Dns.GetHostEntry("localhost").AddressList[0];

            m_ServersList           = new List<ServerItem>();
            m_ClientsList            = new List<ClientItem>();

            m_MaxClientCount        = 10;

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
            
            while(!HasFreePort)
            {
                try
                {
                    RandPort = Rand.Next(StartPort, EndPort);
                    Listener = new TcpListener(IPAddr, RandPort);

                    Listener.Start();
                    Listener.Stop();

                    HasFreePort = true;

                    UiWriteLog("Свободный порт найден! Порт: " + RandPort);
                    lblPort.Text = RandPort.ToString();
                }
                catch (Exception ex)
                {
                    UiWriteLog("Порт занят: " + RandPort);
                }
            }

            return RandPort;
        }

        /// <summary>
        /// Запускает сервер
        /// </summary>
        private void StartServer()
        {
            m_ServerPort = GetFreeListenerPort(out m_Listener, m_ServerIP);
            bool isIsolated = !ConnectToDispatcher();

            m_Listener.Start();

            m_MainListenThread = new Thread(TcpListenThreadFunc);
            m_MainListenThread.Start();

            lblStatus.Text = isIsolated ? "автономно" : "подключен";
        }

        /// <summary>
        /// Останавливает работу сервера
        /// </summary>
        private void StopServer()
        {
            m_Listener.Stop();
            Thread.Sleep(100);

            if (m_MainListenThread != null)
                m_MainListenThread.Abort();

            lstClients.Items.Clear();
            lstServers.Items.Clear();

            lblStatus.Text = "отключен";
            lblPort.Text = "-";
        }

        /// <summary>
        /// Подключиться к диспетчеру
        /// </summary>
        /// <returns>true - удачно</returns>
        private bool ConnectToDispatcher()
        {


             UiWriteLog("Не удалось подключиться к диспетчеру. " +
                            "Сервер работает в автономном режиме. " +
                            "Порт сервера: " + m_ServerPort.ToString());

            return false;
        }



        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Прослушка TCP

        /// <summary>
        /// Цикл прослушки новых tcp-соединений от клиентов. Работает в отдельном потоке
        /// </summary>
        private void TcpListenThreadFunc()
        {
            try
            {
                while (true)
                { 
                    TcpClient Tcp = m_Listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(TcpThreadFunc, Tcp);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(" > Server. Ошибка в ClientsListenThreadFunc: " + ex.Message);
            }
        }

        /// <summary>
        /// Реализует реакцию на команду из tcp-соединения
        /// </summary>
        /// <param name="Tcp">tcp-отправителя</param>
        private void TcpThreadFunc(object tcp)
        {
            TcpClient Tcp = (TcpClient)tcp;

            try
            {
                NetStreamReaderWriter Stream = new NetStreamReaderWriter(Tcp.GetStream());
                NetCommand Cmd = Stream.ReadCmd();

                UiWriteLog("От " + Cmd.sender + " поступила команда '" + Cmd.cmd + "'");

                // Если отправитель - другой сервер сообщений
                if (Cmd.sender == "msgserver")
                {
                }

                // Если отправитель - диспетчер
                else if (Cmd.sender == "dispatcher")
                {
                }

                // Если отправитель - клиент
                else
                {
                    ClientCommandHandler(Stream, Cmd);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(" > Server. Ошибка в TcpThreadFunc: " + ex.Message);
            }
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



        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Взаимодействие с клиентом

        /// <summary>
        /// Генирирует команду ответа на запрос "!who"
        /// </summary>
        /// <returns>команда</returns>
        private NetCommand AnsWho()
        {
            return CreateCommand("!messageserver", "Сервер сообщений");
        }

        /// <summary>
        /// Регистрирует клиента и генерирует команду ответа
        /// </summary>
        /// <param name="RegComand">команда запроса регистрации</param>
        /// <returns>команда</returns>
        private NetCommand AnsRegister(NetCommand RegCmd)
        {
            NetCommand RetCmd;

            // Если сервер переполнен
            if (GetClientCount() >= m_MaxClientCount)
                RetCmd = CreateCommand("!unregistred", "Сервер переполнен");

            // Если клиент с таким именем уже имеется
            else if (FindClient(RegCmd.sender) != null)
            {
                RetCmd = CreateCommand("!unregistred", "Клиент с таким именем уже зарегистрирован");
            }

            // Если все норм, регистрируем
            else if (AddClient(RegCmd.sender, RegCmd.Ip, RegCmd.Port))
                    RetCmd = CreateCommand("!registred", "Регистрация успешно завершена");
            
            // Если по какой то причине регистрация не удалась
            else
                    RetCmd = CreateCommand("!unregistred", "Регистрация не удалась");

            return RetCmd;
        }

        /// <summary>
        /// Генерирует команду в ответ на запрос списка клиентов
        /// </summary>
        /// <returns>команда</returns>
        private NetCommand AnsClientList()
        {
            return CreateCommand("!clientlist", GetClientList());
        }

        /// <summary>
        /// Генерирует команду в ответ на запрос списка файлов
        /// </summary>
        /// <returns>команда</returns>
        private NetCommand AnsFileList()
        {
            return CreateCommand("!filelist", "");
        }

        /// <summary>
        /// Генерирует команду в ответ на запрос свободного файлового сервера
        /// </summary>
        /// <returns>команда ответа</returns>
        private NetCommand AnsFreeFileServer()
        {
            return CreateCommand("!null", "");
        }

        /// <summary>
        /// Генерирует команду в ответ на запрос файлового сервера на котором расположен файл
        /// </summary>
        /// <param name="filename">имя файла</param>
        /// <returns>команда ответа</returns>
        private NetCommand AnsFileServer(string filename)
        {
            return CreateCommand("!null", "");
        }

        /// <summary>
        /// Обработчик клиентской команды
        /// </summary>
        /// <param name="Stream">читатель-писатель</param>
        /// <param name="Cmd">команда</param>
        private void ClientCommandHandler(NetStreamReaderWriter Stream, NetCommand Cmd)
        {
            switch (Cmd.cmd)
            {
                // Инициализация, или "кому я пишу?"
                case "!who":
                    {
                        Stream.WriteCmd(AnsWho());
                    }
                    break;

                // Регистрация клиента
                case "!register":
                    {
                        Stream.WriteCmd(AnsRegister(Cmd));
                    }
                    break;

                // Сообщение всем клиентам
                case "!message":
                    {
                        SendMsgToAllClients(Cmd.sender, Cmd.parameters);
                        Stream.WriteCmd(CreateCommand("!ok", ""));
                    }
                    break;

                // Запрос списка контактов
                case "!getclientlist":
                    {
                        Stream.WriteCmd(AnsClientList());
                    }
                    break;

                // Запрос списка файлов
                case "!getfilelist":
                    {
                        Stream.WriteCmd(AnsFileList());
                    }
                    break;

                // Запрос свободного файлового сервера для закачки
                case "!getfreefileserver":
                    {
                        Stream.WriteCmd(AnsFreeFileServer());
                    }
                    break;

                // Запрос файлового сервера для скачки
                case "!getfileserver":
                    {
                        Stream.WriteCmd(AnsFileServer(Cmd.parameters));
                    }
                    break;

                // Неизвестная команда
                default:
                    {
                        Stream.WriteCmd(CreateCommand("!unknowncommand",
                            "К сожалению сервер не знает такой команды"));
                        UiWriteLog("Такая команда неизвестна серверу!");
                    }
                    break;
            }
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
            return true;
        }



        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Взаимодействие с диспетчером



        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Взаимодействие с другими серверами сообщений



        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Для работы с формой (тут реализуется синхронизация потоков при обращении к контролам)

        public delegate void UiWriteLogHandler(string msg);
        public event UiWriteLogHandler UiWriteLogEvent;

        /// <summary>
        /// Пишет сообщение в лог
        /// </summary>
        /// <param name="text">текст сообщения</param>
        private void UiWriteLog(string msg)
        {
            lock (lstLog)
            {
                lstLog.Items.Add(" > " + msg);
            }
        }

        public delegate void UiInsertClientInListHandler(string name);
        public event UiInsertClientInListHandler UiInsertClientInListEvent;

        /// <summary>
        /// Добавляет имя клиента в лист
        /// </summary>
        /// <param name="name">имя клиента</param>
        private void UiInsertClientInList(string name)
        {
            lock (lstClients)
            {
                lstClients.Items.Add(name);
            }
        }

        public delegate void UiRemoveClientFromListHandler(string name);
        public event UiRemoveClientFromListHandler UiRemoveClientFromListEvent;

        /// <summary>
        /// Удаляет имя клиента из листа
        /// </summary>
        /// <param name="name">имя клиента</param>
        private void UiRemoveClientFromList(string name)
        {
            lock (lstClients)
            {
                lstClients.Items.Remove(name);
            }
        }

        public delegate void UiInsertServerInListHandler(string ip, int port);
        public event UiInsertServerInListHandler UiInsertServerInListEvent;

        /// <summary>
        /// Добавляет сервер в лист
        /// </summary>
        /// <param name="name">имя клиента</param>
        private void UiServerClientInList(string ip, int port)
        {
            lock (lstServers)
            {
                lstServers.Items.Add(ip + ":" + port);
            }
        }

        public delegate void UiRemoveServerFromListHandler(string ip, int port);
        public event UiRemoveServerFromListHandler UiRemoveServerFromListEvent;

        /// <summary>
        /// Удаляет сервер из листа
        /// </summary>
        /// <param name="name">имя клиента</param>
        private void UiRemoveServerFromList(string ip, int port)
        {
            lock (lstServers)
            {
                lstServers.Items.Remove(ip + ":" + port);
            }
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
            StartServer();
        }

        private void MsgServerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopServer();
        }
    }
}
