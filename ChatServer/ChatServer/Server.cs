using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace ChatServer
{
    public class Server
    {
        private Random random = new Random();
        private uint port;

        public Server()
        {
            bool isAvailable = true;

            do
            {
                this.port = (uint) (random.Next(50000) + 10000);

                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnectionInformationArray = ipGlobalProperties.GetActiveTcpConnections();

                foreach (TcpConnectionInformation tcpi in tcpConnectionInformationArray)
                {
                    if ((uint) tcpi.LocalEndPoint.Port == port)
                    {
                        isAvailable = false;

                        break;
                    }
                }
            }
            while (!isAvailable);
        }

        public uint Port { get { return this.port; } }
    }
}
