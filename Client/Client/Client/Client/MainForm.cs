using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Chat;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Diagnostics;
using System.Drawing.Imaging;

namespace Client
{
    public partial class MainForm : Form
    {
        List<string> idNames = new List<string>();
        Bitmap myLogo;

        private ChatClient _chatClient = null;

        public MainForm()
        {
            InitializeComponent();
            AlignWindow();

            //Start Dinu!
            Stream stream = File.Open("config.info", FileMode.Open);
            BinaryFormatter bformatter = new BinaryFormatter();
            bformatter = new BinaryFormatter();

            ConnectingSettingsData info = (ConnectingSettingsData)bformatter.Deserialize(stream);
            stream.Close();

            int port = Int32.Parse(info.port);

            _chatClient = new ChatClient(info.ip, port);
            _chatClient.SetMessageReceiver(ReceiveMessage);
            _chatClient.SetFileReceiver(ConfirmFileReceivement, GetSavePath);
            _chatClient.SetFriendRequestConfirmation(ConfirmFriendRequest);
            _chatClient.SetNotifier(Notify);

            //or in other place
            
            //End Dinu!    
        }

        private void AlignWindow()
        {
            //align the window to the right side of the screen and with maximum height
            Rectangle r = Screen.PrimaryScreen.WorkingArea;
            this.StartPosition = FormStartPosition.Manual;
            this.SetBounds(0, 0, this.Size.Width, Screen.PrimaryScreen.WorkingArea.Height);
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width, Screen.PrimaryScreen.WorkingArea.Height - this.Height);
            if (this.notifyIcon1.ContextMenu == null)
                this.notifyIcon1.ContextMenu = new ContextMenu();
            this.notifyIcon1.ContextMenu.MenuItems.Add(0, new MenuItem("Exit...", new System.EventHandler(this.ExitClick)));

        }
        
        public void UpdateChatClient(string ip, int port)
        {
            _chatClient.SignOut();

            _chatClient = new ChatClient(ip, port);
            _chatClient.SetMessageReceiver(ReceiveMessage);
            _chatClient.SetFileReceiver(ConfirmFileReceivement, GetSavePath);
            _chatClient.SetFriendRequestConfirmation(ConfirmFriendRequest);
            _chatClient.SetNotifier(Notify);
        }

        private void Sign_Up(object sender, EventArgs e)
        {
            this.Controls.Remove(this.pictureBox1);
            this.Controls.Remove(this.textBox1);
            this.Controls.Remove(this.textBox2);
            this.Controls.Remove(this.button1);
            this.Controls.Remove(this.button2);



            this.textBox1.SetBounds(103, 152, 150, 20);
            this.label1.Location = new System.Drawing.Point(10, 152);
            this.label1.Text = "Username";



            this.textBox2.SetBounds(103, 182, 150, 20);
            this.textBox2.UseSystemPasswordChar = true;
            this.label2.Location = new System.Drawing.Point(10, 185);
            this.label2.Text = "Password";


            this.textBox3.SetBounds(103, 212, 150, 20);
            this.textBox3.UseSystemPasswordChar = true;
            this.label3.Location = new System.Drawing.Point(10, 217);
            this.label3.Text = "Conf. Password";


            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox3);

            this.Controls.Add(this.button1);
            this.Controls.Add(this.button2);

            this.button1.Text = "Sign up";
            this.button1.Location = new System.Drawing.Point(103, 245);


            this.button2.Text = "Back";
            this.button2.Location = new System.Drawing.Point(180, 245);

            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            this.Controls.Remove(this.pictureBox1);
            this.Controls.Add(this.pictureBox2);

            this.button1.Click -= new System.EventHandler(this.Sign_in);
            this.button1.Click += new System.EventHandler(this.Create_Account);


            this.button2.Click -= new System.EventHandler(this.Sign_Up);
            this.button2.Click += new System.EventHandler(this.Quit);



        }

        public void Quit(object sender, EventArgs e)
        {

            this.Controls.Remove(this.pictureBox2);
            this.notifyIcon1.Visible = false;
            this.menuStrip1.Visible = false;
            this.Controls.Remove(this.label1);
            this.Controls.Remove(this.label2);
            this.Controls.Remove(this.label3);
            this.Controls.Remove(this.label4);
            this.Controls.Remove(this.label5);
            this.Controls.Remove(this.label6);

            this.Controls.Remove(this.button1);
            this.Controls.Remove(this.button2);

            this.Controls.Remove(this.textBox1);
            this.Controls.Remove(this.textBox2);
            this.Controls.Remove(this.textBox3);
            this.Controls.Remove(this.textBox4);
            this.Controls.Remove(this.textBox5);
            this.Controls.Remove(this.textBox6);

            InitializeComponent();
            AlignWindow();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            MainForm pointerToMainForm = (MainForm)Application.OpenForms[0];
            if (FormWindowState.Normal == WindowState)
            {
                Hide();
                WindowState = FormWindowState.Minimized;
                pointerToMainForm.ShowMessageNotifyIcon(" Minimized in tray");

            }
            else
            {
                Show();
                WindowState = FormWindowState.Normal;
                pointerToMainForm.ShowMessageNotifyIcon(" Restored from tray");

            }
        }

        private void Sign_in(object sender, EventArgs e)
        {
            if (this.textBox1.Text.Length == 0 || this.textBox2.Text.Length == 0)
            {
                MessageBox.Show("Unauthorized log without username and password", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
            }
            else
            if (this.textBox1.Text.Length < 3 || this.textBox1.Text.Length > 15)
            {
                MessageBox.Show("Username length between 3 and 15 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

            }
            else 
            if (this.textBox2.Text.Length < 6 || this.textBox2.Text.Length > 50)
            {
                MessageBox.Show("Password length between 6 and 50 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

            }
            else
            {
                //Start Dinu!

                string username = this.textBox1.Text;
                string password = this.textBox2.Text;

                int response = _chatClient.SignIn(username, password);
                if (response == 0)
                {
                    //TODO check if successful signIn
                    //if signIn
                    this.textBox1.Text = username;
                    this.textBox2.Text = _chatClient.StatusMessage; 
                    if(_chatClient.LogoFileName != "")
                    {
                        if (File.Exists(_chatClient.LogoFileName))
                        {
                            this.pictureBox1.Image = Image.FromFile(_chatClient.LogoFileName);
                        }
                        else
                        {
                            //TODO load image
                        }
                    }

                    //End Dinu!
                    LoadUserListForm();
//                    string path = GetSavePath("me.txt");
//                    MessageBox.Show("path " + path);
                }
            }

        }

        delegate void ReceiveMessageCallback(string username, string message, DateTime date);

        //Start Dinu!  TODO improve
        void ReceiveMessage(string username, string message, DateTime date)
        {
            if (Application.OpenForms[0].InvokeRequired)
            {
                ReceiveMessageCallback d = new ReceiveMessageCallback(ReceiveMessage);
                this.Invoke(d, new object[] { username, message, date });
            }
            else
            {
                if (username != null && username != "" && message != null && message != "")
                {
                    foreach (Form OpenForm in Application.OpenForms)
                    {

                        if (OpenForm is Conversation)
                        {
                            Conversation temp = (Conversation)OpenForm;
                            if (username == temp.id)
                            {
//                                OpenForm.TopMost = true;
                                OpenForm.Focus();
                                temp.Receive_Msg_Auto(message, date);
                                //                                MessageBox.Show("Found");
                                return;
                            }
                        }
                    }

                    //TODO send maybe my name and my/other logo rethink
                    Conversation convFriend = new Conversation(_chatClient, username, null, username, "Tmp Status"); //TODO modify
                    convFriend.Show();
                    convFriend.Receive_Msg_Auto(message, date);
                    //                    MessageBox.Show("Created new");
                }
            }

            // Console.WriteLine("Received Message from: {0} \nMessage: {1}", username, message);
        }


        delegate void RefreshListCallback();
        void Notify(int option, string message)
        {
            //Option:
            //1. Connection Died
            //2. Users information changed (need to update)
            //3. File Received (Rethink)

            switch(option)
            {
                case 1:
                    //TODO do this
                    MessageBox.Show(message);
                    signOutToolStripMenuItem_Click();
//                    Console.WriteLine("Connection Died:\n " + message);
                    break;
                case 2:
                    if (Application.OpenForms[0].InvokeRequired)
                    {
                        RefreshListCallback d = new RefreshListCallback(RefreshFriendsList);
                        this.Invoke(d);
                    }
                    else
                        RefreshFriendsList();
//                    MessageBox.Show(message);
                    break;
                case 3:
//                    Console.WriteLine(message);
                    MessageBox.Show(message);
                    break;
            }

            //TODO
        }

        bool ConfirmFileReceivement(string filename, long size)
        {
            //true - accept ; false - don't accept
            if (MessageBox.Show("Do you want to receive file \"" + filename + "\" of size: " + (int)(size/1024) + " KB ?", "Recieve file", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                return true;
            }
            else return false;
           
        }

        bool ConfirmFriendRequest(string username, string message)
        {
            return
                (MessageBox.Show("Do you accept user " + username + " as friend ?\nMessage: " + message, "Friend Request",
                                 MessageBoxButtons.YesNo) == DialogResult.Yes);
        }

        delegate string GetSavePathCallback(string filename);

        string GetSavePath(string filename)
        {
//            MessageBox.Show("Should be");
            if (Application.OpenForms[0].InvokeRequired)
            {
                GetSavePathCallback d = new GetSavePathCallback(GetSavePath);
                return (string)this.Invoke(d, new object[] {filename});
            }
            else
            {

                SaveFileDialog saveDialog = new SaveFileDialog();
                if(filename.LastIndexOf('.')> 0)
                {
                    string extension = filename.Substring(filename.LastIndexOf('.') + 1);
                    saveDialog.Filter = "specific files (*."+extension+")|*."+extension+"|All files (*.*)|*.*";
                }
                else
                {
                    saveDialog.Filter = "All files (*.*)|*.*";
                }
                saveDialog.FileName = filename;
                
                string path = null;
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    path = Path.GetFullPath(saveDialog.FileName);
                }
                return path;
            }
            
        }

        private void SignOut()
        {
            if(_chatClient != null)
                _chatClient.SignOut();
        }


        //End Dinu!

        private void LoadUserListForm()
        {
            this.Controls.Remove(this.pictureBox1);
            this.Controls.Remove(this.textBox1);
            this.Controls.Remove(this.textBox2);
            this.Controls.Remove(this.button1);
            this.Controls.Remove(this.button2);
            this.Controls.Remove(this.label1);
            this.Controls.Remove(this.label2);
            this.Controls.Remove(this.label3);
            this.optionsToolStripMenuItem.Text = "User options";
            this.settingsToolStripMenuItem.Visible = false;
            this.signOutToolStripMenuItem.Visible = true;
            this.changeLogoToolStripMenuItem.Visible = true;
            this.addFriendToolStripMenuItem.Visible = true;


            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserList));
            this.listView1 = new System.Windows.Forms.ListView();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // listView1
            // 
            this.listView1.BackColor = System.Drawing.Color.LightGray;
            this.listView1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listView1.FullRowSelect = true;
            this.listView1.GridLines = false;
            this.listView1.Location = new System.Drawing.Point(0, 96);
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(151, 496);

            this.listView1.TabIndex = 1;
            this.listView1.TileSize = new System.Drawing.Size(1, 1);
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.DoubleClick += new System.EventHandler(this.DoubleClick_Friend);
            this.listView1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.DoubleClick_Friend);
            this.listView1.ColumnWidthChanging += new System.Windows.Forms.ColumnWidthChangingEventHandler(this.listView_ColumnWidthChanging);
            // 
            // pictureBox1
            // 
            if (_chatClient.LogoFileName != "")
            {
                this.pictureBox1.Image = Image.FromFile(_chatClient.LogoFileName);
                //TODO if file not exists load from server
            }
            else
                this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(10, 30);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(96, 98);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(111, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Nume";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(111, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Status";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(157, 41);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(106, 20);
            this.textBox1.TabIndex = 5;
            //numele trebuie dat de la server pornind de la username/password in
            this.textBox1.Text = _chatClient.UserName;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(157, 74);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(106, 20);
            this.textBox2.TabIndex = 6;
            this.textBox2.KeyPress += new KeyPressEventHandler(this.ChangeStatus);
            this.textBox2.Text = _chatClient.StatusMessage;
            // 
            // UserList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.listView1);
            this.Name = "UserList";
            this.Text = "UserList";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
            //incarcam lista de useri de la server
            MainForm pointerToMainForm = (MainForm)Application.OpenForms[0];
            pointerToMainForm.ShowMessageNotifyIcon(" Loading agenda ...");

            //metoda de testare - am adaugat manual 2 "prieteni"
            PopulateListView();


        }

        private void PopulateListView()
        {
       //commnet
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem();
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem();  
        
         listViewItem1.UseItemStyleForSubItems = false;
        //commnet
         
           listView1.Sorting = SortOrder.Ascending;
            listView1.AllowColumnReorder = true;
            listView1.SmallImageList = imageList1;

            //comment
            this.listView1.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2});
            //commnet

            ColumnHeader header1, header2, header3, header4;
            header1 = new ColumnHeader();
            header2 = new ColumnHeader();
            header3 = new ColumnHeader();
            header4 = new ColumnHeader();

            header1.Text = "";
            header1.TextAlign = HorizontalAlignment.Center;
            header1.Width = 40;
            

            header2.Text = "Username";
            header2.TextAlign = HorizontalAlignment.Center;
            header2.Width = 110;

            header3.TextAlign = HorizontalAlignment.Left;
            header3.Text = "Status message";
            header3.Width = 115;

            header4.TextAlign = HorizontalAlignment.Left;
            header4.Text = "";
            header4.Width = 5;



            //this.listView1.Columns.Add("", 15);
            this.listView1.Columns.Add(header1);
            this.listView1.Columns.Add(header2);
            this.listView1.Columns.Add(header3);
            this.listView1.Columns.Add(header4);

            //this.listView1.SetBounds(0, 0, 100, 100);


            // this.listView1
            
            //comment
//                        listViewItem1.UseItemStyleForSubItems = false;
//                        listViewItem1.BackColor = Color.Red;
//                        listViewItem1.ForeColor = Color.Red;
//
//                        
//                        listViewItem1.ImageIndex = 0;
//
//                        
//                        listViewItem1.SubItems.Add("Maria Ionescu");
//                        listViewItem1.SubItems.Add("mananc..");
//                        listViewItem1.SubItems.Add("");
//                        listViewItem1.SubItems[3].BackColor = Color.Red;
//                        
//                        
//                        //listViewItem1.SubItems[0].
//                        listViewItem1.SubItems[1].Font = new Font(listViewItem1.SubItems[1].Font, FontStyle.Bold);
//                        listViewItem1.BackColor = Color.Red;
//                        
//                   
//                        listViewItem2.ImageIndex = 0;
//                        
//                        listViewItem2.SubItems.Add("Ion Amariei");
//                        listViewItem2.SubItems.Add("lucrez...");
//                        listViewItem2.SubItems.Add("");
//                      
//                        listViewItem2.SubItems[3].BackColor = Color.Green;
//                        listViewItem2.UseItemStyleForSubItems = false;
//                        listViewItem2.BackColor = Color.Green;
//                        listViewItem2.SubItems[1].Font = new Font(listViewItem2.SubItems[1].Font, FontStyle.Bold);
            //commnet
            
            RefreshFriendsList();
        }

        private void RefreshFriendsList()
        {
            //clear all items in the list
            this.listView1.Items.Clear();//trebuie testat
            
            ListViewItem friendItem = null;
            var friends = _chatClient.GetFriends();
            foreach (var friend in friends)
            {
                friendItem = new ListViewItem();

                //TODO inspect ???
                friendItem.ImageIndex = 0;
                friendItem.SubItems.Add(friend.Name);
                friendItem.SubItems.Add(friend.StatusMessage);
                friendItem.SubItems.Add(""); // TODO inspect?
                friendItem.UseItemStyleForSubItems = false;
                //                listViewItem2.SubItems[3].BackColor = Color.Green;
                if (friend.Online())
                {
                    //                    friendItem.BackColor = Color.Green;
                    friendItem.SubItems[3].BackColor = Color.FromArgb(0,205,102);
                }
                else
                {
                    //                    friendItem.BackColor = Color.Red;
                    friendItem.SubItems[3].BackColor = Color.FromArgb(238,44,44);
                }

                friendItem.SubItems[1].Font = new Font(friendItem.SubItems[1].Font, FontStyle.Bold);
                this.listView1.Items.Add(friendItem);
            }
        }


        //TODO improve
        private void Create_Account(object sender, EventArgs e)
        {
            bool good = true;
            //verificam campurile introduse de utlizator
            //username
            if ( this.textBox1.Text.Length < 3)
            {
                MessageBox.Show("Username must have at least 3 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                textBox1.ForeColor = Color.Red;
                good = false;
            }
            else textBox1.ForeColor = Color.Green;

            //parola
            //trebuie parola alpha- numerica
            if ( this.textBox2.Text.Length < 6)
            {
                MessageBox.Show("Password must have at least 6 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                textBox2.ForeColor = Color.Red;
                good = false;
            }
            else textBox2.ForeColor = Color.Green;

            if (this.textBox3.Text.Length == 0 || this.textBox3.Text.Length < 6)
            {
                MessageBox.Show("Password confirmation must have at least 6 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                textBox3.ForeColor = Color.Red;
                good = false;
            }
            else textBox3.ForeColor = Color.Green;
            
            if (!good) return;

            if (this.textBox2.Text != this.textBox3.Text)
            {
                MessageBox.Show("Password and password confirmation don't match", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                textBox2.ForeColor = Color.Red;
                textBox3.ForeColor = Color.Red;
                good = false;
            }
            else
            {
                textBox2.ForeColor = Color.Green;
                textBox3.ForeColor = Color.Green;

            }

            if (!good) return;

            if (!Regex.IsMatch(this.textBox2.Text, @"^[a-zA-Z0-9]+$") && !Regex.IsMatch(this.textBox3.Text, @"^[a-zA-Z0-9]+$"))
            {
                MessageBox.Show("Password must contain alfa-numeric characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                textBox2.ForeColor = Color.Red;
                textBox3.ForeColor = Color.Red;
                good = false;
            }
            else
            {
                textBox2.ForeColor = Color.Green;
                textBox3.ForeColor = Color.Green;

            }

            if (good)
            {
                string username = this.textBox1.Text;
                string password = this.textBox2.Text;

                int response = _chatClient.SignUp(username, password);
                if(response == 0) //Success
                {
                    response = _chatClient.SignIn(username, password);
                    if (response == 0)
                    {
                        this.textBox1.Text = username;
                        this.textBox2.Text = _chatClient.StatusMessage; 
                        LoadUserListForm();
                        this.Controls.Remove(this.textBox3); //Delete manual cimpul
                        this.Controls.Remove(this.pictureBox2);
                    }
                }
                    //Print errors
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {

        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConnectingSettings x = new ConnectingSettings();
            x.Show();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            new ConnectingSettings().LoadConnectingSettingsOnStart();
        }

        public void ShowMessageNotifyIcon(string info)
        {
            notifyIcon1.ShowBalloonTip(1000, "info", info, ToolTipIcon.Info);

        }

        private void DoubleClick_Friend(object sender, EventArgs e)
        {

            Conversation pointerToForm = (Conversation)Application.OpenForms["Conversation"];
            ListView lw = (ListView)sender;
            ListViewItem lwi = lw.SelectedItems[0];
            ListViewItem.ListViewSubItem lws = lwi.SubItems[1];

            foreach (Form OpenForm in Application.OpenForms)
                {
                    if (OpenForm is Conversation)
                    {
                        Conversation temp = (Conversation)OpenForm;
                        if (lws.Text == temp.id)
                        {
                            OpenForm.Focus();
                            return;
                        }
                    }


                }
//            int ok = 1;
//            foreach (string temp in idNames)
//            {
//                if (temp == lws.Text)
//                {
//                    ok = 0;
//                    break;
//                }
//            }
//            if (ok == 1)
//            {
//                idNames.Add(lws.Text);
                string name = listView1.SelectedItems[0].SubItems[1].Text;
                string statusMes = listView1.SelectedItems[0].SubItems[2].Text;
                Conversation conv = new Conversation(_chatClient, lws.Text, myLogo, name, statusMes);
                conv.Show();


        }

        public void CloseConversation(string id)
        {
            idNames.Remove(id);
        }

        private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                Sign_in(sender, e);
            }

        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                Sign_in(sender, e);
            }
        }

        private void signOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Controls.Remove(this.pictureBox1);
            this.Controls.Remove(this.textBox1);
            this.Controls.Remove(this.textBox2);
            this.Controls.Remove(this.label1);
            this.Controls.Remove(this.label2);
            this.Controls.Remove(this.listView1);
            this.Controls.Remove(this.menuStrip1);
            this.notifyIcon1.Visible = false;
//            _chatClient.SignOut();

            SignOut();
            InitializeComponent();
            AlignWindow();
        }

        private void signOutToolStripMenuItem_Click()
        {
            this.Controls.Remove(this.pictureBox1);
            this.Controls.Remove(this.textBox1);
            this.Controls.Remove(this.textBox2);
            this.Controls.Remove(this.label1);
            this.Controls.Remove(this.label2);
            this.Controls.Remove(this.listView1);
            this.Controls.Remove(this.menuStrip1);
            this.notifyIcon1.Visible = false;
//            _chatClient.SignOut();

            SignOut();
            InitializeComponent();
            AlignWindow();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            MainForm pointerToMainForm = (MainForm)Application.OpenForms[0];
            if (FormWindowState.Normal == WindowState)
            {
                Hide();
                WindowState = FormWindowState.Minimized;
                pointerToMainForm.ShowMessageNotifyIcon(" Minimized in tray");
            }
            else
            {
                Show();
                pointerToMainForm.ShowMessageNotifyIcon(" Restored from tray");
                WindowState = FormWindowState.Normal;
            }
        }

        private void ChangeStatus(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                //trebuie sa transmiti statusul la toti clienti

                MainForm pointerToMainForm = (MainForm)Application.OpenForms[0];
                pointerToMainForm.ShowMessageNotifyIcon(" Changed Status");
                _chatClient.ChangeStatus(this.textBox2.Text);
            }
        }

        private void ChangeLogo(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();

            open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
            if (open.ShowDialog() == DialogResult.OK)
            {

                myLogo = new Bitmap(open.FileName);
                Rectangle rect;
                if (myLogo.Width < myLogo.Height)
                {
                    rect = new Rectangle(0, (myLogo.Height-myLogo.Width)/2, myLogo.Width, myLogo.Width);
                }
                else
                {
                    rect = new Rectangle((myLogo.Width - myLogo.Height) / 2, 0, myLogo.Height, myLogo.Height);
                }

                myLogo = myLogo.Clone(rect, myLogo.PixelFormat);

                //TODO to thread
                MemoryStream ms = new MemoryStream();
                myLogo.Save(ms, ImageFormat.Bmp);
                string filename = _chatClient.ChangeLogo(ms.ToArray());
//                byte[] bitmapData = ms.ToArray();
                myLogo.Save(filename, ImageFormat.Bmp);

                pictureBox1.Image = myLogo;
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
//            this.signOutToolStripMenuItem_Click(sender, e);
//            Process[] processlist = Process.GetProcesses();
//
//            foreach(Process theprocess in processlist){
//                string infoProcess = "Process: " + theprocess.ProcessName + " ID: " + theprocess.Id;
//                if (theprocess.ProcessName.Contains("Client"))
//                {
//                    //MessageBox.Show(infoProcess);
//                    theprocess.Kill();    
//                }
//            }
            SignOut();
            this.Close();
            this.Dispose();
//            Application.Exit();
        }

        private void ExitClick(object sender, EventArgs e)
        {
//            this.signOutToolStripMenuItem_Click(sender, e);
//            Process[] processlist = Process.GetProcesses();
//
//            foreach (Process theprocess in processlist)
//            {
//                string infoProcess = "Process: " + theprocess.ProcessName + " ID: " + theprocess.Id;
//                if (theprocess.ProcessName.Contains("Client"))
//                {
//                    //MessageBox.Show(infoProcess);
//                    theprocess.Kill();
//                }
//
//
//            }
            SignOut();
            this.Close();
            this.Dispose();
//            Application.Exit();
        }

        private void listView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = listView1.Columns[e.ColumnIndex].Width;
        }

        //TODO need implementation
        private void addFriendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //form nou + comanda executa din form nou spre formul mainForm
            AddFriend formFriend = new AddFriend();
            formFriend.Show();

            
            //_chatClient.SendFriendRequest("vanea1234");
        }
        public void call_addFriendToolStripMenuItem(string friendUsername, string message)
        {
            //form nou + comanda executa din form nou spre formul mainForm

            int response = _chatClient.SendFriendRequest(friendUsername, message);
            if (response == 0)
            {
                MessageBox.Show("Friend Added");
            }
            else
                if (response == 1)
                    MessageBox.Show("Nu exista asa user. ");
                else
                    if (response == 2)
                        MessageBox.Show("Userul este deja in lista de prieteni sau a ignorat cererea.");
            //3. Mai exista o cerere
        }
    }
}
