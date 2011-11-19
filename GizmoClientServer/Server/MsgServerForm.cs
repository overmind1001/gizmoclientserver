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
        private int                     m_ServerPort;               // Порта сервера
        private IPAddress               m_ServerIP;                 // IP сервера
        private List<ServerItem>        m_ServersList;              // Список серверов
        private List<ClientItem>        m_ClientList;               // Список клиентов
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



        /// <summary>
        /// Проводит начальную инициализацию
        /// </summary>
        private void Init()
        {
            Debug.AutoFlush         = true;//для отладки

            m_ServerIP              = Dns.GetHostEntry("localhost").AddressList[0];
            m_ServerPort            = GetFreeListenerPort(m_Listener, m_ServerIP);

            m_ServersList           = new List<ServerItem>();
            m_ClientList            = new List<ClientItem>();

            m_MaxClientCount        = 10;

            ThreadPool.SetMaxThreads(10, 10);
        }



        /// <summary>
        /// Получает свободный порт и создаем для него tcp-слушаетеля
        /// </summary>
        /// <param name="Listener">создаваемый слушаетль tcp</param>
        /// <param name="IPAddr">ip адрес хоста</param>
        /// <returns>номер порта</returns>
        private int GetFreeListenerPort(TcpListener Listener, IPAddress IPAddr)
        {
            int     StartPort       = 49160;
            int     EndPort         = 65534;
            int     RandPort        = 0;
            bool    HasFreePort     = false;
            Random  Rand            = new Random();
            
            while(!HasFreePort)
            {
                try
                {
                    RandPort = Rand.Next(StartPort, EndPort);
                    Listener = new TcpListener(IPAddr, RandPort);

                    Listener.Start();
                    Listener.Stop();

                    HasFreePort = true;

                    Debug.WriteLine(" > Server. Свободный порт найден! Порт: " + RandPort);
                    lblPort.Text = RandPort.ToString();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(" > Server. Порт занят: " + RandPort);
                }
            }

            return RandPort;
        }



        /// <summary>
        /// Главный цикл tcp-слушателя. Работает в отдельном потоке
        /// </summary>
        private void MainListenThreadFunc()
        {
            while (true)
            {
                TcpClient Tcp = m_Listener.AcceptTcpClient();
                ThreadPool.QueueUserWorkItem(CommandThreadFunc, Tcp);
            }
        }



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
        /// Генирирует команду ответа на запрос "!who"
        /// </summary>
        /// <returns></returns>
        private NetCommand AnsWho()
        {
            return CreateCommand("!messageserver", "Сервер сообщений");
        }



        /// <summary>
        /// Регистрирует клиента и генерирует команду ответа
        /// </summary>
        /// <param name="RegComand">команда запроса регистрации</param>
        /// <returns>команда ответа</returns>
        private NetCommand AnsRegister(NetCommand RegCmd)
        {
            NetCommand RetCmd;

            if (m_ClientList.Count >= m_MaxClientCount)
                RetCmd = CreateCommand("!unregistred", "Сервер переполнен");

            if (FindClient(RegCmd.sender) != null)
            {
                RetCmd = CreateCommand("!unregistred", "Клиент с таким именем уже зарегистрирован");
            }
            else
            {
                if(AddClient(RegCmd.sender, RegCmd.Ip, RegCmd.Port))
                    RetCmd = CreateCommand("!registred", "Регистрация успешно завершена");
                else
                    RetCmd = CreateCommand("!unregistred", "Регистрация не удалась");
            }

            return RetCmd;
        }



        /// <summary>
        /// Рассылает сообщение всем клиентам из списка
        /// </summary>
        /// <param name="Sender">отправитель сообщения</param>
        /// <param name="Msg">текст сообщения</param>
        private void SendMsgToAllClients(string Sender ,string Msg)
        {

        }


        private NetCommand AnsClientList()
        {
            return null;
        }



        private NetCommand AnsFileList()
        {
            return null;
        }



        private NetCommand AnsFreeFileServer()
        {
            return null;
        }


        private NetCommand AnsFileServer(string filename)
        {
            return null;
        }

        /// <summary>
        /// Реализует единицу взаимодействия клиентов
        /// </summary>
        /// <param name="Tcp">tcp клиента</param>
        private void CommandThreadFunc(object tcp)
        {
            TcpClient Tcp = (TcpClient)tcp;

            try
            {
                NetStreamReaderWriter Stream = new NetStreamReaderWriter(Tcp.GetStream());
                NetCommand Cmd = Stream.ReadCmd();

                switch (Cmd.cmd)
                {
                    // Инициализация получателя
                    case "!who":
                        {
                            Stream.WriteCmd(AnsWho());
                        }
                        break;

                    // Регистрация этого клиента
                    case "!register":
                        {
                            Stream.WriteCmd(AnsRegister(Cmd));
                        }
                        break;

                    // Сообщение всем от этого клиента
                    case "!message":
                        {
                            SendMsgToAllClients(Cmd.sender, Cmd.parameters);
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

                    default:
                        {
                            Stream.WriteCmd(CreateCommand("!unknowncommand", 
                                "К сожалению сервер не знает такой команды"));
                        }
                        break;
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(" > Server. Ошибка: " + ex.Message);
            }
        }



        /// <summary>
        /// Запускает сервер
        /// </summary>
        private void StartServer()
        {
            bool isIsolated = !ConnectToDispatcher();

            m_MainListenThread = new Thread(MainListenThreadFunc);
            m_MainListenThread.Start();

            lblStatus.Text = isIsolated ? "автономно" : "подключен";
        }



        /// <summary>
        /// Останавливает работу сервера
        /// </summary>
        private void StopServer()
        {
            if (m_MainListenThread != null)
                m_MainListenThread.Abort();

            m_Listener.Stop();

            lstClients.Items.Clear();
            lstServers.Items.Clear();

            lblStatus.Text = "отключен";
        }



        /// <summary>
        /// Подключиться к диспетчеру
        /// </summary>
        /// <returns>true - удачно</returns>
        private bool ConnectToDispatcher()
        {
            MessageBox.Show("Не удалось подключиться к диспетчеру.\n" +
                            "Сервер работает в автономном режиме.\n" +
                            "Порт сервера: " + m_ServerPort.ToString());
            return false;
        }


        /// <summary>
        /// Находит клиента по имени в списке
        /// </summary>
        /// <param name="name">имя клиента</param>
        /// <returns>экземпляр класса клиента</returns>
        private ClientItem FindClient(string name)
        {
            lock (m_ClientList)
            {
                for (int i = 0; i < m_ClientList.Count; i++)
                {
                    if (m_ClientList[i].GetName() == name)
                        return m_ClientList[i];
                }
                return null;
            }
        }

        /// <summary>
        /// Добавить нового клиента
        /// </summary>
        /// <param name="tcp">tcp клиента</param>
        private bool AddClient(string name, string ip, int port)
        {
            lock (m_ClientList)
            {
                m_ClientList.Add(new ClientItem(name, ip, port));
            }
            return true;
        }


        private bool DeleteClient(string name)
        {
            ClientItem Client = FindClient(name);
            lock (m_ClientList)
            {
                m_ClientList.Remove(Client);
            }
            return true;
        }

        ///// <summary>
        ///// Послать текстовое сообщение всем клиентам
        ///// </summary>
        ///// <param name="name">имя отправителя</param>
        ///// <param name="text">текст сообщения</param>
        ///// <returns></returns>
        //private bool SendTextToAll(string name, string text)
        //{
        //    lock (сlientList)
        //    {
        //        for (int i = 0; i < сlientList.Count; i++)
        //        {
        //            сlientList[i].SendText(name, text);
        //        }
        //    }
        //    WriteLog(" > " + name + ": " + text);
        //    return true;
        //}

        ///// <summary>
        ///// Возвращает список клиентов
        ///// </summary>
        ///// <returns></returns>
        //private string GetClientList()
        //{
        //    string List = "";
        //    lock (сlientList)
        //    {
        //        for (int i = 0; i < сlientList.Count; i++)
        //        {
        //            if (!сlientList[i].IsRegister())
        //                continue;

        //            if (List != "") 
        //                List += "|";
        //            List += сlientList[i].GetName();
        //        }
        //    }
        //    return List;
        //}

        ///// <summary>
        ///// Возвращает список файлов
        ///// </summary>
        ///// <returns>список файлов</returns>
        //private string GetFileList()
        //{
        //    return "";
        //}

        ///// <summary>
        ///// Возвращет имя сервера, на котором содержится файл
        ///// </summary>
        ///// <returns>имя сервера</returns>
        //private string GetFileServer(string filename)
        //{
        //    return "";
        //}


        ///// <summary>
        ///// Возвращает имя свободного файлового сервера
        ///// </summary>
        ///// <returns></returns>
        //private string GetFreeFileServer()
        //{
        //    return "";
        //}

        ///// <summary>
        ///// Проверяет на наличие в списке клиентов клиента, зарегистрированного по данному имени
        ///// </summary>
        ///// <param name="name">проверяемое имя</param>
        ///// <returns>true - клиент с таким именем уже зарегистрирован</returns>
        //private bool IsContained(string name)
        //{
        //    lock (сlientList)
        //    {
        //        for (int i = 0; i < сlientList.Count; i++)
        //        {
        //            if (сlientList[i].GetName() == name && сlientList[i].IsRegister())
        //                return true;
        //        }
        //    }
        //    return false;
        //}


        //private void WriteLog(string text)
        //{
        //    lock (lstLog)
        //    {
        //        lstLog.Items.Add(text);
        //    }
        //}
    }
}
