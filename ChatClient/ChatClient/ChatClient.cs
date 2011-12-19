using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;

namespace Chat
{
    public class ChatClient
    {
        private TcpClient _tcpClient = null;
        private int clientPort = 0;
        private List<Friend> friends = new List<Friend>();
        private ClientListener _clientListener = null;
        private Stream _connStream = null;

        private string _ip = null;
        private int _port = 0;
        private bool connected = false;

        private string _username = "";
        private string _statusMessage = "";
        private string _logoFileName = "";

        private Thread _verifyThread = null;

        public string UserName
        {
            get { return _username; }
        }

        public string StatusMessage
        {
            get { return _statusMessage; }
        }

        public string LogoFileName
        {
            get { return _logoFileName; }
        }

        public ChatClient(string ip, int port)
        {
            this._ip = ip;
            this._port = port;
        }


        private void Connect()
        {
            if (connected)
                return;
            _tcpClient = new TcpClient();
            _tcpClient.Connect(_ip, _port);

            _connStream = _tcpClient.GetStream();

            Console.WriteLine("Connected");

            _clientListener = new ClientListener(this);
            _clientListener.Run();
            clientPort = _clientListener.port;

            _verifyThread = new Thread(VerifyConnection);
            _verifyThread.Start();

            connected = true;
        }

        public int SignIn(string username, string password)
        {
            if (username == null || password == null)
                throw new NullReferenceException("Invalid Username or Password");

            Connect(); //TODO need errors on connection
            //TODO rethink need to return int as response

            Send(ServerOperation.SignIn, username, password, clientPort);

            byte response = ReadResponse();
            Console.WriteLine("Response: " + response);

            if (response != 0)
            {
                //TODO failure
                return response;
            }

            //TODO save status message
            this._username = username;
//            this._statusMessage = "My Status Message";

            //Read friends string
            int length = ReadInt();

            byte[] ba = new byte[length];
            _connStream.Read(ba, 0, length);

            string tmp = Encoding.UTF8.GetString(ba);
            Console.WriteLine(tmp);
            XDocument dataXml = XDocument.Parse(tmp);

            this._statusMessage = dataXml.Root.Element("myself").Element("status").Value;

            if (dataXml.Root.Element("myself").Element("img")!= null); 
            this._logoFileName = dataXml.Root.Element("myself").Element("img").Value;
            
            friends = new List<Friend>();
            foreach (var friend in dataXml.Root.Element("friends").Elements())
            {
                XElement address = friend.Element("last_address_used");
                XElement status = friend.Element("status");
                Friend friendItem = new Friend(friend.Attribute("username").Value, address.Attribute("ip_address").Value,
                                            Int32.Parse(address.Attribute("port").Value),
                                            status.Attribute("state").Value,
                                            status.Value);
                if (friend.Element("img") != null)
                    friendItem.LogoFileName = friend.Element("img").Value;
                friends.Add(friendItem);
            }

            if (dataXml.Root.Element("offline_messages") != null && dataXml.Root.Element("offline_messages").Elements().Count() > 0)
            {
                new Thread(delegate()
                {
                    Thread.Sleep(100);
                    try
                    {
                        if (ReceiveMessage != null)
                            foreach (var element in dataXml.Root.Element("offline_messages").Elements())
                            {
                                ReceiveMessage(element.Attribute("from").Value, element.Value, DateTime.Parse(element.Attribute("date").Value));
                            }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exc: " + ex);
//                        throw;
                    }
                }).Start();
            }

            if (dataXml.Root.Element("friend_requests") != null && dataXml.Root.Element("friend_requests").Elements().Count() > 0)
            {
                new Thread(delegate()
                {
                    Thread.Sleep(100);
                    string friend = "";
                    try
                    {
                        foreach (var element in dataXml.Root.Element("friend_requests").Elements())
                        {
                            friend = element.Attribute("username").Value;
                            if (ConfirmFriendRequest != null && !ConfirmFriendRequest(friend, element.Value))
                                DenyFriend(friend);
                            else
                            {
                                AcceptFriend(friend);
                            }
//                            ReceiveMessage(element.Attribute("from").Value, element.Value, DateTime.Parse(element.Attribute("date").Value));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exc: " + ex);
                        //                        throw;
                    }
                }).Start();
            }

            //TODO implement
//            foreach (var message in dataXml.Root.Element("offline_messages").Elements())
//            {
//                if (ReceiveMessage != null)
//                {
//                    ReceiveMessage(message.Attribute("from").Value, message.Value);
//                }
//            }

            return 0;

        }

        public int SignUp(string username, string password)
        {
            if (username == null || password == null)
                throw new NullReferenceException("Invalid Username or Password");
            Connect();

            Send(ServerOperation.SignUp, username, password);
            int response = ReadResponse();
            Console.WriteLine("response: " + response);
            return response;
        }

        public int SendFriendRequest(string username, string message)
        {
            if (username == null)
                throw new NullReferenceException("Invalid Username");
            if (!connected)
                throw new NotConnectedException("You should Connect before Sending Friend Request");

            Send(ServerOperation.FriendRequest, username, message);
            int response = ReadResponse();
//            if (response == 0)
//            {
//                string xmlFile = ReadString(true);
//                AddFrind(xmlFile);
////                Notify(3, xmlFile);
//            }
            Console.WriteLine("Friend Request Response: " + response);
            return response;
        }

        internal void AddFrind(string xmlString)
        {
            XDocument dataXml = XDocument.Parse(xmlString);

            var friend = dataXml.Root;
            XElement address = friend.Element("last_address_used");
            XElement status = friend.Element("status");
            Friend friendItem = new Friend(friend.Attribute("username").Value, address.Attribute("ip_address").Value,
                                           Int32.Parse(address.Attribute("port").Value), status.Attribute("state").Value,
                                           status.Value);
            if (friend.Element("img") != null)
                friendItem.LogoFileName = friend.Element("img").Value;
            friends.Add(friendItem);

            if (Notify != null)
                Notify(2, null);
        }

        internal void AcceptFriend(string username, bool accepted = true)
        {
//            string xmlString = ReadString(stream, true);
//            _chatClient.AddFrind(xmlString);

//

            byte acceptResp = 1; //Failure
            if (accepted)
                acceptResp = 0; //Success
            Send(ServerOperation.AcceptFriend, username, acceptResp);

            int resp = ReadResponse();
            if (accepted && resp == 0)
            {
                //Read friend xml string
                string xmlString = ReadString(true);
                AddFrind(xmlString);

                if (Notify != null)
                    Notify(2, null);
            }

        }

        internal void DenyFriend(string username)
        {
            AcceptFriend(username, false);
        }

        public void SignOut()
        {
            connected = false;
            if (_tcpClient != null)
                _tcpClient.Close();
            if (_clientListener != null)
                _clientListener.Stop();
//            if(_verifyThread != null && _verifyThread.IsAlive)
//                _verifyThread.Abort();
        }

        //TODO from string to byte or int
        public void ChangeStatus(string statusMessage)
        {
            if (statusMessage == null)
                throw new NullReferenceException("Invalid Status");
            if (!connected)
                throw new NotConnectedException("You should Connect before Changing Status");
            Send(ServerOperation.ChangeStatus, statusMessage);
            Console.WriteLine("Change Status Response: " + ReadResponse());
        }
        
        public void SendMessage(string username, string message)
        {
            var friend = GetFriend(username);
            if(friend != null)
            {
                try
                {
                    TcpClient friendClient = new TcpClient();
                    friendClient.Connect(friend.Ip, friend.Port);

                    Console.WriteLine("Connecting to : " + friend.Port);

                    Stream stream = friendClient.GetStream();

                    stream.WriteByte((byte)ClientOperation.ReceiveMessage);

                    byte[] b = Encoding.UTF8.GetBytes(UserName);
                    stream.WriteByte((byte)b.Length);
                    stream.Write(b, 0, b.Length);


                    b = Encoding.UTF8.GetBytes(message);
                    //TODO change to int
                    stream.WriteByte((byte)b.Length);
                    stream.Write(b, 0, b.Length);
                }
                catch (Exception ex)
                {
                    AddOfflineMessage(username, message);
                }
            }
            //TODO else throw errors
        }

        public void AddOfflineMessage(string username, string message)
        {
            if (username == null)
                throw new NullReferenceException("Invalid Username");
            if (message == null)
                throw new NotConnectedException("Invalid message");
            Send(ServerOperation.AddOfflineMessage, username);
            SendLongString(message);
            Console.WriteLine("Change Status Response: " + ReadResponse());
        }

        public string ChangeLogo(byte[] aBytes)
        {
            Send(ServerOperation.ChangeLogo);

            Send(aBytes.Length);
            _connStream.Write(aBytes, 0, aBytes.Length);
            string filename = ReadString();
            int resp = ReadResponse();
            return filename;
        }

        private Friend GetFriend(string username)
        {
            //TODO maybe change friends to Dictionary
//            foreach (var friend in friends)
//            {
//                if (friend.Name == username)
//                {
//                    return friend;
//                    break;
//                }
//            }
//            Console.WriteLine("Trying to find unknown friend: " + username);
            try
            {
                return friends.Where(f => f.Name == username).First();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Trying to find unknown friend: " + username);
            }

            return null;
        }

        public void SendFile(string username, string filePath)
        {
            new Thread(delegate()
            {
                try
                {
                    //TODO errors
                    FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    string filename = filePath.Split(new char[] { '\\' }).Last();
                    Console.WriteLine("Filename: " + filename);
                    int length = (int)fileStream.Length; //TODO improve
                    var friend = GetFriend(username);
                    if (friend != null)
                    {
                        //TODO connect and send message
                        TcpClient friendClient = new TcpClient();
                        friendClient.Connect(friend.Ip, friend.Port);

                        Stream stream = friendClient.GetStream();
                        stream.WriteByte(102); //TODO to enum

                        //send filename
                        byte[] b = Encoding.UTF8.GetBytes(filename);
                        stream.WriteByte((byte)filename.Length);
                        stream.Write(b, 0, b.Length);


                        b = BitConverter.GetBytes(length);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(b);

                        //write content
                        stream.Write(b, 0, b.Length);

                        if (stream.ReadByte() != 0)
                        {
                            //FIle ignored
                            if (Notify != null)
                                Notify(3, "FIle was not accepted");
                            fileStream.Close(); 
                            return;
                        }
                        int mb10 = 10*1024*1024; //10 megabytes (mem cache)
                        if (length <= mb10)
                        {
                            b = new byte[length];
                            fileStream.Read(b, 0, length); 
                            stream.Write(b, 0, b.Length);
                        }
                        else
                        {
                            int total = length;
                            b = new byte[mb10];

                            while (total > 0)
                            {
                                int localTotal = mb10;
                                if(total <= mb10)
                                    localTotal = total;
                                total -= localTotal;
                                fileStream.Read(b, 0, localTotal); //TODO change this conversion
                                stream.Write(b, 0, localTotal);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Notify(3, ex.ToString());
                }

            }).Start();

        }

        private int ReadInt()
        {
            byte[] ba = new byte[4];
            _connStream.Read(ba, 0, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(ba);

            return BitConverter.ToInt32(ba, 0);
        }

        private byte ReadResponse()
        {
            return (byte)_connStream.ReadByte();
        }

        private void Send(ServerOperation operation, params object[] objects)
        {
            _connStream.WriteByte((byte)operation);
            Send(objects);
        }

        private void Send(params object[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj is byte)
                {
                    _connStream.WriteByte((byte)obj);
                }
                else
                if (obj is int)
                {
                    byte[] ba = BitConverter.GetBytes((int)obj);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(ba);
                    _connStream.Write(ba, 0, 4);
                }
                else
                {
                    byte[] ba = Encoding.UTF8.GetBytes(obj.ToString());

                    _connStream.WriteByte((byte)ba.Length);
                    if(ba.Length != 0)
                        _connStream.Write(ba, 0, ba.Length);
                }
            }
        }

        private void SendLongString(string text)
        {
//                if (obj is byte)
//                {
//                    _connStream.WriteByte((byte)obj);
//                }
//                else
//                if (obj is int)
//                {
//                    byte[] ba = BitConverter.GetBytes((int)obj);
//                    if (BitConverter.IsLittleEndian)
//                        Array.Reverse(ba);
//                    _connStream.Write(ba, 0, 4);
//                }
//                else
//                {

            byte[] ba = Encoding.UTF8.GetBytes(text);

            byte[] blen = BitConverter.GetBytes(ba.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(blen);

            _connStream.Write(blen, 0, blen.Length);

//                    Encoding.UTF8.GetBytes(obj.ToString());
//                    _connStream.WriteByte((byte)ba.Length);
            if (ba.Length != 0)
                _connStream.Write(ba, 0, ba.Length);
//                }
        }

        internal void UpdateFriendAddress(string username, string host, int friendPort)
        {
            var friend = GetFriend(username);
            if (friend != null)
            {
                friend.Ip = host;
                friend.Port = friendPort;
                friend.Status = "online";
            }
        }

        internal void UpdateFriendStatus(string username, string status, string statusMessage = null)
        {
            var friend = GetFriend(username);
            if (friend != null)
            {
                friend.Status = status;
                if(statusMessage != null)
                    friend.StatusMessage = statusMessage;
            }
        }

        internal void UpdateFriendLogo(string username, string filename)
        {
            var friend = GetFriend(username);
            if (friend != null)
            {
                friend.LogoFileName = filename;
            }
        }

        public List<Friend> GetFriends()
        {
            return friends;
        }

        private void VerifyConnection()
        {
//            while (true)
            while(connected)
            {
//                if (connected)
//                {
                    try
                    {
                        _tcpClient.Client.Send(new byte[0]);
                    }
                    catch (Exception ex)
                    {
                        connected = false;
                        if (Notify != null)
                            Notify(1, "Connection Died"); //TODO change to Enum
                    }
//                }
                Thread.Sleep(1000);
                Console.WriteLine("Verify");
            }
        }

        private string ReadString(bool intLength = false)
        {
            int length = 0;
            if (!intLength)
                length = _connStream.ReadByte();
            else
            {
                length = ReadInt();
            }

            if (length == 0)
                return "";
            int total = length, offset = 0;
            byte[] b = new byte[length];
            int k = 0;
            while (total > 0)
            {
                k = _connStream.Read(b, offset, total);
                total -= k;
                offset += k;

                //                _chatClient.Notify(3, "received: " + k + " remained " + total);
            }

            return Encoding.UTF8.GetString(b);
        }

#region callbacks

        internal Action<string, string, DateTime> ReceiveMessage = null;
        internal Func<string, long, bool> ConfirmatFileReceivement = null;
        internal Func<string, string> GetPath = null;
        internal Action<int, string> Notify = null;
        internal Func<string, string, bool> ConfirmFriendRequest = null; 
        public void SetMessageReceiver(Action<string, string, DateTime> method)
        {
            ReceiveMessage = method;
        }

        public void SetFileReceiver(Func<string, long, bool> confirmationMethod, Func<string, string> getPathMethod)
        {
            ConfirmatFileReceivement = confirmationMethod;
            GetPath = getPathMethod;
        }

        public void SetNotifier(Action<int, string> method)
        {
            Notify = method;
        }

        public void SetFriendRequestConfirmation(Func<string, string, bool> method)
        {
            ConfirmFriendRequest = method;
        }
#endregion

   }
}