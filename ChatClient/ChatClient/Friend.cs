using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat
{
    class Friend
    {
        public Friend(string name, string ip, int port)
        {
            this._name = name;
            this._ip = ip;
            this._port = port;
        }

        private string _name;
        private int _port;
        private string _ip;

        public string Name
        {
            get { return _name; }
        }
        public int Port
        {
            get { return _port; }
        }
        public string Ip
        {
            get { return _ip; }
        }
    }
}
