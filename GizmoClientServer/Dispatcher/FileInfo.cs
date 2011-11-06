using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dispatcher
{
    class FileInfo
    {
        public string Filename { get; set; }

        public override string ToString()
        {
            return Filename;
        }
    }
}
