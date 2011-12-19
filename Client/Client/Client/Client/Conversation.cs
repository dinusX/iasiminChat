using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Chat;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Client
{
    public partial class Conversation : Form
    {
        public string id;
        public string id_name = "Eu";
        public string message;
        public Bitmap myLogoConv;
        public string myFriend;
        public string statusFriend;
        private ChatClient _chatClient = null;
        private ContentConversation conConversation = new ContentConversation();
//        private int hasText = 0;
//        private int countCurrentMessages = 0;

        //Receiving chatClient object
        public Conversation(ChatClient chatClient, string _id, Bitmap logo, string myfriend, string statusFriend)
        {
            _chatClient = chatClient;
            id = _id;
            this.myFriend = myfriend;
            this.statusFriend = statusFriend;
            
            InitializeComponent();
            if (logo != null)
            {
                pictureBox2.Image = logo;
            }
            if (statusFriend != null)
            {
                label1.Text = this.myFriend + " - " + this.statusFriend;
            }
            else
            {
                label1.Text = this.myFriend;
            }

            this.Text = this.myFriend;
            this.ActiveControl = textBox2;
        }

        private void Send_Msg(object sender, EventArgs e)
        {
            message = textBox2.Text;

            if (message != "")
            {
                WriteMessage(message);
            }
        }


        public void Receive_Msg_Auto(string message, DateTime date)
        {
            Font forIdName = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
            Font forMessage = new System.Drawing.Font("Arial", 10, FontStyle.Regular);

            textBox1.SelectionStart = textBox1.TextLength;
            textBox1.ScrollToCaret();

            //TODO write time 
            if (message != "")
            {
                textBox1.SelectionColor = Color.Blue;
                textBox1.SelectionFont = forIdName;
                //                textBox1.SelectedText = id_name;
                textBox1.SelectedText = myFriend + ": ";

                textBox1.SelectionColor = Color.Black;
                textBox1.SelectionFont = forMessage;

                textBox1.SelectedText = message + "\n";
                //
                conConversation.AppendMessage(message, myFriend, (DateTime)date);
//                countCurrentMessages++;
//
//                hasText = 1;
            }


        }


        private void WriteMessage(string message)
        {
            Font forIdName = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
            Font forMessage = new System.Drawing.Font("Arial", 10, FontStyle.Regular);

//            textBox1.SelectionStart;
            textBox1.SelectionStart = textBox1.TextLength;
            textBox1.ScrollToCaret();

            textBox1.SelectionColor = Color.Gray;
            textBox1.SelectionFont = forIdName;
            textBox1.SelectedText = id_name + ": ";

            textBox1.SelectionColor = Color.Black;
            textBox1.SelectionFont = forMessage;

            textBox1.SelectedText = message + "\n";
            //Dinu! Sending Message
            _chatClient.SendMessage(myFriend, message);

            //
            conConversation.AppendMessage(message, id_name, DateTime.Now);

//                countCurrentMessages++;
                //
//                hasText = 1;
            

            textBox2.ResetText();
            textBox2.Multiline = false;
            textBox2.Multiline = true;
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {            
                message = textBox2.Text;
                if (message != "\n")
                {
                    WriteMessage(message.Remove(message.LastIndexOf('\n')));
                }
            }
        }

       

        private void Conversation_FormClosing(object sender, FormClosingEventArgs e)
        {
            //serializare converatie
            
            FileStream flStream = new FileStream(this.Text + ".data", FileMode.OpenOrCreate, FileAccess.Write);

            try
            {
                BinaryFormatter binFormatter = new BinaryFormatter();
                binFormatter.Serialize(flStream, conConversation);
            }
            finally
            {
                //File.WriteAllText(this.Text, String.Empty);
                flStream.Close();
                textBox1.Clear();
                MainForm pointerToMainForm = (MainForm)Application.OpenForms[0];
                pointerToMainForm.CloseConversation(id);
            }

        }

        //TODO need implementation
        private void SendFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog choseFile = new OpenFileDialog();

//            choseFile.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
            if (choseFile.ShowDialog() == DialogResult.OK)
            {
                string filePath = choseFile.FileName;
                _chatClient.SendFile(myFriend, filePath);
            }  
        }

        private void Conversation_Load(object sender, EventArgs e)
        {
            conConversation = new ContentConversation();
            string filename = this.Text + ".data";
            if(File.Exists(filename))
            if (true)
            {
                FileStream flStream = null;
                try
                {
                    flStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
                    BinaryFormatter binFormatter = new BinaryFormatter();
                    conConversation = (ContentConversation)binFormatter.Deserialize(flStream);
                    if (conConversation != null) InsertInfoFromRecordList(conConversation, true);
                    //textBox1.Text = conConversation.GetAllMessages();
                    //cum setez pozitia
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ex: " + ex);
                }
                finally
                {
//                    hasText = 0;
                    if(flStream != null)
                        flStream.Close();

                }
            }
        }

        private void InsertInfoFromRecordList(ContentConversation conConversation, bool lastDayRecords)
        {
            Font forIdName = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
            Font forMessage = new System.Drawing.Font("Arial", 10, FontStyle.Regular);
            List<MessageRecord> myList = conConversation.mRec;


            textBox1.Clear();

            foreach (MessageRecord mRecord in myList)
            {

                //get data
                string id_name = mRecord.GetUsername();
                string message = mRecord.GetMessage();
                DateTime now = mRecord.GetReceivedTime();


                if (lastDayRecords == false)
                {

                    textBox1.SelectionColor = Color.Red;
                    textBox1.SelectedText = now.ToString();

                    if (id_name == "Eu")
                    {
                        textBox1.SelectionColor = Color.Gray;
                    }
                    else
                    {
                        textBox1.SelectionColor = Color.Blue;
                    }
                    textBox1.SelectionFont = forIdName;
                    textBox1.SelectedText = id_name +": ";

                    textBox1.SelectionColor = Color.Black;
                    textBox1.SelectionFont = forMessage;
                    textBox1.SelectedText = message + "\n";

                }
                else//true
                {
                    if (now.DayOfYear == DateTime.Now.DayOfYear)
                    {

                        textBox1.SelectionColor = Color.Red;
                        textBox1.SelectedText = now.ToString();

                        if (id_name == "Eu")
                        {
                            textBox1.SelectionColor = Color.Gray;
                        }
                        else
                        {
                            textBox1.SelectionColor = Color.Blue;
                        }
                        textBox1.SelectionFont = forIdName;
                        textBox1.SelectedText = id_name + ": ";

                        textBox1.SelectionColor = Color.Black;
                        textBox1.SelectionFont = forMessage;
                        textBox1.SelectedText = message + "\n";

                    }
                }
            }
        }

        private void GetHistoryAndActualMessages(bool val)
        {
            Font forIdName = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
            Font forMessage = new System.Drawing.Font("Arial", 10, FontStyle.Regular);

            textBox1.Clear();

            InsertInfoFromRecordList(this.conConversation, val);


        }
        //to implement
        private void HistoryButton_Click(object sender, EventArgs e)
        {
            if (this.HistoryButton.Text == "All History")//all history
            {
                this.HistoryButton.Text = "Last day History";
                GetHistoryAndActualMessages(false);
            }
            else //current day entries
            {
                this.HistoryButton.Text = "All History";
                GetHistoryAndActualMessages(true);
            }

        }

    }
}
