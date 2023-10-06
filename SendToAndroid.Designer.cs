namespace StravaHeatMapToKMZ
{
    partial class SendToAndroid
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            adbPath = new TextBox();
            setAdbPath = new Button();
            setTilesPath = new Button();
            tilesPath = new TextBox();
            remotePath = new TextBox();
            label1 = new Label();
            bSend = new Button();
            SuspendLayout();
            // 
            // adbPath
            // 
            adbPath.Location = new Point(205, 12);
            adbPath.Name = "adbPath";
            adbPath.ReadOnly = true;
            adbPath.Size = new Size(724, 23);
            adbPath.TabIndex = 0;
            // 
            // setAdbPath
            // 
            setAdbPath.Location = new Point(38, 12);
            setAdbPath.Name = "setAdbPath";
            setAdbPath.Size = new Size(147, 23);
            setAdbPath.TabIndex = 1;
            setAdbPath.Text = "Set adb.exe path ..";
            setAdbPath.UseVisualStyleBackColor = true;
            setAdbPath.Click += setAdbPath_Click;
            // 
            // setTilesPath
            // 
            setTilesPath.Location = new Point(38, 51);
            setTilesPath.Name = "setTilesPath";
            setTilesPath.Size = new Size(147, 23);
            setTilesPath.TabIndex = 3;
            setTilesPath.Text = "Set tiles path ...";
            setTilesPath.UseVisualStyleBackColor = true;
            setTilesPath.Click += setTilesPath_Click;
            // 
            // tilesPath
            // 
            tilesPath.Location = new Point(205, 51);
            tilesPath.Name = "tilesPath";
            tilesPath.ReadOnly = true;
            tilesPath.Size = new Size(724, 23);
            tilesPath.TabIndex = 2;
            // 
            // remotePath
            // 
            remotePath.Location = new Point(205, 89);
            remotePath.Name = "remotePath";
            remotePath.Size = new Size(724, 23);
            remotePath.TabIndex = 4;
            remotePath.Text = "/sdcard/offline/heatmap/";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(24, 92);
            label1.Name = "label1";
            label1.Size = new Size(161, 15);
            label1.TabIndex = 5;
            label1.Text = "Android device remote path :";
            // 
            // bSend
            // 
            bSend.Location = new Point(443, 132);
            bSend.Name = "bSend";
            bSend.Size = new Size(75, 23);
            bSend.TabIndex = 6;
            bSend.Text = "Send";
            bSend.UseVisualStyleBackColor = true;
            bSend.Click += bSend_Click;
            // 
            // SendToAndroid
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(964, 172);
            Controls.Add(bSend);
            Controls.Add(label1);
            Controls.Add(remotePath);
            Controls.Add(setTilesPath);
            Controls.Add(tilesPath);
            Controls.Add(setAdbPath);
            Controls.Add(adbPath);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "SendToAndroid";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Send to Android device";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox adbPath;
        private Button setAdbPath;
        private Button setTilesPath;
        private TextBox tilesPath;
        private TextBox remotePath;
        private Label label1;
        private Button bSend;
    }
}