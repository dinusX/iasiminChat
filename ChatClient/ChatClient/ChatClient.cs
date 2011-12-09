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

        public ChatClient(string ip, int port)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(ip, port);

            Console.WriteLine("Connected");
            
            _clientListener = new ClientListener();
            _clientListener.Run();
            clientPort = _clientListener.port;
            
            
            
            //TODO think how to get port
            //And to be sure that it is correct

            //Get Port

            //TODO rethink for catching errors and other things ..., or no it shouldn't die


            //TODO Eu trebuie sa fac request once per 10/60 ... sec p/u a verifica daca este net
        }

        public void SignIn(string username, string password)
        {
            if (username == null || password == null)
                throw new Exception("hz1"); //TODO

            Stream stm = _tcpClient.GetStream();

            stm.WriteByte((byte)ServerOperation.SignIn);

            Send(stm, username);
            Send(stm, password);
            Send(stm, clientPort);

            byte response = (byte)stm.ReadByte();
            Console.WriteLine("Response: " + response);

            
            if (response != 0)
            {
                //TODO failure
                return;
            }

            //Read friends string
            byte[] ba = new byte[4];
            stm.Read(ba, 0, 4);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(ba);

            int length = BitConverter.ToInt32(ba, 0);

            ba = new byte[length];
            stm.Read(ba, 0, length);

            //string friendsXml = Encoding.UTF8.GetString(ba);
            XDocument friendsXml = XDocument.Parse(Encoding.UTF8.GetString(ba));

            friends = new List<Friend>(); 
            foreach (var friend in friendsXml.Root.Elements())
            {
                XElement address = friend.Element("last_address_used");
                friends.Add(new Friend(friend.Attribute("username").Value, address.Attribute("ip_address").Value, 
                    Int32.Parse(address.Attribute("notif_port").Value)));
            }

            Console.WriteLine("friends text: " + friendsXml);

        }

        public void SignUp(string username, string password)
        {
            if (username == null || password == null)
                throw new Exception("hz1"); //TODO

            Stream stm = _tcpClient.GetStream();

            stm.WriteByte((byte)ServerOperation.SignUp);


            Send(stm, username);
            Send(stm, password);
            Console.WriteLine("response: " + stm.ReadByte());

        }

        //TODO solve with internet deconnection

        //TODO Execute this functions when online
        public void SendFriendRequest(string username)
        {
            if (username == null )
                throw new Exception("hz1"); //TODO

            Stream stm = _tcpClient.GetStream();

            //TODO add operation
            stm.WriteByte((byte)ServerOperation.FriendRequest);

            Send(stm, username);

            Console.WriteLine("Friend Request Response: " + stm.ReadByte());
        }


        public void SignOut()
        {
            _tcpClient.Close();
            //TODO think
        }

        //TODO from string to byte or int
        public void ChangeStatus(string status)
        {
            if (status == null)
                throw new Exception("hz1"); //TODO

            Stream stm = _tcpClient.GetStream();

            //TODO add operation
            stm.WriteByte((byte)ServerOperation.ChangeStatus);


            Send(stm,status);

            //TODO change status Message + Online/NA/Invisible ..

            //TODO
            Console.WriteLine("Friend Request Response: " + stm.ReadByte());
        }

        
        public void SendMessage(string user, string message)
        {
            //TODO maybe change friends to Dictionary
            foreach (var friend in friends)
            {
                if (friend.Name == user)
                {
                    //TODO connect and send message
                    TcpClient friendClient = new TcpClient();
                    friendClient.Connect(friend.Ip, friend.Port);

                    Stream stream = friendClient.GetStream();
                    stream.WriteByte(101);

                    byte[] b = Encoding.UTF8.GetBytes(message);

                    //TODO change to int
                    stream.WriteByte((byte)b.Length);
                    stream.Write(b,0,b.Length);
                    break;
                }
            }
        }


        public void SendFile(string user, string filePath)
        {

        }

        private void Send(Stream stream, String obj)
        {
            byte[] ba = Encoding.UTF8.GetBytes(obj);

            stream.WriteByte((byte)ba.Length);
            stream.Write(ba, 0, ba.Length);
        }

        private void Send(Stream stream, int number)
        {
            byte[] ba = BitConverter.GetBytes(number);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(ba);

            stream.Write(ba, 0, 4);
        }

        //Func<string, string, int>
        public void SetMessageReceiver(Action<string,string> method)
        {
            _clientListener.SetMessageReceiver(method);
        }


        //Events: 
        //Notification
        //User Changed Status
        //User Sent You message

    }
}

//Serverul trebuie sa pastreze ultimul ip si port
//Intrebari:
//1. Cite Statusuril vor fi