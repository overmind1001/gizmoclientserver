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
            //  Тут необходимо определиться как идентифицировать клиентов. 
            //    По именам или идентификаторам. И есть ли возможность повторения имен.

            if (m_Name == name)
                return;

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


        /// <summary>
        /// Послать текстовое сообщение другим клиентам
        /// </summary>
        /// <param name="text">текст сообщения</param>
        private void SendTextToAll(string text)
        {
        }



        // Запросы

        /// <summary>
        /// Дай мне список зарегистрированных клиентов
        /// </summary>
        private void GetClientList()
        {
        }

        /// <summary>
        /// Дай мне список файлов
        /// </summary>
        private void GetFileList()
        {
        }

        /// <summary>
        /// Дай свободный файловый сервер, хочу залить
        /// </summary>
        private void GetFreeFileServer()
        {
        }

        /// <summary>
        /// Дай файловый сервер с нужным файлом, хочу скачать
        /// </summary>
        /// <param name="filename"></param>
        private void GetFileServer(string filename)
        {
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
                            SendTextToAll(param);
                        }
                        break;
                    
                    // запрос списка контактов
                    case "!getclientlist":
                        {
                            GetClientList();
                        }
                        break;

                    // запрос списка файлов
                    case "!getfilelist":
                        {
                            GetFileList();
                        }
                        break;

                    // запрос свободного файлового сервера для закачки
                    case "!getfreefileserver":
                        {
                            GetFreeFileServer();
                        }
                        break;

                    // запрос файлового сервера для скачки
                    case "!getfileserver":
                        {
                            GetFileServer(param);
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
