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
        private string m_IPAddress;
        private int m_Port;
        private DateTime m_LastPingTime;

        public ClientItem(string name, string ip, int port)
        {
            m_Name = name;
            m_IPAddress = ip;
            m_Port = port;
        }

        public string GetName()
        {
            return m_Name;
        }

        public string GetIP()
        {
            return m_IPAddress;
        }

        public int GetPort()
        {
            return m_Port;
        }

        public DateTime GetLastPingTime()
        {
            return m_LastPingTime;
        }

        public void SetLastPingTime(DateTime dt)
        {
            m_LastPingTime = dt;
        }
    }
}
