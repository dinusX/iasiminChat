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

        public int port = 10000; //TODO Change to Random Port

        private ChatClient _chatClient = null;

        public ClientListener(ChatClient chatClient)
        {
            if (chatClient == null)
                throw new NullReferenceException("Invalid chatClient parameter");
            _chatClient = chatClient;
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
            int operation = stream.ReadByte();

            byte[] b = null;

            if (operation < 100) //Then is NotifyOperation
            {
                string username = ReadString(stream);

                switch ((NotifyOperation)operation)
                {
                    case NotifyOperation.LogIn:
                        string host = ReadString(stream);
                        int friendPort = ReadInt(stream);
                        _chatClient.UpdateFriendAddress(username, host, friendPort);
                        Console.WriteLine("Login Notif");
                        //TODO change number to enum
                        if(_chatClient.Notify != null)
                            _chatClient.Notify(2, "update login me: "+port+", other: "+ host +":"+ friendPort);
                        break;
                    case NotifyOperation.ChangeStatus:
                        string statusMessage = ReadString(stream);
                        _chatClient.UpdateFrindStatus(username, "online", statusMessage);
                        Console.WriteLine("Change Status Notif");
                        if(_chatClient.Notify != null)
                            _chatClient.Notify(2, null);
                        break;
                    case NotifyOperation.LogOut:
                        _chatClient.UpdateFrindStatus(username, "offline");
                        Console.WriteLine("Offline Notif");
                        if(_chatClient.Notify != null)
                            _chatClient.Notify(2, null);
                        break;
                    case NotifyOperation.FriendRequest:
//                        string friend = ReadString(stream);
//                        _chatClient.Notify(3, "friend: " + friend);
                        //TODO ask user ??

                        string message = "";
//                        if (stream.ReadByte() == 1) //Receiving Invitation Message
//                        {
                            message = ReadString(stream);
//                        }
//                            _chatClient.Notify(3, "message: " + message);
                            if (_chatClient.ConfirmFriendRequest != null && !_chatClient.ConfirmFriendRequest(username, message))
                                stream.WriteByte(0); //Denied
                            else
                            {
                                stream.WriteByte(1); //Success
//                                if (_chatClient.Notify != null) 
//                                    _chatClient.Notify(3, "Awaiting for xml file");
                                string xmlString = ReadString(stream, true);
                                _chatClient.AddFrind(xmlString);

                                if (_chatClient.Notify != null) 
                                    _chatClient.Notify(2, null);
                            }
                        //Get Confirmation
                        break;
                        case NotifyOperation.AddNewFriend:

                        break;
                    default:
//                        string message = ReadString(stream);
                        Console.WriteLine("Other operation: " + operation);
                        Console.WriteLine("Data: " + username);
                        break;
                }
            }
            else
            {
                switch ((ClientOperation)operation)
                {
                    case ClientOperation.ReceiveMessage:
                        {
//                            int length = stream.ReadByte();
                            string username = ReadString(stream);
                            string message = ReadString(stream);
                            Console.WriteLine("Received Message from another client (me: {0})", port);

                            if (_chatClient.ReceiveMessage != null)
                            {
                                _chatClient.ReceiveMessage(username, message);
                                //TODO remove
//                                if (_chatClient.Notify != null)
//                                _chatClient.Notify(10,
//                                                   "me: " + port + " other: " + client.Client.RemoteEndPoint.ToString());
                            }
                        }
                        break;
                    case ClientOperation.ReceiveFile:
                        {
                            //TODO maybe read username
                            string filename = ReadString(stream);
                            int size = ReadInt(stream);
                            if (_chatClient.ConfirmatFileReceivement != null && _chatClient.ConfirmatFileReceivement(filename, size))
                            {

                                string path = _chatClient.GetPath(filename);
                                //TODO check if path ok
                                //TODO load file from stream + check if is not directory

                                if (path != null)
                                    stream.WriteByte(0); //Send file
                                else
                                {
                                    stream.WriteByte(1); //Not Accepted
                                    return;
                                }
                                    


                                try
                                {
                                    Console.WriteLine("Receiving file");

                                    b = new byte[size];

                                    int length = size;
                                    int k = 0, offset = 0;
                                    while (length > 0)
                                    {
                                        k = stream.Read(b, offset, length);
                                        Thread.Sleep(10);
                                        Console.WriteLine("Received: " + k);
                                        length -= k;
                                        offset += k;
//                                        _chatClient.Notify(3, "Received: " + k);
                                        //TODO + abort
                                    }
                                    
                                    FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
                                    fileStream.Write(b, 0, b.Length);
                                    fileStream.Close();
                                    fileStream.Dispose(); 

                                    Console.WriteLine("Successfully received");
                                    if(_chatClient.Notify != null)
                                    _chatClient.Notify(3, "File Successfully received");
                                }
                                catch (Exception ex)
                                {
                                    //TODO how to treat?
                                    if (_chatClient.Notify != null) 
                                        _chatClient.Notify(3, "Error: " + ex);
                                    Console.WriteLine(ex);
                                }
                            }
                            else
                                stream.WriteByte(1); //Not Accepted

                        }
                        break;
                }
            }
            

            client.Close();
        }

        private int ReadInt(Stream stream)
        {
            byte[] ba = new byte[4];
            stream.Read(ba, 0, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(ba);

            return BitConverter.ToInt32(ba, 0);
        }

//        private long ReadInt(Stream stream)
//        {
//            byte[] ba = new byte[8];
//            stream.Read(ba, 0, 8);
//
//            if (BitConverter.IsLittleEndian)
//                Array.Reverse(ba);
//
//            return BitConverter.ToInt64(ba, 0);
//        }

        private string ReadString(Stream stream, bool intLength = false)
        {
            int length = 0;
            if (!intLength)
                length = stream.ReadByte();
            else
            {
                length = ReadInt(stream);
            }

            if (length == 0)
                return "";
            int total = length, offset = 0;
            byte[] b = new byte[length];
            int k = 0;
            while (total > 0)
            {
                k = stream.Read(b, offset, length);
                total -= k;
                offset += k;

//                _chatClient.Notify(3, "received: " + k + " remained " + total);
            }
            
            return Encoding.UTF8.GetString(b);
        }

    }
}

