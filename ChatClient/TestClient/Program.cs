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
//            chatClient.SignUp("Dinu", "MyPassword");
//            chatClient2.SignUp("George", "HisPassword");
            chatClient.SignIn("Dinu", "MyPassword");
//            Thread.Sleep(100);
            
            chatClient2.SignIn("George", "HisPassword");
//            Console.WriteLine("Sending friend request");
//            chatClient.SendFriendRequest("George");

            chatClient.SetMessageReceiver(ReceiveMessage);
            chatClient2.SetMessageReceiver(ReceiveMessage);
            chatClient.SetFileReceiver(ConfirmFileReceivement,GetSavePath);
            chatClient2.SetFileReceiver(ConfirmFileReceivement, GetSavePath);

            chatClient.SendMessage("George", "Hi George");

            chatClient.SendFile("George", @"C:\Users\Dinu\Documents\Visual Studio 2010\Projects\ChatClient\nap3.gif");

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

        static bool ConfirmFileReceivement(string filename, int size)
        {
            return true;
        }

        static string GetSavePath(string filename)
        {
            return @"D:\";
        }
    }
}


//Plan de lucru
//1. Simulate Friend Request
//2. Send Message

//3. Send File

//Modify :
//Friend Request - to be also for offline users
//Logout - To be independent of Server


//Test all

//Daca moare conexiunea il facem offline


//Sending files
//1. COnfirmation
//2. Multiple Files
//3. Cancel copy
//4. Progress