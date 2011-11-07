using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.IO.Pipes;

namespace MsgServer
{
    /// <summary>
    /// Класс клиента
    /// </summary>
    class ClientItem
    {
        private string m_Name;
        private TcpClient m_Tcp;
        private Thread m_ServeThread;
        private ClientState m_State;
        private enum ClientState { Connected, Disconnected };

        public ClientItem(TcpClient tcp, string name)
        {
            m_Tcp = tcp;
            m_Name = name;
            m_State = ClientState.Connected;
        }

        ~ClientItem()
        {
            m_ServeThread.Abort();
        }

        /// <summary>
        /// Посылаем другим клиентам текстовое сообщение
        /// </summary>
        /// <param name="text"></param>
        private void SendTextToAll(string text)
        {
        }

        /// <summary>
        /// Начинаем обслуживание клиента
        /// </summary>
        public void StartServe()
        {
            m_ServeThread = new Thread(ServeThreadFunc);
            m_ServeThread.Start();
        }

        /// <summary>
        /// Поток обслуживания
        /// </summary>
        public void ServeThreadFunc()
        {
            StreamReader reader = new StreamReader(m_Tcp.GetStream());
            string line;
            string cmd;
            string txt;

            while (true)
            {
                if (!m_Tcp.Connected)
                {
                    m_State = ClientState.Disconnected;
                    break;
                }

                line = reader.ReadLine();
                cmd = line.Split(new char[] { ' ' })[0];
                txt = line.Substring(cmd.Length);

                switch (cmd)
                {
                    case "!message":
                        if (txt.Length > 0) 
                            SendTextToAll(txt);
                        break;
                    case "!getclientlist":
                        break;
                    case "!getfilelist":
                        break;
                    case "!getfreefileserver":
                        break;
                    case "!getfileserver":
                        break;
                }
            }
        }
    }
}
