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

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Запуск и настройка сервера


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

            m_MainListenThread      = new Thread(MainListenThreadFunc);
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

        }


        /// <summary>
        /// Запускает сервер
        /// </summary>
        private void StartServer()
        {
        }

        /// <summary>
        /// Останавливает работу сервера
        /// </summary>
        private void StopServer()
        {
        }


        ///// <summary>
        ///// Подключиться к диспетчеру
        ///// </summary>
        ///// <returns>удачно ли</returns>
        //private bool ConnectToDispatcher()
        //{
        //    MessageBox.Show("Не удалось подключиться к диспетчеру.\n" +
        //                    "Сервер работает в автономном режиме.\n" +
        //                    "Порт сервера: " + serverPort.ToString());
        //    return false;
        //}



        ///// <summary>
        ///// Отключиться от диспетчера
        ///// </summary>
        //private void DisconnectToDispatcher()
        //{
        //}



        ///// <summary>
        ///// Запустить сервер
        ///// </summary>
        //private void StartServer()
        //{
        //    if (state == ServerState.Running)
        //        return;

        //    state = ServerState.Running;

        //    isIsolated = !ConnectToDispatcher();

        //    mainMsgThread = new Thread(MainMsgFunc);
        //    mainMsgThread.Start();

        //    lblStatus.Text = isIsolated ? "автономно" : "подключен";
        //}

        ///// <summary>
        ///// Отключить сервер
        ///// </summary>
        //private void StopServer()
        //{
        //    if (state == ServerState.Stopped)
        //        return;

        //    state = ServerState.Stopped;

        //    isIsolated = true;
        //    DisconnectToDispatcher();

        //    if (mainMsgThread != null)
        //        mainMsgThread.Abort();

        //    listener.Stop();

        //    lstClients.Items.Clear();
        //    lstServers.Items.Clear();

        //    lblStatus.Text = "отключен";
        //}



        ///// <summary>
        ///// Добавить нового клиента
        ///// </summary>
        ///// <param name="tcp">tcp клиента</param>
        //private void AddClient(TcpClient tcp)
        //{
        //    ClientItem client = new ClientItem(tcp);
        //    lock (сlientList)
        //    {
        //        сlientList.Add(client);

        //        // Подписываемся на события клиента
        //        client.SendTextToAll        += SendTextToAll;
        //        client.GetClientList        += GetClientList;
        //        client.GetFileList          += GetFileList;
        //        client.GetFileServer        += GetFileServer;
        //        client.GetFreeFileServer    += GetFreeFileServer;
        //        client.IsContaind           += IsContained;
        //        client.WriteLog             += WriteLog;

        //        client.StartServe();
        //    }
        //}

        ///// <summary>
        ///// Прослушивает порт на наличие новых подключений
        ///// </summary>
        //private void MainMsgFunc()
        //{
        //    listener.Start();
        //    while (true)
        //    {
        //        TcpClient tcp = listener.AcceptTcpClient();
        //        AddClient(tcp);
        //    }
        //}

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
