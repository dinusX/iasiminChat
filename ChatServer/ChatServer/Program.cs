using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Text;

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
