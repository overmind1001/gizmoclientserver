﻿using System;
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
            RunServer();
        }

        protected void RunServer()
        {

        }

        protected void StopServer()
        {

        }

        //Поднять сервер
        private void m_menuRun_Click(object sender, EventArgs e)
        {
            RunServer();
        }

        //Уронить сервер
        private void m_menuStop_Click(object sender, EventArgs e)
        {
            StopServer();
        }
    }
}
