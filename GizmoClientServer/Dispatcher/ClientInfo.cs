using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dispatcher
{
    class ClientInfo
    {
        public string ClientName { get; set; }

        public override string ToString()
        {
            return ClientName;
        }
    }
}
