using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Client
{
    public partial class AddFriend : Form
    {
        public AddFriend()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != null && textBox1.Text != "")
            {
                string friendUsername = textBox1.Text;
                string message = textBox2.Text;
                MainForm pointerToMainForm = (MainForm)Application.OpenForms[0];
                pointerToMainForm.call_addFriendToolStripMenuItem(friendUsername, message);
                 }
            else
            {
                MessageBox.Show("No username! Please try again!");
            }
            this.Close();
            this.Dispose();
           
        }

       
    }
}
