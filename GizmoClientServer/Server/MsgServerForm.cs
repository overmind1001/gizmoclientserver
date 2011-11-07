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
        /// Остановлен ли сервер
        /// </summary>
        private bool isServerStopped;


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
            mainMsgThread   = new Thread(MainMsgFunc);
        }

        /// <summary>
        /// Коннектимся к диспетчеру
        /// </summary>
        /// <returns>удачно ли</returns>
        private bool ConnectToDispatcher()
        {
            MessageBox.Show("Не удалось подключиться к диспетчеру. Сервер работает в автономном режиме\n" +
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
            isIsolated = !ConnectToDispatcher();

            listener.Start();
            mainMsgThread.Start();
        }

        /// <summary>
        /// Сушим весла
        /// </summary>
        private void StopServer()
        {
            isIsolated = true;
            DisconnectToDispatcher();

            listener.Stop();
            mainMsgThread.Abort();
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

        public void Register(TcpClient tcp, string name)
        {
            if (!tcp.Connected)
                return;

            ClientItem client = new ClientItem(tcp, name);
            clientsList.Add(client);
        }

        /// <summary>
        /// В отдельном потоке. Обрабатывает общие и служебные запросы к серверу
        /// </summary>
        public void MainMsgFunc()
        {
            while (!isServerStopped)
            {
                TcpClient tcp = listener.AcceptTcpClient();
                NetworkStream stream = tcp.GetStream();
                StreamReader reader = new StreamReader(stream);

                string cmd = reader.ReadLine();
                string[] cmdArr = cmd.Split(new char[]{' '});

                switch(cmdArr[0])
                {
                    // Клиент хочет зарегистрироваться
                    case "!register":
                        if (cmdArr.Length == 2)
                            Register(tcp, cmd.Split(new char[] { ' ' })[1]);
                        break;
                }
            }
        }


    }
}
