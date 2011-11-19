using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FileServer
{
    public partial class FileServerForm : Form
    {
        public FileServerForm()
        {
            InitializeComponent();
            m_menuStop.Enabled = false;
            m_menuRun_Click(null, null);
        }

        //Поднять сервер
        protected bool RunServer()
        {
            return false;
        }

        //Уронить сервер
        protected bool StopServer()
        {
            return false;
        }

        private void m_menuRun_Click(object sender, EventArgs e)
        {
            if (RunServer())
            {
                m_menuRun.Enabled = false;
                m_menuStop.Enabled = true;
            }
        }

        private void m_menuStop_Click(object sender, EventArgs e)
        {
            StopServer();
        }
    }
}
