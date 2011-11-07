using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace MsgServer
{
    /// <summary>
    /// Класс клиента
    /// </summary>
    class ClientItem
    {
        private string m_Name;
        private TcpClient m_Tcp;

        // Паблики

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        // В потоке
        public void Serve()
        {
        }
    }
}
