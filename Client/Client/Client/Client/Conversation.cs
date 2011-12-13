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

namespace Client
{
    public partial class Conversation : Form
    {
        public string id;
        public string id_name = "Eu: ";
        public string message;
        public Bitmap myLogoConv;
        public string myFriend;
        public string statusFriend;
        private ChatClient _chatClient = null;
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


        public void Receive_Msg_Auto(string message)
        {
            Font forIdName = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
            Font forMessage = new System.Drawing.Font("Arial", 10, FontStyle.Regular);

            if (message != "")
            {
                textBox1.SelectionColor = Color.Blue;
                textBox1.SelectionFont = forIdName;
                //                textBox1.SelectedText = id_name;
                textBox1.SelectedText = myFriend + ": ";

                textBox1.SelectionColor = Color.Black;
                textBox1.SelectionFont = forMessage;

                textBox1.SelectedText = message + "\n";

            }


        }


        private void WriteMessage(string message)
        {
            Font forIdName = new System.Drawing.Font("Arial", 10, FontStyle.Bold);
            Font forMessage = new System.Drawing.Font("Arial", 10, FontStyle.Regular);


                textBox1.SelectionColor = Color.Gray;
                textBox1.SelectionFont = forIdName;
                textBox1.SelectedText = id_name;

                textBox1.SelectionColor = Color.Black;
                textBox1.SelectionFont = forMessage;

                textBox1.SelectedText = message + "\n";
                //Dinu! Sending Message
                _chatClient.SendMessage(myFriend, message);

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
            MainForm pointerToMainForm = (MainForm)Application.OpenForms[0];
            pointerToMainForm.CloseConversation(id);
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

       
    }
}
