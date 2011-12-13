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
    public partial class UserList : Form
    {
        public UserList()
        {
            InitializeComponent();
            PopulateListView();
            throw new Exception("hello"); //It is unused
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
            
            this.listView1.Columns.Add("", 15);
            this.listView1.Columns.Add("Username", 100);
            this.listView1.Columns.Add("Status", 100);

            listViewItem1.UseItemStyleForSubItems = false;
            listViewItem1.BackColor = Color.Red;

            //TODO inspect ???

            //listViewItem1.SubItems.
            listViewItem1.SubItems.Add("Maria Ionescu");
            listViewItem1.SubItems.Add("mananc..");


            listViewItem2.SubItems.Add("Ion Amariei");
            listViewItem2.SubItems.Add("lucrez...");
            listViewItem2.UseItemStyleForSubItems = false;
            listViewItem2.BackColor = Color.Green;

        }

       

        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            
            
        }

        public static Form IsFormAlreadyOpen(Type FormType)
        {
            foreach (Form OpenForm in Application.OpenForms)
            {
                if (OpenForm.GetType() == FormType)
                    return OpenForm;
            }

            return null;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void listView1_Resize(object sender, EventArgs e)
        {

        }

        private void listView1_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {

        }

       

       
       

        

        

       
        
        
        
    }
}
