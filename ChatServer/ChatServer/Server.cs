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
        private IPAddress ipAddress;
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

            public MyTuple(Fi first, S second, T third, Fo Fourth)
            {
                this.First = first;
                this.Second = second;
                this.Third = third;
                this.Fourth = Fourth;
            }
            public Fi First { get; set; }
            public S Second { get; set; }
            public T Third { get; set; }
            public Fo Fourth { get; set; }
        }

        public Server()
        {
            XDocument config = this.Read(new FileStream(@"files\config.xml", FileMode.Open), typeof(XDocument)) as XDocument;

            this.ipAddress = IPAddress.Parse(config.Root.Element("ip_address").Value);
            this.port = Int32.Parse(config.Root.Element("port").Value);

            config = null;

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
            else
            {
                //Reseting all users to offline (could be Error in last execution)
                XDocument users = (XDocument)this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument));
                foreach (var user in users.Root.Elements())
                {
                    user.Element("status").Attribute("state").Value = "offline";
                }
                this.Write(new FileStream(@"files\users.xml", FileMode.Truncate), users);
            }
        }

        public void Run()
        {
            try
            {
                server = new TcpListener(this.ipAddress, this.port);

                server.Start(1000);
                Console.WriteLine("Server started running at {0}:{1}\n", this.ipAddress, this.port);

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
            //Dinu! am adaugat try->catch
            TcpClient notifClient = null;
            NetworkStream stream = null;
            
                MyTuple<XElement, XDocument, int, object> relation = (MyTuple<XElement, XDocument, int, object>)rel;
            try
            {
//                Console.WriteLine(relation.Second.Root.ToString() + relation.Second.Root.Elements().ToString());
                Int32 port = Int32.Parse(relation.Second.Root.Element("last_address_used").Attribute("notif_port").Value);
                String host = relation.Second.Root.Element("last_address_used").Attribute("ip_address").Value;
                String statusMessage = relation.First.Element("status").Value;
                String username = relation.First.Attribute("username").Value;
                notifClient = new TcpClient();
                Console.WriteLine("Trying to connect : {0}:{1}", host, port);

                notifClient.Connect(host, port);

                stream = notifClient.GetStream();

                Console.WriteLine("Trying to connect 2: {0}:{1}",host,port);

                switch (relation.Third)
                {
                    case 0:
                        {
                            //log in
                            this.Write(stream, (byte)1);
                            this.Write(stream, username);
                            this.Write(stream, host);
//                            this.Write(stream, port);
                            int myPort = (int)relation.Fourth;
                            this.Write(stream, myPort);
                            //TODO send host too
                        }

                        break;
                    case 1: //prietenul apare deja online, dar si-a schimbat mesajul de la status
                        {
                            this.Write(stream, (byte)2);
                            this.Write(stream, username); //Dinu!
                            this.Write(stream, statusMessage);//,false Dinu!
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
                            bool resp1;
//                            byte resp2;
                            //TODO remove resp3
                            string resp3 = "";

                            this.Write(stream, (byte)4);
                            this.Write(stream, username);

//                            Console.WriteLine("Sent byte + username + ");
//                            if (relation.Fourth as string == "")
//                                this.Write(stream, (byte)0);
//                            else
//                            {
//                                this.Write(stream, (byte)1);

//                            Tuple<string,string> tuple = relation.Fourth as Tuple<string, string>; //message, user xmlFile
                            this.Write(stream, relation.Fourth); //writing message
//                            }

//                            resp1 = 1 == (int)this.Read(stream, typeof(byte)) ? true : false;
//                            if(resp1)
//                                this.Write(stream, tuple.Item2, false); //writing friend xmlFile
//                            resp2 = (byte)this.Read(stream, typeof(byte));
//
//                            if (resp2 == 1)
//                                resp3 = (string)this.Read(stream, typeof(string));

//                            Console.WriteLine("Received response: " + resp1);
//                            relation.Fourth = new Tuple<bool, string>(resp1, resp3);
                        }

                        break;
                    case 4: //friend response - when the other is online
                        {
                            Tuple<byte, string, int, string> resp = relation.Fourth as Tuple<byte, string, int, string>;

                            Console.WriteLine("Trying to send data to friend"); //TODO remove

                            this.Write(stream, (byte)5);
                            this.Write(stream, username);
//                            this.Write(stream, resp.Item1); //a acceptat sau nu: 0 - da, 1 - nu

                            this.Write(stream, resp.Item4, false);
                            Console.WriteLine("Sent"); //TODO remove
//                            if (resp.Item4 != "") //friend message
//                            {
//                                this.Write(stream, (byte)1);
//                                this.Write(stream, resp.Item4);
//                            }
//                            else
//                                this.Write(stream, (byte)1);
//
//                            if (resp.Item1 == 0) //a acceptat si trimite hostul si portul de comunicare
//                            {
//                                this.Write(stream, resp.Item2);
//                                this.Write(stream, resp.Item3);
//                            }
                        }

                        break;
                    case 5:
                        {
                            this.Write(stream, (byte)6);
                            this.Write(stream, username);
                            this.Write(stream, relation.Fourth as string); //se trimite numele fisierului ce trebuie downloadat
                        }

                        break;
                    default:
                        break;
                }
            }
            catch { 
                Console.WriteLine("Notification Exception"); 
//                if (relation.Third == 3) //if notifyFriendRequest
//                    throw; //bubling exception
            } //No connection
            finally
            {
//                if(stream != null)
//                    stream.Close();
//                if(notifClient != null)
//                    notifClient.Close();
 
            }
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
            
            rwl.AcquireWriterLock(1000);
            Console.WriteLine("Accepted client from: {0}", client.Client.RemoteEndPoint);
            rwl.ReleaseLock();

            do
            {
                try
                {
//                    Console.WriteLine("Waiting");
//                    while (-1 == (command = (int)this.Read(stream, typeof(byte))))
//                    {
//                        Console.WriteLine("command: " + command);
//                        Thread.Sleep(10);
//                    }
                    command = (int) this.Read(stream, typeof (byte));

                    if (command < 0) command = 2; //LogOff
                        
                }
                catch
                {
                    Console.WriteLine("Command 2");
                    command = 2;
                }

                rwl.AcquireWriterLock(1000);
                Console.WriteLine("Command: {0}", command);

                response = 0; //reseting to default Dinu!

                try
                {
                    switch (command)
                    {
                        case 1: //Sign in
                            {
                                IEnumerable<XElement> requests;
                                XElement listItem = null;
                                XElement user;
                                
                                friends = new XDocument();
                                try
                                {
                                    username = (string)this.Read(stream, typeof(string));
                                    password = (string)this.Read(stream, typeof(string));
                                    notificationsPort = (int)this.Read(stream, typeof(int));
                                }
                                catch
                                {
                                    command = 2;

                                    break;
                                }
                                
                                users = this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                Console.WriteLine("User: "+ username);

                                try
                                {
                                    //se incearca sa se extraga utilizatorul ce se logheaza din lista de utilizatori
                                    //criteriul e nick-ul utilzatorului datorita unicitatii lui
//                                    currentUser = this.GetUSer(username); //Dinu!
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

//                                if (currentUser.Element("status").Attribute("state").Value == "online") //se verifica de utilizatorul e deja online
//                                {
//                                    response = 2;
//
//                                    break;
//                                }
                                //Dinu! Temporary commented, until will be solved
                                if (currentUser.Attribute("password").Value != password) //se verifica de parola introdusa e aceeasi cu cea din baza de date
                                {
                                    response = 3;

                                    break;
                                }

                                remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
                                details = this.Read(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                friends.Add(new XElement("root"));
                                friends.Root.Add(new XElement("myself"));
                                friends.Root.Add(new XElement("friends"));
                                friends.Root.Add(new XElement("friend_requests"));
                                friends.Root.Element("myself").Add(new XElement("status"));

                                friends.Root.Element("myself").Element("status").Value = currentUser.Element("status").Value;

                                if (details.Root.Element("img") != null)
                                {
                                    friends.Root.Element("myself").Add(new XElement("img"));
                                    friends.Root.Element("myself").Element("img").Value = details.Root.Element("img").Value;
                                }

                                currentUser.Element("status").Attribute("state").Value = "online";
                                //TODO inspect if state is saved

                                if (details.Root.Element("offline_messages").Elements().Count() > 0)
                                {
                                    friends.Root.Add(details.Root.Element("offline_messages"));
                                    details.Root.Element("offline_messages").Elements().Remove(); //TODO remove after response???
                                }
                                    
                                this.Write(new FileStream(@"files\users.xml", FileMode.Truncate), users);
                                details.Root.Element("last_address_used").SetAttributeValue("ip_address", remoteEndPoint.Address);
                                details.Root.Element("last_address_used").SetAttributeValue("port", remoteEndPoint.Port);
                                details.Root.Element("last_address_used").SetAttributeValue("notif_port", notificationsPort);
                                
                                friendIDs =
                                    from frnd in details.Root.Element("friends").Elements()
                                    select frnd.Attribute("id").Value;

                                foreach (string id in friendIDs)
                                {
                                    user = users.Root.Elements().Single(usr => usr.Attribute("id").Value == id);
                                    listItem = new XElement("friend");
                                    friend = this.Read(new FileStream(@"files\details\" + id + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                    listItem.Add(new XElement("status"));
                                    listItem.Add(new XElement("img"));
//                                    listItem.Add(new XElement("offline_messages"));
                                    listItem.Element("img").Value = friend.Root.Element("img").Value;
                                    listItem.SetAttributeValue("username", user.Attribute("username").Value);

//                                    if (user.Element("status").Attribute("state").Value == "offline")
//                                    {
//                                        listItem.Element("status").SetAttributeValue("state", "offline");
//                                        friends.Root.Add(listItem);
//
//                                        continue;
//                                    }
//
//                                    listItem.Element("status").SetAttributeValue("state", "online");
                                    
                                    listItem.Element("status").SetAttributeValue("state", user.Element("status").Attribute("state").Value);
                                    listItem.Element("status").Value = user.Element("status").Value;
                                    
                                    (new Thread(this.NotifyFriend)).Start(new MyTuple<XElement, XDocument, int, object>(currentUser, friend, 0, notificationsPort));
                                    
                                    //pe portul normal comunica cu serverul, pe notif_port o sa stabileasca un port de comunicare cu celalt client
                                    listItem.Add(new XElement("last_address_used"));
                                    listItem.Element("last_address_used").SetAttributeValue("ip_address", friend.Root.Element("last_address_used").Attribute("ip_address").Value);
                                    listItem.Element("last_address_used").SetAttributeValue("port", friend.Root.Element("last_address_used").Attribute("notif_port").Value);

                                    friends.Root.Element("friends").Add(listItem);

                                    friend = null;
                                    listItem = null;
                                    user = null;
                                }

                                requests = 
                                    from frnd in details.Root.Element("friend_requests").Elements()
                                    select frnd;

                                foreach (XElement request in requests)
                                {
                                    listItem = new XElement("request");

                                    try
                                    {
                                        if (request.Attribute("to").Value != null)
                                        {
                                            listItem.SetAttributeValue("username", request.Attribute("to").Value);
                                            listItem.SetAttributeValue("type", "to");
                                        }
                                    }
                                    catch 
                                    { 
                                        listItem.SetAttributeValue("username", request.Attribute("from").Value);
                                        listItem.SetAttributeValue("type", "from");
                                    }
                                    
                                    //TODO inspect
                                    if (request.Attribute("state") != null)
                                        listItem.SetAttributeValue("state", request.Attribute("state").Value);
                                    listItem.Value = request.Value;

                                    friends.Root.Element("friend_requests").Add(listItem);

                                    //TODO inspect
                                    if (request.Attribute("state") != null && Int32.Parse(request.Attribute("state").Value) > 1)
                                        request.Remove();
                                }

                                this.Write(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Truncate), details);

                                responseMessage = friends.ToString(SaveOptions.DisableFormatting);
                                friends = null;
                                friendIDs = null;
                                users = null;
                                currentUser = null;
                                details = null;
                            }

                            break;
                        case 2: //Sign out
                            {
                                ok = false;
                                users = (XDocument)this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument));
                                currentUser =
                                (
                                    from usr in users.Root.Elements()
                                    where usr.Attribute("username").Value == username
                                    select usr
                                )
                                .First();

//                                currentUser = this.GetUSer(username);
                                currentUser.Element("status").Attribute("state").Value = "offline";
                                
                                this.Write(new FileStream(@"files\users.xml", FileMode.Truncate), users);
                                this.NotifyFriends(currentUser, 2);
                                
                                stream.Close();
                            }

                            break;
                        case 3: //Sign up
                            {
                                bool usernameExists;
                                int lastUserID = -1;
                                XElement newUser;

                                try
                                {
                                    username = (string)this.Read(stream, typeof(string));
                                    password = (string)this.Read(stream, typeof(string));
                                }
                                catch
                                {
                                    command = 2;

                                    break;
                                }

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

                                //details = this.Read(new FileStream(@"files\details\" + (lastUserID + 1).ToString() + ".xml", FileMode.Create), typeof(XDocument)) as XDocument;
                                FileStream fileStream = null;
                                try
                                {
                                    fileStream = new FileStream(@"files\details\" + (lastUserID + 1).ToString() + ".xml", FileMode.Create);
                                    details = this.Read(fileStream, typeof(XDocument)) as XDocument;

                                    fileStream.Close();
                                }
                                catch { details = new XDocument(); }
                                finally
                                {
                                    if(fileStream != null)
                                        fileStream.Close();
                                }
                                //Dinu! prind exceptie cind nu exista fisier

                                remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

                                details.Add(new XElement("details"));
                                details.Root.Add(new XElement("last_address_used"));
                                details.Root.Add(new XElement("img"));
                                details.Root.Add(new XElement("friends"));
                                details.Root.Add(new XElement("offline_messages"));
                                details.Root.Add(new XElement("friend_requests"));
                                details.Root.Element("last_address_used").SetAttributeValue("ip_address", remoteEndPoint.Address);
                                details.Root.Element("last_address_used").SetAttributeValue("port", remoteEndPoint.Port);
                                this.Write(new FileStream(@"files\details\" + (lastUserID + 1).ToString() + ".xml", FileMode.Truncate), details);
                                
                                Console.WriteLine("Recieved username: {0}", username);
                                Console.WriteLine("Recieved password: {0}", password);

                                users = null;
                                details = null;
                                currentUser = null;
                                newUser = null;
                            }

                            break;
                        case 4: //Friend request
                            {
                                if (username == "")
                                {
                                    response = 1;

                                    break;
                                }

                                bool friendConnectionInterrrupted = false;
                                bool friendOnline;
                                bool accepted;
                                string friendID;
                                string friendUsername;
                                string friendMessage = "";
                                MyTuple<XElement, XDocument, int, object> tuple;
                                XElement fr;

                                try 
                                { 
                                    friendUsername = (string)this.Read(stream, typeof(string));

                                    Console.WriteLine("friend Username: " + friendUsername);
                                    //TODO inspect
//                                    byte z = 3;
//                                    if ((int)this.Read(stream, typeof(byte)) == 0)
//                                    {
//                                        Console.WriteLine("Received type 0");
                                        friendMessage = (string)this.Read(stream, typeof(string)); //să zicem că-i un mesaj de nu mai mult de 256 de caract
//                                    }

//                                    Console.WriteLine("Z: " + z);
                                    Console.WriteLine("Friend Message: " + friendMessage);
                                }
                                catch(Exception ex)
                                {
                                    Console.WriteLine("Ex: " + ex);
                                    command = 2;

                                    break;
                                }

                                users = this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                try
                                {
                                    friendID =
                                        (
                                            from usr in users.Root.Elements()
                                            where usr.Attribute("username").Value == friendUsername
                                            select usr.Attribute("id").Value
                                        )
                                        .First();
                                }
                                catch 
                                {
                                    response = 1;

                                    break;
                                }

                                currentUser = this.GetUSer(username);
                                details = this.Read(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                if (details.Root.Element("friends").Elements().Count(fID => fID.Attribute("id").Value == friendID) == 1)
                                {
                                    response = 2; //userul exista deja in lista de prieteni

                                    break;
                                }

                                friend = this.Read(new FileStream(@"files\details\" + friendID + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                if (friend.Root.Element("friend_requests").Elements().Count(req => req.Attribute("from") != null && req.Attribute("from").Value == username) == 1)
                                {
                                    response = 3; //exista deja o cerere

                                    break;
                                }

                                fr = new XElement("request");
                                fr.Value = friendMessage;
                                fr.SetAttributeValue("from", username);
                                fr.SetAttributeValue("id", currentUser.Attribute("id").Value);
                                friend.Root.Element("friend_requests").Add(fr);
                                this.Write(new FileStream(@"files\details\" + friendID + ".xml", FileMode.Truncate), friend);

                                fr = new XElement("request");
                                fr.SetAttributeValue("to", friendUsername);
                                fr.SetAttributeValue("id", friendID);
                                fr.SetAttributeValue("state", 1); //a intiat o cerere de adaugare in lista de prienteni
                                details.Root.Element("friend_requests").Add(fr);
                                this.Write(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Truncate), details);


                                friendOnline = users.Root.Elements().Single(usr => usr.Attribute("id").Value == friendID).Element("status").Attribute("state").Value == "online" ? true : false;

                                //daca e online se executa protocolul vechi, altfel cel nou
                                if (friendOnline)
                                {
                                    tuple = new MyTuple<XElement, XDocument, int, object>(currentUser, friend, 3);

//                                    //Convert me to xml
//                                    XElement element = new XElement("friend");
//                                    element.Add(new XElement("status"));
//                                    element.Add(new XElement("img"));
//                                    element.Element("img").Value = details.Root.Element("img").Value;
//                                    element.SetAttributeValue("username", currentUser.Attribute("username").Value);
//
//                                    element.Element("status").SetAttributeValue("state", currentUser.Element("status").Attribute("state").Value);
//                                    element.Element("status").Value = currentUser.Element("status").Value;
//
//                                    element.Add(new XElement("last_address_used"));
//                                    element.Element("last_address_used").SetAttributeValue("ip_address", details.Root.Element("last_address_used").Attribute("ip_address").Value);
//                                    element.Element("last_address_used").SetAttributeValue("port", details.Root.Element("last_address_used").Attribute("notif_port").Value);
//                                    Console.WriteLine("Sending : " + element.ToString(SaveOptions.DisableFormatting));
//                                    //End of conversion to xml


                                    tuple.Fourth = friendMessage;
//                                    tuple.Fourth = new Tuple<string, string> (friendMessage,element.ToString(SaveOptions.DisableFormatting));

                                    new Thread(this.NotifyFriend).Start(tuple);
//                                    this.NotifyFriend(tuple); 
//                                    catch(Exception ex)
//                                    {
//                                        Console.WriteLine("Exception frined notify: " + ex);
//                                        friendConnectionInterrrupted = true;
//                                    }

//                                    if (!friendConnectionInterrrupted)
//                                    {
//                                        Tuple<bool, string> resp = tuple.Fourth as Tuple<bool, string>;
//
//
//
//
//
//
//                                        accepted = resp.Item1;
//
//                                        if (accepted)
//                                        {
//                                            response = 0;
//                                            
//                                            fr = new XElement("friend");
//
//                                            fr.SetAttributeValue("id", currentUser.Attribute("id").Value);
//                                            friend.Root.Element("friends").Add(fr);
//                                            this.Write(new FileStream(@"files\details\" + friendID + ".xml", FileMode.Truncate), friend);
//
//                                            fr = new XElement("friend");
//
//                                            fr.SetAttributeValue("id", friendID);
//                                            details.Root.Element("friends").Add(fr);
//                                            this.Write(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Truncate), details);
//
//
//                                            
//                                            //Start Friend XML
//                                            XElement user = users.Root.Elements().Single(usr => usr.Attribute("id").Value == friendID);
////                                            XElement 
//                                            element = new XElement("friend");
//                                            friend = this.Read(new FileStream(@"files\details\" + friendID + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;
//
//                                            element.Add(new XElement("status"));
//                                            element.Add(new XElement("img"));
//                                            element.Element("img").Value = friend.Root.Element("img").Value;
//                                            element.SetAttributeValue("username", user.Attribute("username").Value);
//
//                                            element.Element("status").SetAttributeValue("state", user.Element("status").Attribute("state").Value);
//                                            element.Element("status").Value = user.Element("status").Value;
//
////                                            (new Thread(this.NotifyFriend)).Start(new MyTuple<XElement, XDocument, int, object>(currentUser, friend, 0, notificationsPort));
//
//                                            element.Add(new XElement("last_address_used"));
//                                            element.Element("last_address_used").SetAttributeValue("ip_address", friend.Root.Element("last_address_used").Attribute("ip_address").Value);
//                                            element.Element("last_address_used").SetAttributeValue("port", friend.Root.Element("last_address_used").Attribute("notif_port").Value);
//                                            Console.WriteLine("Sending : " + element.ToString(SaveOptions.DisableFormatting));
////                                            this.Write(stream, element.ToString(SaveOptions.DisableFormatting), false);
//                                            responseMessage = element.ToString(SaveOptions.DisableFormatting);
//                                            //End Friend XML
//                                        }
//                                        else
//                                            response = 2;
//
//                                        //TODO not needed
////                                        try
////                                        {
////                                            if (resp.Item2 != "")
////                                            {
////                                                this.Write(stream, (byte)1);
////                                                this.Write(stream, resp.Item2);
////                                            }
////                                            else
////                                                this.Write(stream, (byte)0);
////                                        }
////                                        catch
////                                        {
////                                            command = 2;
////
////                                            break;
////                                        }
//                                    }
                                }
//                                if (!friendOnline || friendConnectionInterrrupted)
//                                {
//                                    if (friend.Root.Element("friend_requests").Elements().Count(req => req.Attribute("from") != null && req.Attribute("from").Value == username) == 1)
//                                    {
//                                        response = 3; //exista deja o cerere
//
//                                        break;
//                                    }
//
//                                    fr = new XElement("request");
//                                    fr.Value = friendMessage;
//
//                                    fr.SetAttributeValue("from", username);
//                                    fr.SetAttributeValue("id", currentUser.Attribute("id").Value);
//                                    friend.Root.Element("friend_requests").Add(fr);
//                                    this.Write(new FileStream(@"files\details\" + friendID + ".xml", FileMode.Truncate), friend);
//
//                                    fr = new XElement("request");
//
//                                    fr.SetAttributeValue("to", friendUsername);
//                                    fr.SetAttributeValue("id", friendID);
//                                    fr.SetAttributeValue("state", 1); //a intiat o cerere de adaugare in lista de prienteni
//                                    details.Root.Element("friend_requests").Add(fr);
//                                    this.Write(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Truncate), details);
//                                }

                                friend = null;
                                fr = null;
                                users = null;
                                currentUser = null;
                                details = null;
                            }

                            break;
                        case 5: //Change status message => notifyfriend cu 1
                            {
                                if (username == "")
                                {
                                    response = 1;

                                    break;
                                }

                                string message;

                                try { message = (string)this.Read(stream, typeof(string)); }
                                catch
                                {
                                    command = 2;

                                    break;
                                }

                                Console.WriteLine("Status Message");
                                users = this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument)) as XDocument;

//                                currentUser = this.GetUSer(username); //Dinu!
                                currentUser =
                                (
                                    from usr in users.Root.Elements()
                                    where usr.Attribute("username").Value == username
                                    select usr
                                )
                                .First();

                                currentUser.Element("status").Value = message;

                                this.Write(new FileStream(@"files\users.xml", FileMode.Truncate), users);
                                this.NotifyFriends(currentUser, 1);
                            }

                            break;
                        case 6: //cand un user a fost offline a i-a fost adresata o invitatie de prietenie
                                //dupa ce s-a logat a raspuns server-ului de il adauga pe cel ce a initiat cererea ori nu
                            {
                                if (username == "")
                                {
                                    response = 1;

                                    break;
                                }

                                int resp;
                                string friendUsername;
                                string friendMessage = "";
                                XElement request;
                                MyTuple<XElement, XDocument, int, object> tuple;
                                XElement friendElement;
                                XElement fr;

                                try
                                {
                                    friendUsername = (string)this.Read(stream, typeof(string));
                                    resp = (int)this.Read(stream, typeof(byte));

//                                    if ((byte)this.Read(stream, typeof(byte)) == 1)
//                                        friendMessage = (string)this.Read(stream, typeof(string));
                                }
                                catch
                                {
                                    command = 2;
                                    break;
                                }

                                friendElement = this.GetUSer(friendUsername);
                                currentUser = this.GetUSer(username);

                                friend = this.Read(new FileStream(@"files\details\" + friendElement.Attribute("id").Value + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;
                                details = this.Read(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                request =
                                    (
                                        from frnd in details.Root.Element("friend_requests").Elements()
                                        where frnd.Attribute("from").Value == friendUsername
                                        select frnd
                                    )
                                    .First();
                                
                                request.Remove();

                                request =
                                    (
                                        from frnd in friend.Root.Element("friend_requests").Elements()
                                        where frnd.Attribute("to").Value == username
                                        select frnd
                                    )
                                    .First();
                                request.Remove();

                                if(resp == 0) //raspuns favorabil la adaugarea in lista de prieteni
                                {
                                    fr = new XElement("friend");

                                    fr.SetAttributeValue("id", friendElement.Attribute("id").Value);
                                    details.Root.Element("friends").Add(fr);

                                    fr = new XElement("friend");

                                    fr.SetAttributeValue("id", currentUser.Attribute("id").Value);
                                    friend.Root.Element("friends").Add(fr);

                                    XElement element = null;
                                    if (friendElement.Element("status").Attribute("state").Value == "online")
                                    {
                                        Console.WriteLine("User is online"); //TODO remove
                                        //Convert me to xml
                                        element = new XElement("friend");
                                        element.Add(new XElement("status"));
                                        element.Add(new XElement("img"));
                                        element.Element("img").Value = details.Root.Element("img").Value;
                                        element.SetAttributeValue("username", currentUser.Attribute("username").Value);

                                        element.Element("status").SetAttributeValue("state", currentUser.Element("status").Attribute("state").Value);
                                        element.Element("status").Value = currentUser.Element("status").Value;

                                        element.Add(new XElement("last_address_used"));
                                        element.Element("last_address_used").SetAttributeValue("ip_address", details.Root.Element("last_address_used").Attribute("ip_address").Value);
                                        element.Element("last_address_used").SetAttributeValue("port", details.Root.Element("last_address_used").Attribute("notif_port").Value);
                                        string meXml = element.ToString(SaveOptions.DisableFormatting);
                                        //End of conversion to xml

                                        fr = friend.Root.Element("last_address_used");
                                        tuple = new MyTuple<XElement, XDocument, int, object>(currentUser, friend, 4);

                                        tuple.Fourth = new Tuple<byte, string, int, string>((byte)resp, fr.Attribute("ip_address").Value, Int32.Parse(fr.Attribute("notif_port").Value), meXml);

                                        (new Thread(this.NotifyFriend)).Start(tuple);
//                                        this.NotifyFriend(tuple);
                                    }

                                    //TODO change to a function this xml string
                                    //Start Friend XML
                                    element = new XElement("friend");

                                    element.Add(new XElement("status"));
                                    element.Add(new XElement("img"));
                                    element.Element("img").Value = friend.Root.Element("img").Value;
                                    element.SetAttributeValue("username", friendElement.Attribute("username").Value);

                                    element.Element("status").SetAttributeValue("state", friendElement.Element("status").Attribute("state").Value);
                                    element.Element("status").Value = friendElement.Element("status").Value;

                                    element.Add(new XElement("last_address_used"));
                                    element.Element("last_address_used").SetAttributeValue("ip_address", friend.Root.Element("last_address_used").Attribute("ip_address").Value);
                                    element.Element("last_address_used").SetAttributeValue("port", friend.Root.Element("last_address_used").Attribute("notif_port").Value);
                                    responseMessage = element.ToString(SaveOptions.DisableFormatting);
                                    //End Friend XML
                                    //This string I will receive as repsonse
                                }


//                                if (sent)
//                                {
//                                    request.Remove();
//                                }
//                                else
//                                {
//                                    /*
//                                     * 2 - ok, cererea de adaugare in lista de prieteni a fost acceptata
//                                     * 3 - not ok, altfel
//                                     */
//                                    request.SetAttributeValue("state", 0 == resp ? 2 : 3);
//                                    request.Value = friendMessage;
//                                }

                                this.Write(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Truncate), details);
                                this.Write(new FileStream(@"files\details\" + friendElement.Attribute("id").Value + ".xml", FileMode.Truncate), friend);

                                details = null;
                                friend = null;
                                friendElement = null;
                                currentUser = null;
                                users = null;
                            }

                            break;

                        case 7: //change logo
                            {
                                if (username == "")
                                {
                                    response = 1;

                                    break;
                                }

                                byte[] logoContent;
                                int currUserID;
                                int lastRand;
                                int rand;

                                currentUser = this.GetUSer(username);
                                currUserID = Int32.Parse(currentUser.Attribute("id").Value);
                                details = this.Read(new FileStream(@"files\details\" + currUserID + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                try
                                {
                                    int length = (int)this.Read(stream, typeof (int));
                                    logoContent = new byte[length];
                                    int tmp = length;
                                    int k = 0;
                                    while (tmp > 0)
                                    {
                                        k = stream.Read(logoContent, length - tmp, tmp);
                                        tmp -= k;
                                    }
                                    
                                }
                                catch
                                {
                                    command = 2;

                                    break;
                                }
                                
//                                lastRand = details.Root.Element("img").Value != "" ? Int32.Parse(details.Root.Element("img").Attribute("last_rand").Value) : -1;

                                if (!Directory.Exists(@"files\images"))
                                    Directory.CreateDirectory(@"files\images");
//                                if (lastRand != -1)
//                                {
//                                    do
//                                    {
//                                        rand = this.random.Next(100000);
//                                    }
//                                    while (rand == lastRand);

//                                    File.Delete(@"files\images\" + currUserID + "_" + lastRand + ".bmp");
//                                }
//                                else
                                rand = this.random.Next(100000);

                                string filename = currUserID + "_" + rand + ".bmp";
                                if (details.Root.Element("img") == null)
                                    details.Root.Add(new XElement("img"));
                                details.Root.Element("img").Value = filename;

                                File.WriteAllBytes(@"files\images\" + filename, logoContent);
                                this.Write(new FileStream(@"files\details\" + currUserID + ".xml", FileMode.Truncate), details);

                                try { this.Write(stream, filename); }
                                catch
                                {
                                    command = 2;

                                    break;
                                }

                                this.NotifyFriends(currentUser, 5, filename);
                            }

                            break;
                        case 8:
                            {
                                if (username == "")
                                {
                                    response = 1;

                                    break;
                                }

                                string bmpName;
                                string bmpContent;

                                try { bmpName = (string)this.Read(stream, typeof(string)); }
                                catch
                                {
                                    command = 2;

                                    break;
                                }

                                try { bmpContent = File.ReadAllText(@"files\images\" + bmpName, Encoding.UTF8); }
                                catch
                                {
                                    response = 1;

                                    break;
                                }

                                try { this.Write(stream, bmpContent, false); }
                                catch
                                {
                                    command = 2;

                                    break;
                                }
                            }

                            break;
                        case 9: //adding offline messages
                            {
                                if (username == "")
                                {
                                    response = 1;

                                    break;
                                }


                                //avem username
                                string friendName = (string)this.Read(stream, typeof (string));
                                string offlineMessage = (string)this.Read(stream, typeof(string), false);

                                Console.WriteLine("Friend: " + friendName);
                                Console.WriteLine("OfflineMessage: " + offlineMessage );
                                //add to xml
                                XElement friendUser = this.GetUSer(friendName);

                                //TODO if friendUser is null ??

                                int friendId = Int32.Parse(friendUser.Attribute("id").Value);

                                Console.WriteLine("friend id: " + friendId);

                                details = this.Read(new FileStream(@"files\details\" + friendId + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;

                                XElement message = new XElement("message");
                                message.SetAttributeValue("from", username);
                                message.SetAttributeValue("date", DateTime.Now.ToUniversalTime());
                                message.Value = offlineMessage;

                                details.Root.Element("offline_messages").Add(message);

                                this.Write(new FileStream(@"files\details\" + friendId + ".xml", FileMode.Truncate), details);

                                response = 0;
//
//
//                                this.Write(stream,(byte)0);
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

                    try
                    {
                        if (command > 0 && command != 2)
                            this.Write(stream, (byte)response);
                        if (command == 1 && response == 0)
                            this.Write(stream, responseMessage, false);
                        if(command == 6 && response == 0)
                            this.Write(stream, responseMessage, false);
                    }
                    catch (Exception e) { Console.WriteLine("Exception: {0}", e); }
                }
            }
            while (ok);
//            Console.WriteLine("End ...");
            client.Close();
        }

        private XElement GetUSer(string username)
        {
            XElement user;
            XDocument users = this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument)) as XDocument;

            //TODO maybe required try catch 
            user =
                (
                    from usr in users.Root.Elements()
                    where usr.Attribute("username").Value == username
                    select usr
                )
                .First();

            return user;
        }

        private void NotifyFriends(XElement currentUser, int command, object obj = null)
        {
            IEnumerable<string> friendIDs;
            MyTuple<XElement, XDocument, int, object> tuple;
            XDocument details = this.Read(new FileStream(@"files\details\" + currentUser.Attribute("id").Value + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;
            XDocument users = this.Read(new FileStream(@"files\users.xml", FileMode.Open), typeof(XDocument)) as XDocument; ;
            XDocument friend;

            friendIDs =
                from frnd in details.Root.Element("friends").Elements()
                join usr in users.Root.Elements()
                on new { frnd.Attribute("id").Value } equals new { usr.Attribute("id").Value }
                where usr.Element("status").Attribute("state").Value == "online"
                select frnd.Attribute("id").Value;

            //notify
            foreach (string id in friendIDs)
            {
                friend = this.Read(new FileStream(@"files\details\" + id + ".xml", FileMode.Open), typeof(XDocument)) as XDocument;
                tuple = new MyTuple<XElement, XDocument, int, object>(currentUser, friend, command);
                tuple.Fourth = obj;

                try
                {
                    this.NotifyFriend(tuple);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error notify from NotifyFriends: " );
                }
                
                //TODO rethink with parameters + catching errors
//                (new Thread(this.NotifyFriend)).Start();

                friend = null;
            }
        }

        private object Read(Stream stream, Type type, bool isLenByte = true)
        {
//            Console.WriteLine("Type: " + type + " : " + (type.ToString() == "System.Byte"));
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
                case "System.Int64":
                    {
                        byte[] bytes = new byte[8];

                        stream.Read(bytes, 0, 8);

                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes);

                        return BitConverter.ToInt64(bytes, 0);
                    }
                case "System.String":
                    {
                        int len;
                        byte[] bytes;

                        len = (int)(isLenByte ? this.Read(stream, typeof(byte)) : this.Read(stream, typeof(int)));
                        if (len == 0)
                            return "";
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
                case "System.Int64":
                    {
                        long _long = (long)obj;
                        byte[] bytes = new byte[8];

                        bytes = BitConverter.GetBytes(_long);

                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(bytes);

                        stream.Write(bytes, 0, 8);
                    }

                    break;
                case "System.String":
                    {
                        string _string = (string)obj;
                        byte[] bytes = new byte[Encoding.UTF8.GetByteCount(_string)];
//                        byte[] bytes = Encoding.UTF8.GetBytes(_string);

                        if (isLenByte) { this.Write(stream, (byte)bytes.Length); }
                        else { this.Write(stream, bytes.Length); }

                        bytes = Encoding.UTF8.GetBytes(_string);
                        if(bytes.Length > 0)
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
