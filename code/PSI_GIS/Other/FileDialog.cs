using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PSI_GIS
{
    public partial class FileDialog : Form
    {
        public FileDialog()
        {
            InitializeComponent();
        }

        private void FileDialog_Load(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result != DialogResult.OK)
            {
                MessageBox.Show("Error");
                Application.Exit();
            }
            Program.Work(openFileDialog1.FileNames);
            Application.Exit();
        }
    }
}
