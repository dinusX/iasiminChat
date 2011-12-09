using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace ChatServer
{
    public class Server
    {
        private int port;
        private IPAddress localIP = IPAddress.Parse("127.0.0.1");
        private Random random = new Random();
        private TcpListener server;
        private ReaderWriterLock rwl = new ReaderWriterLock();

        private class Tuple<Fi, S, T, Fo>
        {
            public Tuple();

            public Tuple(Fi first, S second, T third)
            {
                this.First = first;
                this.Second = second;
                this.Third = third;
            }

            public Fi First { get; set; }
            public S Second { get; set; }
            public T Third { get; set; }
            public Fo Fourth { get; set; }
        }

        public Server()
        {
            bool isAvailable = true;

            do
            {
                this.port = (random.Next(50000) + 10000);

                IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] tcpConnectionInformationArray = ipGlobalProperties.GetActiveTcpConnections();

                foreach (TcpConnectionInformation tcpi in tcpConnectionInformationArray)
                {
                    if (tcpi.LocalEndPoint.Port == port)
                    {
                        isAvailable = false;

                        break;
                    }
                }
            }
            while (!isAvailable);

            if (!File.Exists(@"files\users.xml"))
            {
                XDocument users = new XDocument();
                FileStream fs = new FileStream(@"files\users.xml", FileMode.Create);

                if (!Directory.Exists("files"))
                    Directory.CreateDirectory("files");
                
                users.Add(new XElement("users"));
                users.Save(fs);
                fs.Close();
            }
        }

        public void Run()
        {
            try
            {
                server = new TcpListener(this.localIP, this.port);

                server.Start(1000);
                Console.WriteLine("Server started running at {0}:{1}\n", this.localIP, this.port);

                while (true)
                {
                    try 
                    {
                        TcpClient client = server.AcceptTcpClient();

                        (new Thread(this.SatisfyClient)).Start((object)client);
                    }
                    catch (SocketException se) { Console.WriteLine("SocketException: {0}", se); }
                    catch (InvalidOperationException ioe) { Console.WriteLine("InvalidOperationException: {0}", ioe); }
                    catch (OutOfMemoryException oome) { Console.WriteLine("OutOfMemoryException: {0}", oome); }
                    catch (ThreadStateException tse) { Console.WriteLine("ThreadStateException: {0}", tse); }
                    catch (ArgumentNullException ane) { Console.WriteLine("ArgmunetNullException: {0}", ane); }
                }
            }
            catch (InvalidOperationException ioe) { Console.WriteLine("InvalidOperationException: {0}", ioe); }
            catch (ArgumentOutOfRangeException aoore) { Console.WriteLine("ArgumentOutOfRangeException: {0}", aoore); }
            catch (SocketException se) { Console.WriteLine("SocketException: {0}", se); }
            catch (ArgumentNullException ane) { Console.WriteLine("ArgmunetNullException: {0}", ane); }
            finally 
            {
                try { server.Stop(); }
                catch (SocketException se) { Console.WriteLine("SocketException: {0}", se); }
            }
        }

        private void NotifyFriend(object rel)
        {
            byte[] bytes;
            Tuple<XElement, XDocument, int, object> relation = (Tuple<XElement, XDocument, int, object>)rel;
            Int32 port = Int32.Parse(relation.Second.Root.Element("last_address_used").Attribute("notif_port").Value);
            String host = relation.Second.Root.Element("last_address_used").Attribute("ip_address").Value;
            String statusMessage = relation.First.Element("status").Value;
            String username = relation.First.Attribute("username").Value;
            TcpClient notifClient = new TcpClient(host, port);
            NetworkStream stream = notifClient.GetStream();

            switch (relation.Third)
            {
                case 0:
                    {
                        //log in
                        stream.WriteByte(1);

                        bytes = Encoding.ASCII.GetBytes(username);

                        stream.WriteByte((byte)bytes.Length); //3-15 caractere => 6-30 in raza 0..255 a unui byte
                        stream.Write(bytes, 0, bytes.Length);

                        bytes = Encoding.ASCII.GetBytes(host); //maxim 12 caractere => maxim 24

                        stream.WriteByte((byte)bytes.Length);
                        stream.Write(bytes, 0, bytes.Length);

                        bytes = BitConverter.GetBytes(port);

                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes, 0, 4);

                        stream.Write(bytes, 0, 4); //sfarsit  protocol cu 4 bytes ce reprezita portul
                    }
                    break;
                case 1: //prietenul apare deja online, dar si-a schimbat mesajul de la status
                    {
                        stream.WriteByte(2);

                        //nu știm cat de mare e un astfel de mesaj, numarul de bytes poate depasi 255, am putea folosi
                        //shor ce e pe 2 bytes, dar e ok și cu int
                        bytes = BitConverter.GetBytes(statusMessage.Length);

                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes, 0, 4);

                        stream.Write(bytes, 0, 4);

                        bytes = Encoding.ASCII.GetBytes(statusMessage);

                        stream.Write(bytes, 0, bytes.Length);
                    }

                    break;
                case 2: //log out
                    {
                        //e necesar numai sa fie trimis numele utilizatorului ce se delogheaza
                        //datorita unicitatii acestuia
                        stream.WriteByte(3);

                        bytes = Encoding.ASCII.GetBytes(username);

                        stream.WriteByte((byte)bytes.Length);
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    break;
                case 3: //friend request
                    {
                        stream.WriteByte(4);

                        bytes = Encoding.ASCII.GetBytes(username);

                        stream.WriteByte((byte)bytes.Length);
                        stream.Write(bytes, 0, bytes.Length);

                        relation.Fourth = stream.ReadByte() == 1 ? true : false;
                    }

                    break;
            }

            stream.Close();
            notifClient.Close();
        }

        private void SatisfyClient(object cl)
        {
            int notificationsPort;
            int command = 0;
            int responseLength = 0;
            int usernameLength;
            int passwordLength;
            bool ok = true;
            byte response = 0;
            byte[] bytes = new byte[256];
            string username;
            string password;
            string responseMessage = "";
            IEnumerable<string> friendIDs;
            IPEndPoint remoteEndPoint = null;
            TcpClient client = (TcpClient)cl;
            NetworkStream stream = client.GetStream();
            FileStream fs = null;
            XDocument users = null;
            XDocument details = null;
            XDocument friends = null;
            XDocument friend = null;
            XElement currentUser = null;
            
            Console.WriteLine("Accepted client from: {0}", client.Client.RemoteEndPoint);

            do
            {
                if ((command = stream.ReadByte()) == -1)
                    Thread.Sleep(10);

                rwl.AcquireWriterLock(1000);
                Console.WriteLine("Command: {0}", command);

                try
                {
                    switch (command)
                    {
                        case 1: //Sign in
                            {
                                XElement listItem = null;
                                XElement user;
                                
                                friends = new XDocument();
                                usernameLength = stream.ReadByte();

                                stream.Read(bytes, 0, usernameLength);

                                username = Encoding.ASCII.GetString(bytes, 0, usernameLength);
                                passwordLength = stream.ReadByte();

                                stream.Read(bytes, 0, passwordLength);

                                password = Encoding.ASCII.GetString(bytes, 0, passwordLength);

                                stream.Read(bytes, 0, 4);

                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(bytes, 0, 4);

                                notificationsPort = BitConverter.ToInt32(bytes, 0);

                                fs = new FileStream(@"files\users.xml", FileMode.Open);
                                users = XDocument.Load(fs);

                                fs.Close();
                                fs.Dispose();

                                try
                                {
                                    currentUser =
                                    (
                                        from usr in users.Root.Elements()
                                        where usr.Attribute("username").Value == username
                                        select usr
                                    )
                                    .First();
                                }
                                catch
                                {
                                    response = 1;

                                    break;
                                }

                                if (currentUser.Element("status").Attribute("state").Value == "online")
                                {
                                    response = 2;

                                    break;
                                }
                                if (currentUser.Attribute("password").Value != password)
                                {
                                    response = 3;

                                    break;
                                }

                                remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                                fs = new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Open);
                                details = XDocument.Load(fs);

                                fs.Close();
                                fs.Dispose();
                                friends.Add(new XElement("friends"));
                                currentUser.Element("status").Attribute("state").Value = "online";
                                currentUser.Element("status").Value = null;
                                details.Root.Element("last_address_used").SetAttributeValue("ip_address", remoteEndPoint.Address);
                                details.Root.Element("last_address_used").SetAttributeValue("port", remoteEndPoint.Port);
                                details.Root.Element("last_address_used").SetAttributeValue("notif_port", notificationsPort);

                                fs = new FileStream(@"files\users.xml", FileMode.Truncate);

                                users.Save(fs);
                                fs.Close();
                                fs.Dispose();

                                fs = new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Truncate);
                                
                                details.Save(fs);
                                fs.Close();
                                fs.Dispose();

                                friendIDs =
                                    from frnd in details.Root.Element("friends").Elements()
                                    select frnd.Attribute("id").Value;

                                foreach (string id in friendIDs)
                                {
                                    user = users.Root.Elements().Single(usr => usr.Attribute("id").Value == id);
                                    listItem = new XElement("friend", new XElement("status"));

                                    listItem.SetAttributeValue("username", user.Attribute("username").Value);

                                    if (user.Element("status").Attribute("state").Value == "offline")
                                    {
                                        listItem.Element("status").SetAttributeValue("state", "offline");
                                        friends.Root.Add(listItem);

                                        continue;
                                    }

                                    listItem.Element("status").SetAttributeValue("state", "online");
                                    
                                    listItem.Element("status").Value = user.Element("state").Value;
                                    fs = new FileStream(@"files\details\" + id + ".xml", FileMode.Open);
                                    friend = XDocument.Load(fs);

                                    fs.Close();
                                    fs.Dispose();
                                    (new Thread(this.NotifyFriend)).Start(new Tuple<XElement, XDocument, int, object>(currentUser, friend, 0));
                                    listItem.Add(new XElement("last_address_used"));
                                    listItem.Element("last_address_used").SetAttributeValue("ip_address", friend.Root.Element("last_address_used").Attribute("ip_address").Value);
                                    listItem.Element("last_address_used").SetAttributeValue("port", friend.Root.Element("last_address_used").Attribute("port").Value);
                                    friends.Root.Add(listItem);

                                    friend = null;
                                    listItem = null;
                                    user = null;
                                }

                                responseMessage = friends.ToString(SaveOptions.DisableFormatting);
                                responseLength = responseMessage.Length;
                                friends = null;
                                friendIDs = null;
                            }

                            break;
                        case 2: //Sign out
                            {
                                ok = false;
                                currentUser.Element("status").Attribute("state").Value = "offline";
                                currentUser.Element("status").Value = null;
                                fs = new FileStream(@"files\users.xml", FileMode.Truncate);

                                stream.Close();
                                users.Save(fs);
                                fs.Close();
                                fs.Dispose();

                                fs = new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Open);
                                friend = XDocument.Load(fs);

                                fs.Close();
                                fs.Dispose();
                                
                                friendIDs = 
                                    from frnd in friend.Root.Element("friends").Elements()
                                    select frnd.Attribute("id").Value;

                                //notify
                                foreach (string id in friendIDs)
                                {
                                    fs = new FileStream(@"files\details\" + id + ".xml", FileMode.Open);
                                    friend = XDocument.Load(fs);

                                    fs.Close();
                                    fs.Dispose();
                                    (new Thread(this.NotifyFriend)).Start(new Tuple<XElement, XDocument, int, object>(currentUser, friend, 2));

                                    friend = null;
                                }

                                friendIDs = null;
                            }

                            break;
                        case 3: //Sign up
                            {
                                bool usernameExists;
                                int lastUserID = -1;
                                XElement newUser;

                                response = 1;
                                usernameLength = stream.ReadByte();

                                stream.Read(bytes, 0, usernameLength);

                                username = Encoding.ASCII.GetString(bytes, 0, usernameLength);
                                response = 2;
                                passwordLength = stream.ReadByte();

                                stream.Read(bytes, 0, passwordLength);

                                response = 3;
                                password = Encoding.ASCII.GetString(bytes, 0, passwordLength);
                                fs = new FileStream(@"files\users.xml", FileMode.Open);
                                users = XDocument.Load(fs);
                                usernameExists =
                                    (
                                        from user in users.Root.Elements()
                                        where user.Attribute("username").Value == username
                                        select user.Attribute("username").Value
                                    )
                                    .Any(user => user == username);

                                fs.Close();
                                fs.Dispose();

                                if (usernameExists)
                                    break;
                                else
                                    response = 0;

                                if (users.Root.Elements().Count() > 0)
                                    lastUserID = int.Parse(users.Root.Elements().Last().Attribute("id").Value);

                                newUser = new XElement("user");

                                newUser.SetAttributeValue("id", (lastUserID + 1).ToString());
                                newUser.SetAttributeValue("username", username);
                                newUser.SetAttributeValue("password", password);
                                newUser.Add(new XElement("status"));
                                newUser.Element("status").SetAttributeValue("state", "online");
                                users.Root.Add(newUser);

                                fs = new FileStream(@"files\users.xml", FileMode.Truncate);

                                users.Save(fs);
                                fs.Close();
                                fs.Dispose();

                                if (!Directory.Exists(@"files\details"))
                                    Directory.CreateDirectory(@"files\details");

                                fs = new FileStream(@"files\details\" + (lastUserID + 1).ToString() + ".xml", FileMode.Create);
                                details = new XDocument();
                                remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

                                details.Add(new XElement("details"));
                                details.Root.Add(new XElement("last_address_used"));
                                details.Root.Add(new XElement("friends"));
                                details.Root.Add(new XElement("offline_messages"));
                                details.Root.Element("last_address_used").SetAttributeValue("ip_address", remoteEndPoint.Address);
                                details.Root.Element("last_address_used").SetAttributeValue("port", remoteEndPoint.Port);
                                details.Save(fs);
                                fs.Close();
                                fs.Dispose();

                                Console.WriteLine("Recieved username: {0}", username);
                                Console.WriteLine("Recieved password: {0}", password);
                            }

                            break;
                        case 4: //Friend request
                            {
                                bool accepted;
                                string friendID;
                                Tuple<XElement, XDocument, int, object> tuple;
                                XElement fr;

                                usernameLength = stream.ReadByte();

                                stream.Read(bytes, 0, usernameLength);

                                username = Encoding.ASCII.GetString(bytes);
                                fs = new FileStream(@"files\users.xml", FileMode.Open);
                                users = XDocument.Load(fs);

                                fs.Close();
                                fs.Dispose();

                                try
                                {
                                    friendID =
                                        (
                                            from usr in users.Root.Elements()
                                            where usr.Attribute("username").Value == username && usr.Element("status").Attribute("state").Value == "online"
                                            select usr.Attribute("id").Value
                                        )
                                        .First();
                                }
                                catch 
                                {
                                    response = 1;

                                    break;
                                }

                                fs = new FileStream(@"files\details\" + friendID + ".xml", FileMode.Open);
                                friend = XDocument.Load(fs);
                                tuple = new Tuple<XElement, XDocument, int, object>(currentUser, friend, 3);

                                fs.Close();
                                fs.Dispose();
                                this.NotifyFriend(tuple);

                                accepted = (bool)tuple.Fourth;

                                if (accepted)
                                {
                                    response = 0;
                                    fs = new FileStream(@"files\details\" + friendID + ".xml", FileMode.Truncate);
                                    fr = new XElement("friend");

                                    fr.SetAttributeValue("id", currentUser.Attribute("id").Value);
                                    friend.Root.Element("friends").Add(fr);
                                    friend.Save(fs);
                                    fs.Close();
                                    fs.Dispose();

                                    fs = new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Truncate);
                                    fr = new XElement("friend");

                                    fr.SetAttributeValue("id", friendID);
                                    details.Root.Element("friends").Add(fr);
                                    details.Save(fs);
                                    fs.Close();
                                    fs.Dispose();
                                }
                                else
                                    response = 2;
                            }

                            break;
                        case 5: //Change status message => notifyfriend cu 1
                            {
                                int messageLength;
                                string message;

                                stream.Read(bytes, 0, 4);

                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(bytes, 0, 4);

                                messageLength = BitConverter.ToInt32(bytes, 0);

                                stream.Read(bytes, 0, messageLength);

                                message = Encoding.ASCII.GetString(bytes, 0, messageLength);
                                currentUser.Element("status").Value = message;
                                fs = new FileStream(@"files\users.xml", FileMode.Truncate);

                                users.Save(fs);
                                fs.Close();
                                fs.Dispose();

                                friendIDs =
                                    from frnd in details.Root.Element("friends").Elements()
                                    select frnd.Attribute("id").Value;

                                //notify
                                foreach (string id in friendIDs)
                                {
                                    fs = new FileStream(@"files\details\" + id + ".xml", FileMode.Open);
                                    friend = XDocument.Load(fs);

                                    fs.Close();
                                    fs.Dispose();
                                    (new Thread(this.NotifyFriend)).Start(new Tuple<XElement, XDocument, int, object>(currentUser, friend, 1));

                                    friend = null;
                                }

                                friendIDs = null;
                            }

                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e) { Console.WriteLine("Exception: {0}", e); }
                finally
                {
                    Console.WriteLine();

                    rwl.ReleaseLock();
                    if (command > 0 && command != 2)
                        stream.WriteByte(response);
                    else if (command == 1 && response == 0)
                    {
                        bytes = BitConverter.GetBytes(responseLength);

                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes);

                        stream.Write(bytes, 0, bytes.Length);

                        bytes = Encoding.ASCII.GetBytes(responseMessage);

                        stream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            while (ok);

            client.Close();
        }
    }
}
