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

using Dispatcher;

namespace MsgServer
{
    class ClientItem
    {

        private string m_Name;
        private IPAddress m_IPAddress;
        private int m_Port;

        public ClientItem(string name, string ip, int port)
        {
            m_Name = name;
            m_IPAddress = Dns.GetHostEntry(ip).AddressList[0];
            m_Port = port;
        }


        public string GetName()
        {
            return m_Name;
        }
















        //private string m_Name;                                      // Имя клиента
        //private TcpClient m_Tcp;                                    // tcp - соединение клиента

        //private Thread m_ServeThread;                               // Поток обработки сообщений

        //private ClientState m_State;                                // Состояние подключения клиента
        //private enum ClientState { Connected, Disconnected };

        //private RegistrationState m_Reg;                            // Состояние регистрации клиента
        //private enum RegistrationState { Register, Unregister };

        //private NetStreamReaderWriter m_StreamRW;                   // Читатель-писатель потока

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //// События и делегаты

        //// Для отсылки текстового сообщения другим клиентам
        //public delegate bool SendTextToAllHandler(string name, string text);
        //public event SendTextToAllHandler SendTextToAll; 

        //// Для запроса списка клиентов
        //public delegate string GetClientListHandler();
        //public event GetClientListHandler GetClientList;

        //// Для запроса списка файлов
        //public delegate string GetFileListHandler();
        //public event GetFileListHandler GetFileList;

        //// Для запроса файлового сервера, на котором хранится файл
        //public delegate string GetFileServerHandler(string name);
        //public event GetFileServerHandler GetFileServer;

        //// Для запроса свободного файлового сервера
        //public delegate string GetFreeFileServerHandler();
        //public event GetFreeFileServerHandler GetFreeFileServer;

        //// Для запроса наличия клиента с заданным именем в списке клиентов
        //public delegate bool IsContainedHandler(string name);
        //public event IsContainedHandler IsContaind;

        //// Для записи в лог
        //public delegate void WriteLogHandler(string text);
        //public event WriteLogHandler WriteLog;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////



        ///// <summary>
        ///// Констурктор класса клиента
        ///// </summary>
        ///// <param name="tcp">tcp - соединение</param>
        //public ClientItem(TcpClient tcp)
        //{
        //    m_Tcp = tcp;
        //    if (tcp.Connected)
        //        m_State = ClientState.Connected;

        //    m_Reg = RegistrationState.Unregister; // Вначале клиент незарегистрирован
        //    m_Name = "unknown";

        //    m_StreamRW = new NetStreamReaderWriter(m_Tcp.GetStream()); // Создаем читателя-писателя
        //}
        //~ClientItem()
        //{
        //    m_ServeThread.Abort();
        //}

        ///// <summary>
        ///// Возвращает имя клиента
        ///// </summary>
        ///// <returns>строка имени</returns>
        //public string GetName()
        //{
        //    return m_Name;
        //}

        ///// <summary>
        ///// Зарегистрирован ли клиент
        ///// </summary>
        ///// <returns>true - да</returns>
        //public bool IsRegister()
        //{
        //    if (m_Reg == RegistrationState.Register)
        //        return true;
        //    else
        //        return false;
        //}

        ///// <summary>
        ///// Регистрация клиента на сервере
        ///// </summary>
        ///// <param name="name">имя клиента</param>
        //private bool Registry(string name)
        //{
        //    if (!IsContaind(name))
        //    {
        //        m_Name = name;
        //        m_Reg = RegistrationState.Register;
        //        return true;
        //    }
        //    else
        //        return false;
        //}


        //// Сообщения

        ///// <summary>
        ///// Послать сообщение клиенту от...
        ///// </summary>
        ///// <param name="name">имя адресата</param>
        ///// <param name="text">текст сообщения</param>
        //public void SendText(string name, string text)
        //{
        //    m_StreamRW.WriteLine("!message " + name + ":" + text);
        //}


        ///// <summary>
        ///// Послать сообщение этому клиенту
        ///// </summary>
        ///// <param name="text">текст сообщения</param>
        //public void SendText(string text)
        //{
        //    m_StreamRW.WriteLine(text);
        //}


        //// Цикл обработки сообщений

        ///// <summary>
        ///// Запуск потока обработки сообщений клиента
        ///// </summary>
        //public void StartServe()
        //{
        //    m_ServeThread = new Thread(ServeThreadFunc);
        //    m_ServeThread.Start();
        //}

        ///// <summary>
        ///// Обрабатывает поступающие сообщения от клиента
        ///// </summary>
        //private void ServeThreadFunc()
        //{
        //    string line;
        //    string cmd;
        //    string param;

        //    while (true)
        //    {
        //        // Если нет соединения, завершаем поток
        //        if (!m_Tcp.Connected)
        //        {
        //            m_State = ClientState.Disconnected;
        //            break;
        //        }

        //        line = m_StreamRW.ReadLine();
        //        cmd = line.Split(new char[]{' '})[0];
        //        param = line.Substring(cmd.Length);
         
        //        // Обработка команд
        //        switch (cmd)
        //        {
        //            // Кто
        //            case "!who":
        //                {
        //                    SendText("messageserver");
        //                }
        //                break;

        //            // Регистрация этого клиента
        //            case "!register":
        //                {
        //                    if (Registry(param))
        //                        SendText("!registred");
        //                    else
        //                        SendText("!unregistred");
        //                }
        //                break;

        //            // Сообщение всем от этого клиента
        //            case "!message":
        //                {
        //                    bool ret = SendTextToAll(m_Name, param);
        //                }
        //                break;
                    
        //            // Запрос списка контактов
        //            case "!getclientlist":
        //                {
        //                    string ClientList = GetClientList();
        //                    SendText(ClientList);
        //                }
        //                break;

        //            // Запрос списка файлов
        //            case "!getfilelist":
        //                {
        //                    string FileList = GetFileList();
        //                    SendText(FileList);
        //                }
        //                break;

        //            // Запрос свободного файлового сервера для закачки
        //            case "!getfreefileserver":
        //                {
        //                    string FreeFileServer = GetFreeFileServer();
        //                    SendText(FreeFileServer);
        //                }
        //                break;

        //            // Запрос файлового сервера для скачки
        //            case "!getfileserver":
        //                {
        //                    string FileServer = GetFileServer(param);
        //                    SendText(FileServer);
        //                }
        //                break;

        //            default:
        //                {
        //                    SendText("!unknowncmd");
        //                    WriteLog("от клиента " + m_Name + " поступила команда '" + line +
        //                        "'. Такая команда не известна серверу");
        //                }
        //                break;
        //        }
        //    }
        //    // завершение потока
        //}
    }
}
