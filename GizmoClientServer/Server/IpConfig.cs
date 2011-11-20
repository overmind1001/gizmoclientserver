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
    public partial class IpConfig : Form
    {
        public IPAddress Ip;

        public IpConfig()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Ip = IPAddress.Parse(cmbIP.Text);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {

        }

        private void IpConfig_Load(object sender, EventArgs e)
        {
            IPAddress[] ipArr = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (IPAddress ip in ipArr)
            {
                cmbIP.Items.Add(ip.ToString());
            }
        }
    }
}
