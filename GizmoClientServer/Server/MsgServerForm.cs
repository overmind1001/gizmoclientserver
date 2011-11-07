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

namespace MsgServer
{
    enum ServerState
    {
        Stopped,
        Running,
        Started
    }

    public partial class MsgServerForm : Form
    {
        // Поля

        /// <summary>
        /// Слухач
        /// </summary>
        private TcpListener listener;
        /// <summary>
        /// Номер порта диспетчера
        /// </summary>
        private int dispatcerPort;
        /// <summary>
        /// Номер порта этого сервера
        /// </summary>
        private int serverPort;
        /// <summary>
        /// Список всех серверов
        /// </summary>
        private List<ServerItem> serversList;
        /// <summary>
        /// Список всех клиентов
        /// </summary>
        private List<ClientItem> clientsList;
        /// <summary>
        /// Если сервер обособлен
        /// </summary>
        private bool isIsolated;
        /// <summary>
        /// Поток реализует обработку запросов к серверу
        /// </summary>
        private Thread mainMsgThread;
        /// <summary>
        /// Состояние сервера
        /// </summary>
        private ServerState state;


        // Методы

        /// <summary>
        /// Инициализируем поля
        /// </summary>
        private void Init()
        {
            serverPort      = 505;
            dispatcerPort   = 501;
            isIsolated      = false;
            serversList     = new List<ServerItem>();
            clientsList     = new List<ClientItem>();
            listener        = new TcpListener(serverPort);
            state           = ServerState.Started;
        }

        /// <summary>
        /// Коннектимся к диспетчеру
        /// </summary>
        /// <returns>удачно ли</returns>
        private bool ConnectToDispatcher()
        {
            MessageBox.Show("Не удалось подключиться к диспетчеру.\nСервер работает в автономном режиме\n" +
                            "Порт сервера: " + serverPort.ToString());
            return false;
        }

        /// <summary>
        /// Отключаемся от диспетчера
        /// </summary>
        private void DisconnectToDispatcher()
        {
        }

        /// <summary>
        /// Запуск сервера
        /// </summary>
        /// <returns></returns>
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
        /// Сушим весла
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

        /// <summary>
        /// Запуск сервера вручную
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void поднятьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartServer();
        }

        /// <summary>
        /// Роняем сервер вручную
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void уронитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StopServer();
        }

        /// <summary>
        /// Типа запуск формы
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MsgServerForm_Load(object sender, EventArgs e)
        {
            StartServer();
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        public MsgServerForm()
        {
            InitializeComponent();

            Init();
        }

        /// <summary>
        /// Регистрирует нового клиента
        /// </summary>
        /// <param name="tcp">TCP</param>
        /// <param name="name">имя</param>
        public void Register(TcpClient tcp, string name)
        {
            if (!tcp.Connected)
                return;

            ClientItem client = new ClientItem(tcp, name);
            clientsList.Add(client);
            client.StartServe(); // запускаем поток обслуживания

            lstClients.Items.Add(name);
            lstClients.Top = lstClients.Items.Count;
        }

        /// <summary>
        /// В отдельном потоке. Обрабатывает общие и служебные запросы к серверу
        /// </summary>
        public void MainMsgFunc()
        {
            listener.Start();
            while (true)
            {
                TcpClient tcp = listener.AcceptTcpClient();

                if (tcp == null)
                    continue;

                NetworkStream stream = tcp.GetStream();
                StreamReader reader = new StreamReader(stream);

                string line = reader.ReadLine();
                string[] cmdArr = line.Split(new char[] { ' ' });

                switch(cmdArr[0])
                {
                    // Клиент хочет зарегистрироваться
                    case "!register":
                        if (cmdArr.Length >= 2)
                            Register(tcp, line.Substring(cmdArr[0].Length));
                        break;
                }
            }
        }

        private void MsgServerForm_Leave(object sender, EventArgs e)
        {
            
        }

        private void MsgServerForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            StopServer();
        }
    }
}
