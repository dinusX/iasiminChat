using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;

namespace ChatServer
{
    public class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();

            server.Run();
        }
    }
}
