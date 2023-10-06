using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StravaHeatMapToKMZ
{
    public partial class SendToAndroid : Form
    {
        public SendToAndroid()
        {
            InitializeComponent();
        }

        private void bSend_Click(object sender, EventArgs e)
        {
            if (adbPath.Text.Length > 0 && tilesPath.Text.Length > 0 && remotePath.Text.Length > 0)
            {
                bSend.Enabled = false;
                Process process = new();
                ProcessStartInfo startInfo = new();
                //startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.FileName = adbPath.Text;
                startInfo.Arguments = "push \"" + tilesPath.Text + "\\.\" \"" + remotePath.Text + "\"";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                bSend.Enabled = true;
            }
            else
            {
                MessageBox.Show("All fields are required !");
            }
        }

        private void setAdbPath_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new();
            openFileDialog.DefaultExt = "exe";
            openFileDialog.Filter = "adb.exe (adb.exe)|adb.exe";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                adbPath.Text = openFileDialog.FileName;
            }
        }

        private void setTilesPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "Select folder contain tiles (zoom folders)";

            var result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                tilesPath.Text = folderBrowserDialog.SelectedPath;
            }
        }
    }
}
