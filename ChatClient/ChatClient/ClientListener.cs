using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Chat
{
    class ClientListener
    {
        private static string ip = "127.0.0.1";

        public int port = 10000;

        private Action<string, string> receiveMessage = null;

        public ClientListener()
        {
            //TODO implement this
//            int i = 3;
        }

        public void Run()
        {
            bool isAvailable = false;

            IPGlobalProperties ipGlobalProperties =
                IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] endPoints =
                ipGlobalProperties.GetActiveTcpListeners();

            do
            {
                port++;
                isAvailable = true;
                foreach (IPEndPoint endPoint in endPoints)
                {
                    if (endPoint.Port != port) continue;
                    isAvailable = false;
                    break;
                }

            }
            while (!isAvailable);


            try
            {
                Console.WriteLine("Port : {0}",port);
                TcpListener server = new TcpListener(IPAddress.Parse(ip), port);

                server.Start(1000);

                Console.WriteLine("Server started running at {0}:{1}\n", ip, port);

                new Thread(() => ListenToConnections(server)).Start();


//                Console.WriteLine("Client Listener Started");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on Client Listener: " + ex);
            }
        }

        private void ListenToConnections(TcpListener server)
        {
            while (true)
            {
                try
                {
                    TcpClient client = server.AcceptTcpClient();

                    (new Thread(() => TreatConnection(client))).Start();

                    //TODO modify Exception catching
                }
                catch (SocketException se) { Console.WriteLine("SocketException: {0}", se); }
                catch (InvalidOperationException ioe) { Console.WriteLine("InvalidOperationException: {0}", ioe); }
                catch (OutOfMemoryException oome) { Console.WriteLine("OutOfMemoryException: {0}", oome); }
                catch (ThreadStateException tse) { Console.WriteLine("ThreadStateException: {0}", tse); }
                catch (ArgumentNullException ane) { Console.WriteLine("ArgmunetNullException: {0}", ane); }
            }

        }

        private void TreatConnection(TcpClient client)
        {

            Stream stream = client.GetStream();
            int option = stream.ReadByte();
            int length = stream.ReadByte();
            byte[] b = new byte[length];
            stream.Read(b, 0, length);

            if(option == 4)
            {
                Console.WriteLine("Received Message of {0} length with UTF8 conent: {1} ", length, Encoding.UTF8.GetString(b));

                stream.WriteByte(1);//Success
            }
            else
            {
                Console.WriteLine("Other option: " + option);
                Console.WriteLine("Message: " + Encoding.UTF8.GetString(b));
            }

            if (option == 101)
            {
                Console.WriteLine("Received Message from another client (me: {0})",port);
                //TODO how to treat username and ip/port
                string message = Encoding.UTF8.GetString(b);
//                Console.WriteLine("Message: " + Encoding.UTF8.GetString(b));
                if (receiveMessage != null)
                {
                    receiveMessage("hz", message);
                }
            }

            client.Close();
        }

        public void SetMessageReceiver(Action<string, string> method)
        {
            receiveMessage = method;
        }

    }
}

//Operations:
//1. LogIn
//2. ChangeStatus
//3. LogOut
//4. FriendRequest
