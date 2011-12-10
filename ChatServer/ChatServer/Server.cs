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

        private class MyTuple<Fi, S, T, Fo>
        {
            public MyTuple() { }

            public MyTuple(Fi first, S second, T third)
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
            this.port = 8001;

            if (!Directory.Exists("files"))
                Directory.CreateDirectory("files");
            if (!File.Exists(@"files\users.xml"))
            {
                XDocument users = new XDocument();
                FileStream fs = new FileStream(@"files\users.xml", FileMode.Create);

                users.Add(new XElement("users"));
                users.Save(fs);
                fs.Close();
                fs.Dispose();
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
                    catch (Exception e) { Console.WriteLine("Exception: {0}", e); }
                }
            }
            catch (Exception e) { Console.WriteLine("Exception: {0}", e); }
            finally 
            {
                try { server.Stop(); }
                catch (Exception e) { Console.WriteLine("Exception: {0}", e); }
            }
        }

        private void NotifyFriend(object rel)
        {
            byte[] bytes;
            MyTuple<XElement, XDocument, int, object> relation = (MyTuple<XElement, XDocument, int, object>)rel;
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
                        this.Write(stream, (byte)1);
                        this.Write(stream, username);
                        this.Write(stream, host);
                        this.Write(stream, port);
                    }

                    break;
                case 1: //prietenul apare deja online, dar si-a schimbat mesajul de la status
                    {
                        this.Write(stream, (byte)2);
                        this.Write(stream, statusMessage, false);
                    }

                    break;
                case 2: //log out
                    {
                        //e necesar numai sa fie trimis numele utilizatorului ce se delogheaza
                        //datorita unicitatii acestuia
                        this.Write(stream, (byte)3);
                        this.Write(stream, username);
                    }

                    break;
                case 3: //friend request
                    {
                        this.Write(stream, (byte)4);
                        this.Write(stream, username);

                        relation.Fourth = 1 == (int)this.Read(stream, typeof(byte)) ? true : false;
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
            bool ok = true;
            byte response = 0;
            byte[] bytes = new byte[256];
            string username = "";
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
                while (-1 == (command = (int)this.Read(stream, typeof(byte))))
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
                                username = (string)this.Read(stream, typeof(string));
                                password = (string)this.Read(stream, typeof(string));
                                notificationsPort = (int)this.Read(stream, typeof(int));
                                users = this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                try
                                {
                                    //se incearca sa se extraga utilizatorul ce se logheaza din lista de utilizatori
                                    //criteriul e nick-ul utilzatorului datorita unicitatii lui
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
                                    //utilizatorul ce incearca sa se autentifice nu exista in baza de date
                                    response = 1;

                                    break;
                                }

                                if (currentUser.Element("status").Attribute("state").Value == "online") //se verifica de utilizatorul e deja online
                                {
                                    response = 2;

                                    break;
                                }
                                if (currentUser.Attribute("password").Value != password) //se verifica de parola introdusa e aceeasi cu cea din baza de date
                                {
                                    response = 3;

                                    break;
                                }

                                remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                                details = this.Read(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                friends.Add(new XElement("friends"));
                                currentUser.Element("status").Attribute("state").Value = "online";
                                currentUser.Element("status").Value = null;
                                this.Write(new FileStream(@"files\users.xml", FileMode.Truncate), users);
                                details.Root.Element("last_address_used").SetAttributeValue("ip_address", remoteEndPoint.Address);
                                details.Root.Element("last_address_used").SetAttributeValue("port", remoteEndPoint.Port);
                                details.Root.Element("last_address_used").SetAttributeValue("notif_port", notificationsPort);
                                this.Write(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Truncate), details);

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
                                    friend = this.Read(new FileStream(@"files\details\" + id + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;
                                    
                                    (new Thread(this.NotifyFriend)).Start(new MyTuple<XElement, XDocument, int, object>(currentUser, friend, 0));
                                    
                                    listItem.Add(new XElement("last_address_used"));
                                    listItem.Element("last_address_used").SetAttributeValue("ip_address", friend.Root.Element("last_address_used").Attribute("ip_address").Value);
                                    listItem.Element("last_address_used").SetAttributeValue("port", friend.Root.Element("last_address_used").Attribute("port").Value);
                                    friends.Root.Add(listItem);

                                    friend = null;
                                    listItem = null;
                                    user = null;
                                }

                                responseMessage = friends.ToString(SaveOptions.DisableFormatting);
                                friends = null;
                                friendIDs = null;
                            }

                            break;
                        case 2: //Sign out
                            {
                                XDocument user;

                                ok = false;
                                users = (XDocument)this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument));
                                currentUser = null;

                                currentUser =
                                    (
                                        from usr in users.Root.Elements()
                                        where usr.Attribute("username").Value == username
                                        select usr
                                    )
                                    .First();

                                currentUser.Element("status").Attribute("state").Value = "offline";
                                currentUser.Element("status").Value = null;
                                this.Write(new FileStream(@"files\users.xml", FileMode.Truncate), users);

                                user = this.Read(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                friendIDs = 
                                    from frnd in user.Root.Element("friends").Elements()
                                    select frnd.Attribute("id").Value;

                                user = null;

                                //notify
                                foreach (string id in friendIDs)
                                {
                                    friend = this.Read(new FileStream(@"files\details\" + id + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                    (new Thread(this.NotifyFriend)).Start(new MyTuple<XElement, XDocument, int, object>(currentUser, friend, 2));

                                    friend = null;
                                }

                                friendIDs = null;

                                stream.Close();
                            }

                            break;
                        case 3: //Sign up
                            {
                                bool usernameExists;
                                int lastUserID = -1;
                                XElement newUser;

                                response = 1;
                                username = (string)this.Read(stream, typeof(string)); 
                                response = 2;
                                password = (string)this.Read(stream, typeof(string));
                                response = 3;
                                users = this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument)) as XDocument;
                                
                                usernameExists =
                                    (
                                        from user in users.Root.Elements()
                                        where user.Attribute("username").Value == username
                                        select user.Attribute("username").Value
                                    )
                                    .Any(user => user == username);

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
                                this.Write(new FileStream(@"files\users.xml", FileMode.Truncate), users);

                                if (!Directory.Exists(@"files\details"))
                                    Directory.CreateDirectory(@"files\details");

                                details = this.Read(new FileStream(@"files\details\" + (lastUserID + 1).ToString() + ".xml", FileMode.Create), typeof(XDocument)) as XDocument;
                                remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

                                details.Add(new XElement("details"));
                                details.Root.Add(new XElement("last_address_used"));
                                details.Root.Add(new XElement("friends"));
                                details.Root.Add(new XElement("offline_messages"));
                                details.Root.Element("last_address_used").SetAttributeValue("ip_address", remoteEndPoint.Address);
                                details.Root.Element("last_address_used").SetAttributeValue("port", remoteEndPoint.Port);
                                this.Write(new FileStream(@"files\details\" + (lastUserID + 1).ToString() + ".xml", FileMode.Truncate), details);
                                
                                Console.WriteLine("Recieved username: {0}", username);
                                Console.WriteLine("Recieved password: {0}", password);
                            }

                            break;
                        case 4: //Friend request
                            {
                                bool accepted;
                                string friendID;
                                MyTuple<XElement, XDocument, int, object> tuple;
                                XElement fr;

                                username = (string)this.Read(stream, typeof(string));
                                users = this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument)) as XDocument;

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

                                friend = this.Read(new FileStream(@"files\details\" + friendID + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;
                                tuple = new MyTuple<XElement, XDocument, int, object>(currentUser, friend, 3);

                                this.NotifyFriend(tuple);

                                accepted = (bool)tuple.Fourth;

                                if (accepted)
                                {
                                    response = 0;
                                    fr = new XElement("friend");

                                    fr.SetAttributeValue("id", currentUser.Attribute("id").Value);
                                    friend.Root.Element("friends").Add(fr);
                                    this.Write(new FileStream(@"files\details\" + friendID + ".xml", FileMode.Truncate), friend);
                                    
                                    fr = new XElement("friend");

                                    fr.SetAttributeValue("id", friendID);
                                    details.Root.Element("friends").Add(fr);
                                    this.Write(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Truncate), details);    
                                }
                                else
                                    response = 2;
                            }

                            break;
                        case 5: //Change status message => notifyfriend cu 1
                            {
                                string message = (string)this.Read(stream, typeof(string), false);

                                currentUser.Element("status").Value = message;
                                this.Write(new FileStream(@"files\users.xml", FileMode.Truncate), users);
                                
                                friendIDs =
                                    from frnd in details.Root.Element("friends").Elements()
                                    select frnd.Attribute("id").Value;

                                //notify
                                foreach (string id in friendIDs)
                                {
                                    friend = this.Read(new FileStream(@"files\details\" + id + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                    (new Thread(this.NotifyFriend)).Start(new MyTuple<XElement, XDocument, int, object>(currentUser, friend, 1));

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
                        this.Write(stream, (byte)response);
                    if (command == 1 && response == 0)
                        this.Write(stream, responseMessage, false);
                }
            }
            while (ok);

            client.Close();
        }

        private object Read(Stream stream, Type type, bool isLenByte = true)
        {
            switch (type.ToString())
            {
                case "System.Byte":
                    return stream.ReadByte();
                case "System.Int32":
                    {
                        byte[] bytes = new byte[4];

                        stream.Read(bytes, 0, 4);

                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes);

                        return BitConverter.ToInt32(bytes, 0);
                    }
                case "System.String":
                    {
                        int len;
                        byte[] bytes;

                        len = (int)(isLenByte ? this.Read(stream, typeof(byte)) : this.Read(stream, typeof(int)));
                        bytes = new byte[len];

                        stream.Read(bytes, 0, len);

                        return Encoding.UTF8.GetString(bytes);
                    }
                case "System.Xml.Linq.XDocument":
                    {
                        XDocument document = XDocument.Load(stream);

                        stream.Close();
                        stream.Dispose();

                        return document;
                    }   
                default:
                    return false;
            }
        }

        private void Write(Stream stream, object obj, bool isLenByte = true)
        {
            switch (obj.GetType().ToString())
            {
                case "System.Byte":
                    {
                        byte _byte = (byte)obj;

                        stream.WriteByte(_byte);
                    }

                    break;
                case "System.Int32":
                    {
                        int _int = (int)obj;
                        byte[] bytes = new byte[4];

                        bytes = BitConverter.GetBytes(_int);
                        
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes);

                        stream.Write(bytes, 0, 4);
                    }

                    break;
                case "System.String":
                    {
                        string _string = (string)obj;
                        byte[] bytes = new byte[Encoding.UTF8.GetByteCount(_string)];

                        if (isLenByte) { this.Write(stream, (byte)_string.Length); }
                        else { this.Write(stream, _string.Length); }

                        bytes = Encoding.UTF8.GetBytes(_string);

                        stream.Write(bytes, 0, bytes.Length);
                    }

                    break;
                case "System.Xml.Linq.XDocument":
                    {
                        XDocument document = obj as XDocument;

                        document.Save(stream);
                        stream.Close();
                        stream.Dispose();
                    }

                    break;
                default:
                    return;
            }
        }
    }
}
