using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace ICP_Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string path = @"C:\ICP_Test_Server";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        private async Task<bool> WaitClient()
        {
            await Task.Delay(1000);
            return false;
        }
    }
}
