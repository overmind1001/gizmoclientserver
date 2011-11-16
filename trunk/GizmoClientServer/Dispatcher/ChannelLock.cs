﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace Dispatcher
{
    class ChannelLock
    {
        Mutex mutex;
        NetStreamReaderWriter netStream;

        public ChannelLock(Stream stream)
        {
            mutex = new Mutex(false);
            netStream = new NetStreamReaderWriter(stream);
        }

        /// <summary>
        /// Метод вызывается перед началом(инициацией) любого взаимодействия с другой стороной. Если канал занят, то ожидание. Если сбой, то возвращает false.
        /// </summary>
        /// <returns>false если не удалось начать транзакцию(если другая сторона в этот момент тоже пытается занять канал, либо не отвечает)</returns>
        bool Begin()    
        {
            string ans = "";
            mutex.WaitOne();
            netStream.WriteLine("!beginlockrequest");
            try
            {
                ans = netStream.ReadLine();
            }
            catch (Exception ex)
            {
                mutex.ReleaseMutex();
                return false;
            }

            if (ans != "!lockaccepted")
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Вызывается по окончании взаимодействия с другой стороной. Освобождает канал.
        /// </summary>
        void End()
        {
            netStream.WriteLine("!endlock");
            mutex.ReleaseMutex();
        }
        /// <summary>
        /// Вызывается для подтверждения запроса о начале блокировки канала. И блокирует канал со 2 стороны в случае удачи.
        /// </summary>
        /// <returns>true если удалось подтвердить</returns>
        bool Accept()
        {
            if (!mutex.WaitOne(0))
                return false;
            else
            {
                netStream.WriteLine("!lockaccepted");
                return true;
            }
        }
    }
}