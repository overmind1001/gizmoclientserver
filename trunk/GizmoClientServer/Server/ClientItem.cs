using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.IO.Pipes;
using System.Windows.Forms;

using Dispatcher;

namespace MsgServer
{
    class ClientItem
    {
        private string m_Name;                                      // Имя клиента
        private TcpClient m_Tcp;                                    // tcp - соединение клиента

        private Thread m_ServeThread;                               // Поток обработки сообщений

        private ClientState m_State;                                // Состояние подключения клиента
        private enum ClientState { Connected, Disconnected };

        private RegistrationState m_Reg;                            // Состояние регистрации клиента
        private enum RegistrationState { Register, Unregister };

        private NetStreamReaderWriter m_StreamRW;                   // Читатель-писатель потока

        ////////////////////////////////////////////////////////////////////////////////////////////////////////
        // События и делегаты

        // Для отсылки текстового сообщения другим клиентам
        public delegate bool SendTextToAllHandler(string name, string text);
        public event SendTextToAllHandler SendTextToAll; 

        // Для запроса списка клиентов
        public delegate string GetClientListHandler();
        public event GetClientListHandler GetClientList;

        // Для запроса списка файлов
        public delegate string GetFileListHandler();
        public event GetFileListHandler GetFileList;

        // Для запроса файлового сервера, на котором хранится файл
        public delegate string GetFileServerHandler(string name);
        public event GetFileServerHandler GetFileServer;

        // Для запроса свободного файлового сервера
        public delegate string GetFreeFileServerHandler();
        public event GetFreeFileServerHandler GetFreeFileServer;

        ////////////////////////////////////////////////////////////////////////////////////////////////////////



        /// <summary>
        /// Констурктор класса клиента
        /// </summary>
        /// <param name="tcp">tcp - соединение</param>
        public ClientItem(TcpClient tcp)
        {
            m_Tcp = tcp;
            if (tcp.Connected)
                m_State = ClientState.Connected;

            m_Reg = RegistrationState.Unregister; // Вначале клиент незарегистрирован

            m_StreamRW = new NetStreamReaderWriter(m_Tcp.GetStream()); // Создаем читателя-писателя
        }
        ~ClientItem()
        {
            m_ServeThread.Abort();
        }

        /// <summary>
        /// Возвращает имя клиента
        /// </summary>
        /// <returns>строка имени</returns>
        public string GetName()
        {
            return m_Name;
        }

        /// <summary>
        /// Зарегистрирован ли клиент
        /// </summary>
        /// <returns>true - да</returns>
        public bool IsRegister()
        {
            if (m_Reg == RegistrationState.Register)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Регистрация клиента на сервере
        /// </summary>
        /// <param name="name">имя клиента</param>
        private bool Registry(string name)
        {
            m_Name = name;
            m_Reg = RegistrationState.Register;

            // Тут нужно наверное сделать какую то проверку на совпадение имен (или идентификаторов) 

            return true;
        }


        // Сообщения

        /// <summary>
        /// Послать сообщение клиенту от...
        /// </summary>
        /// <param name="name">имя адресата</param>
        /// <param name="text">текст сообщения</param>
        public void SendText(string name, string text)
        {
            m_StreamRW.WriteLine("!message " + name + ":" + text);
        }


        /// <summary>
        /// Послать сообщение этому клиенту
        /// </summary>
        /// <param name="text">текст сообщения</param>
        public void SendText(string text)
        {
            m_StreamRW.WriteLine(text);
        }


        // Цикл обработки сообщений

        /// <summary>
        /// Запуск потока обработки сообщений клиента
        /// </summary>
        public void StartServe()
        {
            m_ServeThread = new Thread(ServeThreadFunc);
            m_ServeThread.Start();
        }

        /// <summary>
        /// Обрабатывает поступающие сообщения от клиента
        /// </summary>
        private void ServeThreadFunc()
        {
            string line;
            string cmd;
            string param;

            while (true)
            {
                // Если нет соединения, завершаем поток
                if (!m_Tcp.Connected)
                {
                    m_State = ClientState.Disconnected;
                    break;
                }

                line = m_StreamRW.ReadLine();
                cmd = line.Split(new char[]{' '})[0];
                param = line.Substring(cmd.Length);
         
                // Обработка команд
                switch (cmd)
                {
                    // кто
                    case "!who":
                        {
                            SendText("messageserver");
                        }
                        break;

                    // регистрация этого клиента
                    case "!register":
                        {
                            if (Registry(param))
                                SendText("!registred");
                            else
                                SendText("!unregistred");
                        }
                        break;

                    // сообщение всем от этого клиента
                    case "!message":
                        {
                            bool ret = SendTextToAll(m_Name, param);
                        }
                        break;
                    
                    // запрос списка контактов
                    case "!getclientlist":
                        {
                            string ClientList = GetClientList();
                            SendText(ClientList);
                        }
                        break;

                    // запрос списка файлов
                    case "!getfilelist":
                        {
                            string FileList = GetFileList();
                            SendText(FileList);
                        }
                        break;

                    // запрос свободного файлового сервера для закачки
                    case "!getfreefileserver":
                        {
                            string FreeFileServer = GetFreeFileServer();
                            SendText(FreeFileServer);
                        }
                        break;

                    // запрос файлового сервера для скачки
                    case "!getfileserver":
                        {
                            string FileServer = GetFileServer(param);
                            SendText(FileServer);
                        }
                        break;

                    default:
                        {
                            SendText("!unknowncmd");
                            MessageBox.Show("Неизвестная команда от пользователя " + m_Name
                                             + "\nТекст команды: " + line, "Внимание");
                        }
                        break;
                }
            }
            // завершение потока
        }
    }
}
