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
        // Методы отвечающие за прослушку TCP

        /// <summary>
        /// Цикл прослушки новых tcp-соединений от клиентов. Работает в отдельном потоке
        /// </summary>
        private void TcpListenThreadFunc()
        {
            while (true)
            {
                try
                {
                    TcpClient Tcp = m_Listener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(TcpThreadFunc, Tcp);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(" > Server. Ошибка в TcpListenThreadFunc: " + ex.Message);
                }
            }
        }



        /// <summary>
        /// Реализует реакцию на команду из tcp-соединения
        /// </summary>
        /// <param name="Tcp">tcp-отправителя</param>
        private void TcpThreadFunc(object tcp)
        {
            TcpClient Tcp = (TcpClient)tcp;

            try
            {
                NetStreamReaderWriter Stream = new NetStreamReaderWriter(Tcp.GetStream());
                NetCommand Cmd = Stream.ReadCmd();

                UiWriteLog("От " + Cmd.sender + " поступила команда '" + Cmd.cmd + "'");

                // Если отправитель - другой сервер сообщений
                if (Cmd.sender == "msgserver")
                {
                }

                // Если отправитель - диспетчер
                else if (Cmd.sender == "dispatcher")
                {
                }

                // Если отправитель - клиент
                else
                {
                    ClientCommandHandler(Stream, Cmd);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(" > Server. Ошибка в TcpThreadFunc: " + ex.Message);
            }
        }




    }
}

