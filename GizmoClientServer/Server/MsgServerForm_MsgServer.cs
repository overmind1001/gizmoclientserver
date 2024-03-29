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
        /// <summary>
        /// Обработчик команды сервера
        /// </summary>
        /// <param name="Stream">поток</param>
        /// <param name="Cmd">команда</param>
        private void ServerCommandHandler(NetStreamReaderWriter Stream, NetCommand Cmd)
        {
            try
            {
                switch (Cmd.cmd)
                {
                    // Принимает сообщение
                    case "!message":
                        {
                            Stream.WriteCmd(CreateCommand("!ok", "Вас понял!"));
                            UiWriteLog(Cmd.parameters);
                            SendMsgToAllClients("!msgserver", Cmd.parameters);
                        }
                        break;

                    case "!clientregistred":
                    case "!clientunregistred":
                        {
                            Stream.WriteCmd(CreateCommand("!ok", "Вас понял!"));
                            Cmd.sender = "!msgserver";
                            Cmd.Ip = m_ServerIP.ToString();
                            Cmd.Port = m_ServerPort;
                            SendCmdToAllClients(Cmd);
                        }
                        break;
                    case "!serverregistred":
                    case "!serverunregistred":
                        {
                            Stream.WriteCmd(CreateCommand("!ok", "Вас понял!"));
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
            catch (Exception ex)
            {
                Debug.Write(" > Ошибка в ServerCommandHandler: " + ex.Message);
            }
        }
    }
}
