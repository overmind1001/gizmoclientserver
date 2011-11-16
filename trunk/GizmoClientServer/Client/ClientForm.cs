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

            // Тут косяк. По истечению срока соединение закрывается. И при дальнейших попытках получить стрим, падение

            //sr.BaseStream.ReadTimeout = 2000000;//20 сек ждем, затем снова пытаемся читать
            char[] sep = { ' ' };
            while (tcpClient.Connected)
            {
                try
                {
                    string cmd = sr.ReadLine();
                    string[] splited = cmd.Split(sep);

                    switch (splited[0])
                    {
                        case "!message"://прием сообщения

                            // Тут косяки. Нельзя(!) работать с контролами из другого потока, Санек, сам же говорил))

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
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine("Client. за 20 сек сервер ничего не сказал");
                }
                
            }
        }
        //регистрация на сервере (через диспетчер или напрямую)
        private bool registerMe(NetworkStream ns,string name)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
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
        //получение контакт листа
        private void getContactListFromServer(NetworkStream ns)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
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
        //получение файл листа
        private void getFileListFromServer(NetworkStream ns)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
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
        //получение адреса файл-сервера для заливки файла
        private string getFreeFileServer(NetworkStream ns)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
            sw.WriteLine("!getfreefileserver");         //посылка команды

            StreamReader sr = new StreamReader(ns);
            string answer = sr.ReadLine();          //получение ответа

            //char[] sep = { ' ' };
            //string[] addrOfFS = answer.Split(sep);//получили адрес свободного файл-сервера

            //return addrOfFS[1] + " " + addrOfFS[2];//ip port
            return answer;
        }
        //получение адреса файл-сервера, с которого можно скачать данный файл
        private string getFileServer(NetworkStream ns,string file)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
            sw.WriteLine("!getfileserver "+file);         //посылка команды

            StreamReader sr = new StreamReader(ns);
            string answer = sr.ReadLine();          //получение ответа

            //char[] sep = { ' ' };
            //string[] addrOfFS = answer.Split(sep);//получили адрес свободного файл-сервера

            //return addrOfFS[1] + " " + addrOfFS[2];//ip port

            return answer;
        }
        //выгрузка файла на сервер
        private void uploadFile(string ip,int port,string filePath,string fileName)
        {
            TcpClient cl = new TcpClient(ip, port);
            NetworkStream ns = cl.GetStream();
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
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
        //загрузка файла с сервера
        //это надо тоже запилить в отдельный поток
        private void downloadFile(string ip, int port, string Path, string fileName)
        {
            string name = Guid.NewGuid().ToString(); //может так пойдёт
            NamedPipeServerStream nps = new NamedPipeServerStream(name);            

            TcpClient cl = new TcpClient(ip, port);
            NetworkStream ns = cl.GetStream();
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
            sw.WriteLine("!downloadfile " + fileName +" "+ nps.GetImpersonationUserName());
            sw.Close();
            ns.Close();
            cl.Close();

            nps.WaitForConnection();

            FileStream fs = new FileStream(Path, FileMode.Create);

            byte[] buf = new byte[10];
            int byteCount;

            while ((byteCount = nps.Read(buf, 0, buf.Length)) > 0)
            {
                fs.Write(buf, 0, byteCount);
            }
            fs.Close();
            nps.Close();
        }
        //посылка сообщения
        private void sendMessage(NetworkStream ns,string mes)
        {
            StreamWriter sw = new StreamWriter(ns);
            sw.AutoFlush = true;
            sw.WriteLine("!message " + mes);
        }
        private void btnSend_Click(object sender, EventArgs e)
        {
            if (tcpClient == null)
                return;

            if (tbMessage.Text.Trim() == string.Empty)
                return;
            if (tcpClient.Connected != false)
            {
                NetworkStream ns = tcpClient.GetStream();
                sendMessage(ns, tbMessage.Text);
            }
            else
                MessageBox.Show("С какого то хера я отключился!");

        }
    }
}
