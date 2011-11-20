using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Dispatcher;
using System.Net.Sockets;
using System.Net;

namespace FileServer
{
    public partial class FileServerForm : Form
    {
#region Делегаты
        public delegate void ClearLogHandler();
        public delegate void WriteLogHandler(string msg);
        public delegate void AddFileHandler(string filename);
        public delegate void RemoveFileHandler(string filename);

        protected ClearLogHandler clearLogD;
        protected WriteLogHandler writeLogD;
        protected AddFileHandler addFileD;
        protected RemoveFileHandler removeFileD;
#endregion

#region Поля
        TcpListener m_tcpListener;
        IPAddress m_ip;
        uint m_port;            
#endregion

#region Методы

        public FileServerForm()
        {
            InitializeComponent();
            InitDelegates();
            m_menuStop.Enabled = false;
            m_menuRun_Click(null, null);
        }

        private void InitDelegates()
        {
            clearLogD += ClearLog;
            writeLogD += WriteLog;
            addFileD += AddFile;
            removeFileD += RemoveFile;
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

#endregion

#region Интерфейсные штуки

        private void m_menuRun_Click(object sender, EventArgs e)
        {
            if (RunServer()) {
                ClearLog();
                m_statusLabel.Text = "Состояние: запущен";
                m_menuRun.Enabled = false;
                m_menuStop.Enabled = true;
            }
        }

        private void m_menuStop_Click(object sender, EventArgs e)
        {
            if (StopServer()) {
                m_statusLabel.Text = "Состояние: остановлен";
                m_menuRun.Enabled = true;
                m_menuStop.Enabled = false;
            }
        }

        private void ClearLog()
        {
            lock (m_tbLog) {
                m_tbLog.Clear();
            }
        }

        private void uiClearLog()
        {
            m_tbLog.Invoke(clearLogD);
        }

        private void WriteLog(string msg)
        {
            lock (m_tbLog) {
                m_tbLog.Text += "\n" + msg;
            }
        }

        private void uiWriteLog(string msg)
        {
            m_tbLog.Invoke(writeLogD, new object[] { msg });
        }

        private void AddFile(string filename)
        {
            lock (m_lbFiles) {
                m_lbFiles.Items.Add(filename);
            }
        }

        private void uiAddFile(string filename)
        {
            m_lbFiles.Invoke(addFileD, new object[] { filename });
        }

        private void RemoveFile(string filename)
        {
            lock (m_lbFiles) {
                m_lbFiles.Items.Remove(filename);
            }
        }

        private void uiRemoveFile(string filename)
        {
            m_lbFiles.Invoke(removeFileD, new object[] { filename });
        }

#endregion
    }
}
