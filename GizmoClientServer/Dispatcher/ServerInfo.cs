using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Dispatcher
{
    class ServerInfo
    {
        public string Ip {get;set;}
        public int Port { get; set; }
        public TcpClient tcpClient { get; set; }


        //только для чтения
        public string Address
        {
            get
            {
                return Ip + " : " + Port.ToString();
            }
        }

        public override string ToString()
        {
            return Address;
        }
    }
}
