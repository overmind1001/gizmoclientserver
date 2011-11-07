using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MsgServer
{
    /// <summary>
    /// Тип сервера
    /// </summary>
    enum ServerType
    {
        FileServer,
        MsgServer
    };

    /// <summary>
    /// Класс сервера
    /// </summary>
    class ServerItem
    {
        private ServerType type;
    }
}
