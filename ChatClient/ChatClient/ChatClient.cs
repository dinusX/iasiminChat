using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace Chat
{
    public class ChatClient
    {
        private TcpClient _tcpClient = null;

        public ChatClient(string ip, int port)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect("127.0.0.1",8001);

            Console.WriteLine("Connected");
 
        }

        public void Login(string username, string password)
        {
            if (username == null || password == null)
                throw new Exception("hz1"); //TODO

            Stream stm = _tcpClient.GetStream();


            stm.WriteByte(1);

            ASCIIEncoding asen = new ASCIIEncoding();
            byte[] ba = asen.GetBytes(username);

            stm.WriteByte((byte)ba.Length);
            stm.Write(ba, 0, ba.Length);

            ba = asen.GetBytes(password);

            stm.WriteByte((byte)ba.Length);
            stm.Write(ba, 0, ba.Length);

            Console.WriteLine("First response: " + stm.ReadByte());
            Console.WriteLine("Second response: " + stm.ReadByte());
            
        }


        public void Logout()
        {
            _tcpClient.Close();
        }

        public void SendMessage(string user, string message)
        {
            
        }

        //TODO from string to byte or int
        public void ChangeStatus(string status)
        {
            
        }

        public void SendFile(string user, string filePath)
        {
            
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