using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace Chat
{
    public class ChatClient
    {
        private TcpClient _tcpClient = null;
        private int clientPort = 0;
        List<Friend>  friends = new List<Friend>();
        private ClientListener _clientListener = null;
        private Stream _connStream = null;

        public ChatClient(string ip, int port)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(ip, port);
            _connStream = _tcpClient.GetStream();

            Console.WriteLine("Connected");
            
            _clientListener = new ClientListener(this);
            _clientListener.Run();
            clientPort = _clientListener.port;

            //TODO stop when error

        }

        public void SignIn(string username, string password)
        {
            if (username == null || password == null)
                throw new NullReferenceException("Invalid Username or Password"); 

            Send(ServerOperation.SignIn, username, password, clientPort);

            byte response = ReadResponse();
            Console.WriteLine("Response: " + response);
            
            if (response != 0)
            {
                //TODO failure
                return;
            }

            //Read friends string
            int length = ReadInt();

            byte[] ba = new byte[length];
            _connStream.Read(ba, 0, length);

            //TODO remove this line
            string tmp = Encoding.UTF8.GetString(ba);
            Console.WriteLine("friends: " + tmp);
            XDocument friendsXml = XDocument.Parse(tmp);

            friends = new List<Friend>(); 
            foreach (var friend in friendsXml.Root.Element("friends").Elements())
            {
                XElement address = friend.Element("last_address_used");
                XElement status = friend.Element("status");
                friends.Add(new Friend(friend.Attribute("username").Value, address.Attribute("ip_address").Value, 
                    Int32.Parse(address.Attribute("port").Value), status.Attribute("state").Value, status.Value));
            }

//            Console.WriteLine("friends text: " + friendsXml);
        }

        public void SignUp(string username, string password)
        {
            if (username == null || password == null)
                throw new NullReferenceException("Invalid Username or Password"); 

            Send(ServerOperation.SignUp, username, password);
            Console.WriteLine("response: " + ReadResponse());
        }

        public void SendFriendRequest(string username)
        {
            if (username == null )
                throw new NullReferenceException("Invalid Username"); 

            Send(ServerOperation.FriendRequest, username);
            Console.WriteLine("Friend Request Response: " + ReadResponse());
        }

        public void SignOut()
        {
            _tcpClient.Close();
        }

        //TODO from string to byte or int
        public void ChangeStatus(string status)
        {
            if (status == null)
                throw new NullReferenceException("Invalid Status"); 

            Send(ServerOperation.ChangeStatus, status);
            Console.WriteLine("Friend Request Response: " + ReadResponse());
        }
        
        public void SendMessage(string username, string message)
        {
            var friend = GetFriend(username);
            if(friend != null)
            {
                TcpClient friendClient = new TcpClient();
                friendClient.Connect(friend.Ip, friend.Port);

                Console.WriteLine("Connecting to : " + friend.Port);

                Stream stream = friendClient.GetStream();

                stream.WriteByte((byte)ClientOperation.ReceiveMessage);

                byte[] b = Encoding.UTF8.GetBytes(message);

                //TODO change to int
                stream.WriteByte((byte)b.Length);
                stream.Write(b,0,b.Length);
            }
            //TODO else throw errors
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
                //TODO errors
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                long length = fileStream.Length;
                var friend = GetFriend(username);
                if (friend != null)
                {
                    //TODO connect and send message
                    TcpClient friendClient = new TcpClient();
                    friendClient.Connect(friend.Ip, friend.Port);

                    Stream stream = friendClient.GetStream();
                    stream.WriteByte(102); //TODO to enum

                    byte[] b = BitConverter.GetBytes(length);

                    Console.WriteLine("Len: " + length + " " + (int)length);
                    //TODO change to int
                    //stream.WriteByte((byte)b.Length);

                    stream.Write(b, 0, b.Length);
                    b = new byte[length];
                    fileStream.Read(b, 0, (int)length); //TODO change this conversion
                    stream.Write(b, 0, b.Length);
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
                if(obj is int)
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
                    _connStream.Write(ba, 0, ba.Length);
                }
            }
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

        internal void UpdateFrindStatus(string username, string status, string statusMessage = null)
        {
            var friend = GetFriend(username);
            if (friend != null)
            {
                friend.Status = status;
                if(statusMessage != null)
                    friend.StatusMessage = statusMessage;
            }
        }

#region callbacks
        internal Action<string, string> ReceiveMessage = null;
        internal Func<string, int, bool> ConfirmatFileReceivement = null;
        internal Func<string, string> GetPath = null;

        public void SetMessageReceiver(Action<string, string> method)
        {
            ReceiveMessage = method;
        }

        public void SetFileReceiver(Func<string, int, bool> confirmationMethod, Func<string, string> getPathMethod)
        {
            ConfirmatFileReceivement = confirmationMethod;
            GetPath = getPathMethod;
        }

#endregion

   }
}