using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.IO.Pipes;

namespace Client
{
    public partial class ClientForm : Form
    {
        TcpClient tcpClient;

        public ClientForm()
        {
            InitializeComponent();
        }


        private void ПодключитьсяtoolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConnectForm cf = new ConnectForm();
            DialogResult dr = cf.ShowDialog();
            if (dr != DialogResult.OK)
            {//не подключились
                return;
            }
            ////////////тут загружаем список контактов и т.д.
            tcpClient = cf.tcpClient;
            NetworkStream ns = tcpClient.GetStream();
            //регистрируемся на сервере
            if (!registerMe(ns, cf.tbName.Text))
                return;
            //загружаем список контактов
            getContactListFromServer(ns);
            //загружаем список файлов
            getFileListFromServer(ns);


            //запускаем поток прослушки
            Thread t = new Thread(Listen);
            t.Start(tcpClient);
        }

        //поток слухача
        private void Listen(Object tcpCliento)
        {
            TcpClient tcpClient =(TcpClient) tcpCliento;
            NetworkStream ns = tcpClient.GetStream();
            StreamReader sr = new StreamReader(ns);
            char[] sep = { ' ' };
            while (tcpClient.Connected)
            {
                string cmd = sr.ReadLine();
                string[] splited = cmd.Split(sep);

                switch (splited[0])
                {
                    case "!message"://прием сообщения
                        string message = cmd.Substring(splited[0].Length);
                        this.tbChat.Text += message + Environment.NewLine;
                        break;
                    case "!clientregistered":
                        string cl = splited[1];
                        this.lbPeople.Items.Add(cl);
                        break;
                    case "!clientunregistered":
                        cl = splited[1];
                        this.lbPeople.Items.Remove(cl);
                        break;
                    case "!addfile":
                        string file = splited[1];
                        lbFilesList.Items.Add(file);
                        break;
                    case "!deletefile":
                        file = splited[1];
                        lbFilesList.Items.Remove(file);
                        break;
                    default:
                        MessageBox.Show("неизвестная команда!");
                        break;
                }
                
            }
        }

        private bool registerMe(NetworkStream ns,string name)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.WriteLine("!register "+name);

            StreamReader sr = new StreamReader(ns);
            string answer = sr.ReadLine();

            if (answer == "!registred")
            {
                return true;
            }
            else
                return false;
        }
        private void getContactListFromServer(NetworkStream ns)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.WriteLine("!getclientlist");         //посылка команды

            StreamReader sr = new StreamReader(ns);
            string answer = sr.ReadLine();          //получение ответа

            char[] sep = { '|' };
            string[] names = answer.Split(sep);//получили массив имен

            lbPeople.Items.Clear();
            foreach (String s in names)
            {
                lbPeople.Items.Add(s);
            }
        }
        private void getFileListFromServer(NetworkStream ns)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.WriteLine("!getfilelist");         //посылка команды

            StreamReader sr = new StreamReader(ns);
            string answer = sr.ReadLine();          //получение ответа

            char[] sep = { '|' };
            string[] files = answer.Split(sep);//получили массив имен

            lbFilesList.Items.Clear();
            foreach (String s in files)
            {
                lbFilesList.Items.Add(s);
            }
        }
        private string getFreeFileServer(NetworkStream ns)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.WriteLine("!getfreefileserver");         //посылка команды

            StreamReader sr = new StreamReader(ns);
            string answer = sr.ReadLine();          //получение ответа

            //char[] sep = { ' ' };
            //string[] addrOfFS = answer.Split(sep);//получили адрес свободного файл-сервера

            //return addrOfFS[1] + " " + addrOfFS[2];//ip port
            return answer;
        }
        private string getFileServer(NetworkStream ns,string file)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.WriteLine("!getfileserver "+file);         //посылка команды

            StreamReader sr = new StreamReader(ns);
            string answer = sr.ReadLine();          //получение ответа

            //char[] sep = { ' ' };
            //string[] addrOfFS = answer.Split(sep);//получили адрес свободного файл-сервера

            //return addrOfFS[1] + " " + addrOfFS[2];//ip port

            return answer;
        }

        private void uploadFile(string ip,int port,string filePath,string fileName)
        {
            TcpClient cl = new TcpClient(ip, port);
            NetworkStream ns = cl.GetStream();
            StreamWriter sw = new StreamWriter(ns);
            sw.WriteLine("!uploadfile "+fileName+"10000");

            StreamReader sr = new StreamReader(ns);
            string pipeAddr = sr.ReadLine();            //получаем адрес пайпа

            NamedPipeClientStream nps = new NamedPipeClientStream(pipeAddr);
            nps.Connect();


//это надо будет запилить в отделный поток

            FileStream fs = new FileStream(filePath,FileMode.Open);
            
            //читаем файл и пишем в канал
            byte [] buf = new byte[10];
            int bytesCount;
            while((bytesCount=fs.Read(buf,0,buf.Length))>0)
            {
                nps.WaitForPipeDrain();
                nps.Write(buf, 0, bytesCount);
            }
            nps.Close();
            fs.Close();
        }
        private void sendMessage(NetworkStream ns,string mes)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.WriteLine("!message " + mes);
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (tcpClient == null)
                return;

            if (tbMessage.Text.Trim() == string.Empty)
                return;
            NetworkStream ns = tcpClient.GetStream();
            sendMessage(ns, tbMessage.Text);
        }
    }
}
