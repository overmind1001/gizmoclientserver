using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Dispatcher
{
    public partial class DispatcherForm : Form
    {
        List<ServerInfo> MsgServers;
        List<ServerInfo> FileServers;
        List<ClientInfo> Clients;
        List<FileInfo> Files;

        public DispatcherForm()
        {
            InitializeComponent();

            MsgServers = new List<ServerInfo>();
            FileServers = new List<ServerInfo>();
            Clients = new List<ClientInfo>();
            Files = new List<FileInfo>();
            
            //запустить поток слушающий порт для клиентов
            //запустить поток, слушающий порт для серверов
            //запустить поток для посылки широковещательных сообщений
        }
    }
}
