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

namespace MsgServer
{
    public partial class MsgServerForm : Form
    {
        // Поля

        private TcpListener listener;                               // Прослушивает порт сервера для обнаружения новых tcp подключений

        private int dispatcerPort;                                  // Номер порта диспетчера
        private int serverPort;                                     // Номер порта сервера
        private IPAddress serverIp;                                 // ip сервера

        private List<ServerItem> serversList;                       // Список известных серверов
        private List<ClientItem> сlientList;                        // Список клиентов

        private ServerState state;                                  // Состояние сервера
        private enum ServerState { Stopped, Running, Started };

        private Thread mainMsgThread;                               // Поток, который прослушивает порт сервера и фиксирует новые подключения
        
        private bool isIsolated;                                    // Показывает, запущен ли сервер под управлением диспетчера или автономно
        


        // Методы

        /// <summary>
        /// Начальная инициализация 
        /// </summary>
        private void Init()
        {
            dispatcerPort   = 501;
            serverPort      = 505;
            serverIp        = Dns.Resolve("localhost").AddressList[0];

            isIsolated      = false;

            serversList     = new List<ServerItem>();
            сlientList = new List<ClientItem>();


            listener        = new TcpListener(serverIp, serverPort);

            state           = ServerState.Started;
        }



        /// <summary>
        /// Подключиться к диспетчеру
        /// </summary>
        /// <returns>удачно ли</returns>
        private bool ConnectToDispatcher()
        {
            MessageBox.Show("Не удалось подключиться к диспетчеру.\n" +
                            "Сервер работает в автономном режиме.\n" +
                            "Порт сервера: " + serverPort.ToString());
            return false;
        }



        /// <summary>
        /// Отключиться от диспетчера
        /// </summary>
        private void DisconnectToDispatcher()
        {
        }



        /// <summary>
        /// Запустить сервер
        /// </summary>
        private void StartServer()
        {
            if (state == ServerState.Running)
                return;

            state = ServerState.Running;

            isIsolated = !ConnectToDispatcher();

            mainMsgThread = new Thread(MainMsgFunc);
            mainMsgThread.Start();

            lblStatus.Text = isIsolated ? "автономно" : "подключен";
        }

        /// <summary>
        /// Отключить сервер
        /// </summary>
        private void StopServer()
        {
            if (state == ServerState.Stopped)
                return;

            state = ServerState.Stopped;

            isIsolated = true;
            DisconnectToDispatcher();

            if (mainMsgThread != null)
                mainMsgThread.Abort();

            listener.Stop();

            lstClients.Items.Clear();
            lstServers.Items.Clear();

            lblStatus.Text = "отключен";
        }


        public MsgServerForm()
        {
            InitializeComponent();

            Init();
        }



        /// <summary>
        /// Добавить нового клиента
        /// </summary>
        /// <param name="tcp">tcp клиента</param>
        private void AddClient(TcpClient tcp)
        {
            ClientItem client = new ClientItem(tcp);
            сlientList.Add(client);
            client.StartServe();
        }

        /// <summary>
        /// Прослушивает порт на наличие новых подключений
        /// </summary>
        private void MainMsgFunc()
        {
            listener.Start();
            while (true)
            {
                TcpClient tcp = listener.AcceptTcpClient();
                AddClient(tcp);
            }
        }



        //////////////////////////////////////////////////////////////////////
        // Обработчики событий формы

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
