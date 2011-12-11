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
        List<Friend>  friends = new List<Friend>();
        private ClientListener _clientListener = null;
        private Stream _connStream = null;

        private string _ip = null;
        private int _port = 0;
        private bool connected = false;

        private string _username = "";
        private string _statusMessage = "";

        public string UserName { get { return _username; } }
        public string StatusMessage { get { return _statusMessage; } }

        public ChatClient(string ip, int port)
        {
            this._ip = ip;
            this._port = port;
            new Thread(VerifyConnection).Start();
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
            connected = true;
        }

        public void SignIn(string username, string password)
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
                return;
            }

            //TODO save status message
            this._username = username;
            this._statusMessage = "My Status Message";

            //Read friends string
            int length = ReadInt();

            byte[] ba = new byte[length];
            _connStream.Read(ba, 0, length);

            string tmp = Encoding.UTF8.GetString(ba);
            Console.WriteLine(tmp);
            XDocument friendsXml = XDocument.Parse(tmp);

            friends = new List<Friend>(); 
            foreach (var friend in friendsXml.Root.Element("friends").Elements())
            {
                XElement address = friend.Element("last_address_used");
                XElement status = friend.Element("status");
                friends.Add(new Friend(friend.Attribute("username").Value, address.Attribute("ip_address").Value, 
                    Int32.Parse(address.Attribute("port").Value), status.Attribute("state").Value, status.Value));
            }

        }

        public void SignUp(string username, string password)
        {
            if (username == null || password == null)
                throw new NullReferenceException("Invalid Username or Password"); 
            Connect();

            Send(ServerOperation.SignUp, username, password);
            Console.WriteLine("response: " + ReadResponse());
        }

        public void SendFriendRequest(string username)
        {
            if (username == null )
                throw new NullReferenceException("Invalid Username"); 
            if(!connected)
                throw new NotConnectedException("You should Connect before Sending Friend Request");

            Send(ServerOperation.FriendRequest, username);
            Console.WriteLine("Friend Request Response: " + ReadResponse());
        }

        public void SignOut()
        {
            if(_tcpClient != null)
                _tcpClient.Close();
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
                string filename = filePath.Split(new char[] {'\\'}).Last();
                Console.WriteLine("Filename: " + filename);
                long length = fileStream.Length;
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

        public List<Friend> GetFriends()
        {
            return friends;
        }

        private void VerifyConnection()
        {
            while (true)
            {
                if (connected)
                {
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
                }
                Thread.Sleep(1000);
            }
        }

#region callbacks

        internal Action<string, string> ReceiveMessage = null;
        internal Func<string, long, bool> ConfirmatFileReceivement = null;
        internal Func<string, string> GetPath = null;
        internal Action<int, string> Notify = null;
        public void SetMessageReceiver(Action<string, string> method)
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
#endregion

   }
}