using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chat;
using System.Threading;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 8001;
            string ip = "127.0.0.1";

            ChatClient chatClient = null;
            ChatClient chatClient2 = null;

            try
            {
                chatClient = new ChatClient(ip, port);
                chatClient2 = new ChatClient(ip, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            //Decomenteaza liniile ca sa creezi useri
            chatClient.SignUp("Dinu", "MyPassword");
            chatClient2.SignUp("George", "HisPassword");

            chatClient.SignIn("Dinu", "MyPassword");
            
            Thread.Sleep(100);
            
            chatClient2.SignIn("George", "HisPassword");
//            Console.WriteLine("Sending friend request");

            //            chatClient.SendFriendRequest("George");

            chatClient.SetMessageReceiver(ReceiveMessage);
            chatClient2.SetMessageReceiver(ReceiveMessage);
            chatClient.SetFileReceiver(ConfirmFileReceivement,GetSavePath);
            chatClient2.SetFileReceiver(ConfirmFileReceivement, GetSavePath);

            chatClient.SendMessage("George", "Hi George");

//            chatClient.SendFile("George", @"C:\nap3.gif");

            chatClient.ChangeStatus("Dinu Status");

            chatClient.SignOut();
            chatClient2.SignOut(); 
        }

        static void ReceiveMessage(string username, string message)
        {
            Console.WriteLine("Received Message from: {0} \nMessage: {1}",username,message);
        }

        static void ReceiveFile()
        {
            
        }

        static bool ConfirmFileReceivement(string filename, long size)
        {
            return true;
        }

        static string GetSavePath(string filename)
        {
            return @"D:\";
        }
    }
}

