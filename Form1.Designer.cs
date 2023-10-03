namespace StravaHeatMapToKMZ
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            createKMZ = new Button();
            updateKMZ = new Button();
            progressBar1 = new ProgressBar();
            toggleElements = new Button();
            ((System.ComponentModel.ISupportInitialize)webView21).BeginInit();
            SuspendLayout();
            // 
            // webView21
            // 
            webView21.AllowExternalDrop = true;
            webView21.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            webView21.CreationProperties = null;
            webView21.DefaultBackgroundColor = Color.White;
            webView21.Enabled = false;
            webView21.Location = new Point(12, 40);
            webView21.Name = "webView21";
            webView21.Size = new Size(1000, 1000);
            webView21.Source = new Uri("https://www.strava.com/heatmap", UriKind.Absolute);
            webView21.TabIndex = 0;
            webView21.ZoomFactor = 1D;
            // 
            // createKMZ
            // 
            createKMZ.Enabled = false;
            createKMZ.Location = new Point(12, 11);
            createKMZ.Name = "createKMZ";
            createKMZ.Size = new Size(100, 23);
            createKMZ.TabIndex = 1;
            createKMZ.Text = "Create KMZ";
            createKMZ.UseVisualStyleBackColor = true;
            createKMZ.Click += createKMZ_Click;
            // 
            // updateKMZ
            // 
            updateKMZ.Enabled = false;
            updateKMZ.Location = new Point(118, 11);
            updateKMZ.Name = "updateKMZ";
            updateKMZ.Size = new Size(100, 23);
            updateKMZ.TabIndex = 2;
            updateKMZ.Text = "Update KMZ";
            updateKMZ.UseVisualStyleBackColor = true;
            updateKMZ.Click += updateKMZ_ClickAsync;
            // 
            // progressBar1
            // 
            progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar1.Location = new Point(305, 11);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(706, 23);
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.TabIndex = 3;
            // 
            // toggleElements
            // 
            toggleElements.Enabled = false;
            toggleElements.Location = new Point(224, 11);
            toggleElements.Name = "toggleElements";
            toggleElements.Size = new Size(75, 23);
            toggleElements.TabIndex = 4;
            toggleElements.Text = "Toggle UI";
            toggleElements.UseVisualStyleBackColor = true;
            toggleElements.Click += toggleElements_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1023, 1056);
            Controls.Add(toggleElements);
            Controls.Add(progressBar1);
            Controls.Add(updateKMZ);
            Controls.Add(createKMZ);
            Controls.Add(webView21);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Name = "Form1";
            Text = "Strava Heatmap to KMZ";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)webView21).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
        private Button createKMZ;
        private Button updateKMZ;
        private ProgressBar progressBar1;
        private Button toggleElements;
    }
}