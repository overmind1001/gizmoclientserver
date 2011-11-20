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

        /////////////////////////////////////////////////////////////////////////////////////////////////////
        // Взаимодействие с диспетчером

        /// <summary>
        /// Пингует диспетчер. В отдельном потоке.
        /// </summary>
        private void DispatcherPingThreadFunc()
        {
            int tryPing = 0;
            while (!m_IsIsolated)
            {
                try
                {
                    if (SendCommand("!ping", "", m_DispatcherIP, m_DispatcherPort).cmd != "!pong")
                    {
                        UiWriteLog("Диспетчер не отвечает на пинг");
                        tryPing++;

                        if (tryPing == 3)
                        {
                            UiWriteLog("Диспетчер не пинговал уже 3 раза!");
                            m_IsIsolated = DisconnectToDispatcher();
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write(" > Ошибка в DispatcherPingThreadFunc: " + ex.Message);
                }
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Добавляет сервер в список
        /// </summary>
        /// <param name="ip">адрес</param>
        /// <param name="port">порт</param>
        /// <returns>ответ</returns>
        private NetCommand AnsAddServer(string ip, int port)
        {
            NetCommand retn = CreateCommand("!ok", "");

            if (FindServer(ip, port) == null)
            {
                AddServer(ip, port);
            }

            return retn;
        }

        /// <summary>
        /// Удаляет сервер из списка
        /// </summary>
        /// <param name="ip">адрес</param>
        /// <param name="port">порт</param>
        /// <returns>ответ</returns>
        private NetCommand AnsDeleteServer(string ip, int port)
        {
            NetCommand retn = CreateCommand("!ok", "");

            if (FindServer(ip, port) != null)
            {
                DeleteServer(ip, port);
            }

            return retn;
        }

        /// <summary>
        /// Обработчик команды диспетчера
        /// </summary>
        /// <param name="Stream">поток</param>
        /// <param name="Cmd">команда</param>
        private void DispatcherCommandHandler(NetStreamReaderWriter Stream, NetCommand Cmd)
        {
            try
            {
                switch (Cmd.cmd)
                {
                    // Зарегистрировался новый сервер
                    case "!serverregistered":
                        {
                            string[] param = Cmd.parameters.Split(new char[] { ' ' });
                            if (param.Length < 2)
                            {
                                UiWriteLog("Неверный формат команды");
                                break;
                            }

                            string ip = param[0];
                            string port = param[1];
                            Stream.WriteCmd(AnsAddServer(ip, int.Parse(port)));
                        }
                        break;

                    // Сервер разрегистрировался
                    case "!serverunregistered":
                        {
                            string[] param = Cmd.parameters.Split(new char[] { ' ' });
                            if (param.Length < 2)
                            {
                                UiWriteLog("Неверный формат команды");
                                break;
                            }

                            string ip = param[0];
                            string port = param[1];
                            Stream.WriteCmd(AnsDeleteServer(ip, int.Parse(port)));
                        }
                        break;
                    
                    // Регистрация и анрегистрация клиентов
                    case "!clientregistered":
                    case "!clientunregistered":
                        Stream.WriteCmd(CreateCommand("!ok", "Вас понял!"));
                        NetCommand CloneCmd = Cmd.Clone();
                        CloneCmd.Ip = m_ServerIP.ToString();
                        CloneCmd.Port = m_ServerPort;
                        CloneCmd.sender = "msgserver";
                        SendCmdToAllClients(CloneCmd);
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
            catch (Exception ex)
            {
                Debug.Write(" > Ошибка в DispatcherCommandHandler: " + ex.Message);
            }
        }
    }
}
