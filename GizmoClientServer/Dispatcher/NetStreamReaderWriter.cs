using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Dispatcher
{
    public class NetStreamReaderWriter
    {
        private StreamReader sr;
        private StreamWriter sw;

        public bool AutoFlush 
        {
            get 
            {
                return sw.AutoFlush;
            }
            set 
            {
                sw.AutoFlush = value;
            }
        }
        public int ReadTimeout
        {
            get
            {
                return sr.BaseStream.ReadTimeout;
            }
            set
            {
                sr.BaseStream.ReadTimeout = value;
            }
        }

        public NetStreamReaderWriter(Stream s)
        {
            sr = new StreamReader(s);
            sw = new StreamWriter(s);
            AutoFlush = true;
            ReadTimeout = 60000;
        }

        public void WriteLine(string line)
        {
            sw.WriteLine(line);
        }
        public string ReadLine()
        {
            return sr.ReadLine();
        }

        //для команд
        public void WriteCmd(NetCommand cmd)
        {
            WriteLine(cmd.ToString());
        }
        public NetCommand ReadCmd()
        {
            NetCommand cmd = new NetCommand();
            string line = ReadLine();
            cmd.FromString(line);
            return cmd;
        }
    }
}
