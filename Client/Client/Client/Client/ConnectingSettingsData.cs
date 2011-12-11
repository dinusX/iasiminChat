using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace Client
{
    [Serializable()]
    class ConnectingSettingsData
    {
        public string ip, port;

        public ConnectingSettingsData()
        {
            ip = "127.0.0.1";
            port = "8001";
        }
        public ConnectingSettingsData(string _ip, string _port)
        {
            ip = _ip;
            port = _port;
        }
    }
}
