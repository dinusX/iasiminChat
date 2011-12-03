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

        private void SatisfyClient(object cl)
        {
            int command;
            int responseLength = 0;
            int usernameLength;
            int passwordLength;
            byte response = 0;
            byte[] bytes = new byte[256];
            string username;
            string password;
            string responseMessage = "";
            TcpClient client = (TcpClient)cl;
            NetworkStream stream = client.GetStream();
            FileStream fs;
            XDocument users;
            XDocument details;
            
            Console.WriteLine("Accepted client from: {0}", client.Client.RemoteEndPoint);
            
            command = stream.ReadByte();

            rwl.AcquireWriterLock(1000);
            Console.WriteLine("Command: {0}", bytes[0]);
            
            try
            {
                switch (command)
                {
                    case 1: //Sign in
                        {
                            IEnumerable<string> friendIDs;
                            XElement user = null;
                            XElement listItem = null;
                            XDocument friend = null;
                            XDocument list = new XDocument();

                            usernameLength = stream.ReadByte();

                            stream.Read(bytes, 0, usernameLength);

                            username = Encoding.ASCII.GetString(bytes, 0, usernameLength);
                            passwordLength = stream.ReadByte();

                            stream.Read(bytes, 0, passwordLength);

                            password = Encoding.ASCII.GetString(bytes, 0, passwordLength);
                            fs = new FileStream(@"files\users.xml", FileMode.Open);
                            users = XDocument.Load(fs);
                            
                            fs.Close();
                            fs.Dispose();

                            try
                            {
                                user =
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

                            if (user.Element("status").Attribute("state").Value == "online")
                            {
                                response = 2;
                             
                                break;
                            }
                            if (user.Attribute("password").Value != password)
                            {
                                response = 3;

                                break;
                            }

                            fs = new FileStream(@"files\details\" + user.Attribute("id").Value + ".xml", FileMode.Open);
                            details = XDocument.Load(fs);

                            fs.Close();
                            fs.Dispose();
                            list.Add(new XElement("friends"));

                            friendIDs = 
                                from frnd in details.Root.Element("friends").Elements()
                                select frnd.Attribute("id").Value;

                            foreach (string id in friendIDs)
                            {
                                user = users.Root.Elements().Single(usr => usr.Attribute("id").Value == id);
                                listItem = new XElement(user.Attribute("username").Value, new XElement("status"));

                                if (user.Element("status").Attribute("state").Value == "offline")
                                {
                                    listItem.Element("status").SetAttributeValue("state", "offline");
                                    list.Root.Add(listItem);

                                    continue;
                                }

                                listItem.Element("status").SetAttributeValue("state", "online");
                                listItem.Element("status").Value = user.Element("state").Value;

                                fs = new FileStream(@"files\details\" + id + ".xml", FileMode.Open);
                                friend = XDocument.Load(fs);

                                fs.Close();
                                fs.Dispose();
                                listItem.Add(new XElement("last_address_used"));
                                listItem.Element("last_address_used").SetAttributeValue("ip_address", friend.Root.Element("last_address_used").Attribute("ip_address").Value);
                                listItem.Element("last_address_used").SetAttributeValue("port", friend.Root.Element("last_address_used").Attribute("port").Value);
                                list.Root.Add(listItem);

                                friend = null;
                                listItem = null;
                                user = null;
                            }

                            responseMessage = list.ToString(SaveOptions.DisableFormatting);
                            responseLength = responseMessage.Length;
                        }

                        break;
                    case 2: //Sign out
                        {
                            
                        }

                        break;
                    case 3: //Sign up
                        {
                            bool usernameExists;
                            int lastUserID = -1;
                            IPEndPoint remoteEndPoint;
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
                    /*
                     * case 4: //Change status message
                     *     break;
                     */
                    default:
                        break;
                }
            }
            catch (DecoderFallbackException dfe) { Console.WriteLine("DecodeFallbackException: {0}", dfe); }
            catch (ArgumentOutOfRangeException aoore) { Console.WriteLine("ArgumentOutOfRangeException: {0}", aoore); }
            catch (ArgumentNullException ane) { Console.WriteLine("ArgumentNullException: {0}", ane); }
            catch (ArgumentException ae) { Console.WriteLine("ArgumentException: {0}", ae); }
            catch (ObjectDisposedException ode) { Console.WriteLine("ObjectDisposedException: {0}", ode); }
            catch (IOException ioe) { Console.WriteLine("IOException: {0}", ioe); }
            catch (NotSupportedException nse) { Console.WriteLine("NotSupportedException: {0}", nse); }
            catch (Exception e) { Console.WriteLine("Exception: {0}", e); }
            finally
            {
                Console.WriteLine();

                rwl.ReleaseLock();
                stream.WriteByte(response);

                if (command == 1 && response == 0)
                {
                    bytes = BitConverter.GetBytes(responseLength);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);

                    stream.Write(bytes, 0, bytes.Length);

                    bytes = Encoding.ASCII.GetBytes(responseMessage);

                    stream.Write(bytes, 0, bytes.Length);
                }

                client.Close();
            }
        }
    }
}
