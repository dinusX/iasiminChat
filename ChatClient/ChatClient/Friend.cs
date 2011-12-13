using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat
{
    public class Friend
    {
        public Friend(string name, string ip, int port, string status, string statusMessage = null)
        {
            this._name = name;
            this._ip = ip;
            this._port = port;
            this._status = status;
            if(_statusMessage != null)
                this._statusMessage = statusMessage;
//            this._statusMessage += port.ToString();
        }

        private string _name;
        private int _port;
        private string _ip;
        private string _status;
        private string _statusMessage = "";

        public string Name
        {
            get { return _name; }
        }
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
        public string Ip
        {
            get { return _ip; }
            set { _ip = value; }
        }
        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { _statusMessage = value; }
        }

        public bool Online()
        {
            return this.Status == "online";
        }
    }
}
