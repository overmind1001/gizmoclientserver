using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgServer
{
    /// <summary>
    /// Класс сервера
    /// </summary>
    class ServerItem
    {
        private string m_IPAddress;
        private int m_Port;

        public ServerItem(string ip, int port)
        {
            m_IPAddress = ip;
            m_Port = port;
        }

        public string GetIP()
        {
            return m_IPAddress;
        }

        public int GetPort()
        {
            return m_Port;
        }

    }
}
