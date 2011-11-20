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
        public string MailSlot { get; set; }
        //public TcpClient tcpClient { get; set; }
        //public ChannelLock channelLock { get; set; }

        public DateTime lastPingTime { get; set; }
        public List<ClientInfo> clientInfos { get; set; }   //ссылки на клиентов сервера
        public List<FileInfo> fileInfos { get; set; }       //ссылки на файлы сервера
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

        public ServerInfo()
        {
            clientInfos = new List<ClientInfo>();
        }
    }
}
