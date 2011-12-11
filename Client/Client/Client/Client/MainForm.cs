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
        }

        private void AlignWindow()
        {
            //align the window to the right side of the screen and with maximum height
            Rectangle r = Screen.PrimaryScreen.WorkingArea;
            this.StartPosition = FormStartPosition.Manual;
            this.SetBounds(0, 0, this.Size.Width, Screen.PrimaryScreen.WorkingArea.Height);
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width, Screen.PrimaryScreen.WorkingArea.Height - this.Height);
            this.notifyIcon1.ContextMenu.MenuItems.Add(0, new MenuItem("Exit...", new System.EventHandler(this.ExitClick))); 
            
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
            if (this.textBox1.Text.Length < 3 || this.textBox1.Text.Length > 15)
            {
                MessageBox.Show("Username length between 3 and 15 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
          
            }
            if (this.textBox2.Text.Length < 6 || this.textBox2.Text.Length > 50)
            {
                MessageBox.Show("Password length between 6 and 50 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);

            }
            else
            {

//                LoadUserListForm();//Dinu!
            }
            
        }

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
            this.textBox1.Text = "Cutarica Popica";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(157, 74);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(106, 20);
            this.textBox2.TabIndex = 6;
            this.textBox2.KeyPress += new KeyPressEventHandler(this.ChangeStatus);
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
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem();
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem();
            listViewItem1.UseItemStyleForSubItems = false;
            listView1.Sorting = SortOrder.Ascending;
            listView1.AllowColumnReorder = true;



            this.listView1.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2});

             ColumnHeader header1, header2,header3;
            header1 = new ColumnHeader();
            header2 = new ColumnHeader();
            header3 = new ColumnHeader();

            header1.Text = "";
            header1.TextAlign = HorizontalAlignment.Center;
            header1.Width = 15;
           

            header2.Text = "Username";
            header2.TextAlign = HorizontalAlignment.Center;
            header2.Width = 120;

            header3.TextAlign = HorizontalAlignment.Left;
            header3.Text = "Status message";
            header3.Width = 140;

            //this.listView1.Columns.Add("", 15);
            this.listView1.Columns.Add(header1);
            this.listView1.Columns.Add(header2);
            this.listView1.Columns.Add(header3);
            //this.listView1.SetBounds(0, 0, 100, 100);
           
            
            // this.listView1

            
            listViewItem1.UseItemStyleForSubItems = false;
            listViewItem1.BackColor = Color.Red;
            listViewItem1.SubItems.Add("Maria Ionescu");
            listViewItem1.SubItems.Add("mananc..");

            //listViewItem1.SubItems[0].
            listViewItem1.SubItems[1].Font = new Font(listViewItem1.SubItems[1].Font,FontStyle.Bold);

            listViewItem2.SubItems.Add("Ion Amariei");
            listViewItem2.SubItems.Add("lucrez...");
            listViewItem2.UseItemStyleForSubItems = false;
            listViewItem2.BackColor = Color.Green;
            listViewItem2.SubItems[1].Font = new Font(listViewItem2.SubItems[1].Font, FontStyle.Bold);

        }


        private void Create_Account(object sender, EventArgs e)
        {
            //verificam campurile introduse de utlizator
            //username
            if (this.textBox1.Text.Length == 0 ||this.textBox1.Text.Length < 8)
            {
                MessageBox.Show("Username must have at least 8 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning,MessageBoxDefaultButton.Button1);
                textBox1.ForeColor = Color.Red;
            }
            else textBox1.ForeColor = Color.Green;
            
            //parola
            //trebuie parola alpha- numerica
            if (this.textBox2.Text.Length == 0 || this.textBox2.Text.Length < 8)
            {
                MessageBox.Show("Password must have at least 8 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                textBox2.ForeColor = Color.Red;
            }
            else textBox2.ForeColor = Color.Green;

            if (this.textBox3.Text.Length == 0 || this.textBox3.Text.Length < 8)
            {
                MessageBox.Show("Password confirmation must have at least 8 characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                textBox3.ForeColor = Color.Red;
            }
            else textBox3.ForeColor = Color.Green;

            if (this.textBox2.Text != this.textBox3.Text)
            {
                MessageBox.Show("Password and password confirmation don't match", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                textBox2.ForeColor = Color.Red;
                textBox3.ForeColor = Color.Red;
            }
            else 
            {
                textBox2.ForeColor = Color.Green;
                textBox3.ForeColor = Color.Green;
            
            }
            if (!Regex.IsMatch(this.textBox2.Text, @"^[a-zA-Z0-9]+$") && !Regex.IsMatch(this.textBox3.Text, @"^[a-zA-Z0-9]+$"))
            {
                MessageBox.Show("Password must contain alfa-numeric characters", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
                textBox2.ForeColor = Color.Red;
                textBox3.ForeColor = Color.Red;
            }
            else
            {
                textBox2.ForeColor = Color.Green;
                textBox3.ForeColor = Color.Green;

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

            int ok =1;
            foreach (string temp in idNames)
            {
                if (temp == lws.Text)
                {
                    ok = 0;
                    break;
                }
            }
            if (ok == 1)
            {
                idNames.Add(lws.Text);
                string name = listView1.SelectedItems[0].SubItems[1].Text;
                string statusMes = listView1.SelectedItems[0].SubItems[2].Text;
                Conversation conv = new Conversation(lws.Text, myLogo,name,statusMes);
                conv.Show();

            }
            else 
            {
                int marked = 0;
                //string name = listView1.SelectedItems[0].SubItems[1].Text;
                Conversation temp1 = new Conversation(null,null,null,null);
                foreach (Form OpenForm in Application.OpenForms)
                {
                    if (OpenForm.GetType() == temp1.GetType())
                    {

                        if(OpenForm is Conversation)
                        {
                            Conversation temp = (Conversation)OpenForm;
                            if (lws.Text == temp.id)
                            {
                                OpenForm.TopMost = true;
                                OpenForm.Focus();
                                marked = 1;
                                break;
                            }
                            if (marked == 0)
                            {
                                OpenForm.TopMost = false;
                            }

                        }
                         
                    }
                        
                }
            }

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
            
            }
        }
        private void ChangeLogo(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog();
            
            open.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.bmp)|*.jpg; *.jpeg; *.gif; *.bmp";
            if (open.ShowDialog() == DialogResult.OK)
            {
                myLogo = new Bitmap(open.FileName);
                pictureBox1.Image = myLogo;
            } 
        } 

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.signOutToolStripMenuItem_Click(sender, e);
            this.Dispose();
        }

        private void ExitClick(object sender, EventArgs e)
        {
            this.signOutToolStripMenuItem_Click(sender, e);
            this.Dispose();
        }

        private void listView_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            e.Cancel = true;
            e.NewWidth = listView1.Columns[e.ColumnIndex].Width;
        }   
    }
}
