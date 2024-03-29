﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dispatcher
{
    public class NetCommand
    {
        public string Ip { get; set; }
        public int Port { get; set; }
        public string sender { get; set; }
        public string cmd { get; set; }
        public string parameters {get; set; }

        public NetCommand Clone()
        {
            return new NetCommand()
            {
                Ip = this.Ip,
                Port = this.Port,
                sender = this.sender,
                cmd = this.cmd,
                parameters = this.parameters
            };
        }

        public void FromString(string s)
        {
            string[] splited = s.Split(new char[] { ' ' });
            Ip = splited[0];
            Port = Convert.ToInt32(splited[1]);
            sender = splited[2];
            cmd = splited[3];

            int len = splited[0].Length + splited[1].Length + splited[2].Length + splited[3].Length+4;
            parameters = s.Substring(len);
        }
        public override string ToString()
        {
            return String.Format("{0} {1} {2} {3} {4}",Ip,Port,sender,cmd,parameters);
        }
    }
}
