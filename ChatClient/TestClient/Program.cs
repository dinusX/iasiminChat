using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 8000;
            string ip = "127.0.0.1";

            ChatClient chatClient = null;

            try
            {
                chatClient = new ChatClient(ip, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            chatClient.Login("Dinu", "MyPassword");
            chatClient.Logout();
        }
    }
}
