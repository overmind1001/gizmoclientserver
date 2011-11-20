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
        // Взаимодействие с клиентом

        /// <summary>
        /// Генирирует команду ответа на запрос "!who"
        /// </summary>
        /// <returns>команда</returns>
        private NetCommand AnsWho()
        {
            return CreateCommand("!messageserver", "Сервер сообщений");
        }

        /// <summary>
        /// Регистрирует клиента и генерирует команду ответа
        /// </summary>
        /// <param name="RegComand">команда запроса регистрации</param>
        /// <returns>команда</returns>
        private NetCommand AnsRegister(NetCommand RegCmd)
        {
            NetCommand RetCmd;

            // Если сервер переполнен
            if (GetClientCount() >= m_MaxClientCount)
                RetCmd = CreateCommand("!unregistred", "Сервер переполнен");

            // Если клиент с таким именем уже имеется
            else if (FindClient(RegCmd.sender) != null)
            {
                RetCmd = CreateCommand("!unregistred", "Клиент с таким именем уже зарегистрирован");
            }

            // Если все норм, регистрируем
            else if (AddClient(RegCmd.sender, RegCmd.Ip, RegCmd.Port))
                RetCmd = CreateCommand("!registred", "Регистрация успешно завершена");

            // Если по какой то причине регистрация не удалась
            else
                RetCmd = CreateCommand("!unregistred", "Регистрация не удалась");

            return RetCmd;
        }

        /// <summary>
        /// Генерирует команду в ответ на запрос списка клиентов
        /// </summary>
        /// <returns>команда</returns>
        private NetCommand AnsClientList()
        {
            return CreateCommand("!clientlist", GetClientList());
        }

        /// <summary>
        /// Генерирует команду в ответ на запрос списка файлов
        /// </summary>
        /// <returns>команда</returns>
        private NetCommand AnsFileList()
        {
            return CreateCommand("!filelist", "");
        }

        /// <summary>
        /// Генерирует команду в ответ на запрос свободного файлового сервера
        /// </summary>
        /// <returns>команда ответа</returns>
        private NetCommand AnsFreeFileServer()
        {
            return CreateCommand("!null", "");
        }

        /// <summary>
        /// Генерирует команду в ответ на запрос файлового сервера на котором расположен файл
        /// </summary>
        /// <param name="filename">имя файла</param>
        /// <returns>команда ответа</returns>
        private NetCommand AnsFileServer(string filename)
        {
            return CreateCommand("!null", "");
        }

        /// <summary>
        /// Обработчик клиентской команды
        /// </summary>
        /// <param name="Stream">читатель-писатель</param>
        /// <param name="Cmd">команда</param>
        private void ClientCommandHandler(NetStreamReaderWriter Stream, NetCommand Cmd)
        {
            switch (Cmd.cmd)
            {
                // Инициализация, или "кому я пишу?"
                case "!who":
                    {
                        Stream.WriteCmd(AnsWho());
                    }
                    break;

                // Регистрация клиента
                case "!register":
                    {
                        Stream.WriteCmd(AnsRegister(Cmd));
                    }
                    break;

                // Сообщение всем клиентам
                case "!message":
                    {
                        SendMsgToAllClients(Cmd.sender, Cmd.parameters);
                        Stream.WriteCmd(CreateCommand("!ok", "Вас понял"));
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

                // Пинг от клиента
                case "!ping":
                    {
                        Stream.WriteCmd(CreateCommand("!pong", "Я тут"));
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
    }
}
