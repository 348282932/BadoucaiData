namespace Badoucai.WindowsForm.Zhaopin
{
    partial class WatchOldResumeForm
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
            this.tbx_Log = new System.Windows.Forms.TextBox();
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            this.btn_Connection = new System.Windows.Forms.Button();
            this.btn_Start = new System.Windows.Forms.Button();
            this.btn_Stop = new System.Windows.Forms.Button();
            this.btn_Login = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbx_Log
            // 
            this.tbx_Log.Location = new System.Drawing.Point(453, 110);
            this.tbx_Log.Multiline = true;
            this.tbx_Log.Name = "tbx_Log";
            this.tbx_Log.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbx_Log.Size = new System.Drawing.Size(193, 554);
            this.tbx_Log.TabIndex = 1;
            // 
            // webBrowser
            // 
            this.webBrowser.Location = new System.Drawing.Point(3, 12);
            this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new System.Drawing.Size(444, 652);
            this.webBrowser.TabIndex = 0;
            // 
            // btn_Connection
            // 
            this.btn_Connection.Location = new System.Drawing.Point(453, 6);
            this.btn_Connection.Name = "btn_Connection";
            this.btn_Connection.Size = new System.Drawing.Size(96, 49);
            this.btn_Connection.TabIndex = 2;
            this.btn_Connection.Text = "连接服务器";
            this.btn_Connection.UseVisualStyleBackColor = true;
            this.btn_Connection.Click += new System.EventHandler(this.btn_Connection_Click);
            // 
            // btn_Start
            // 
            this.btn_Start.Location = new System.Drawing.Point(453, 61);
            this.btn_Start.Name = "btn_Start";
            this.btn_Start.Size = new System.Drawing.Size(96, 43);
            this.btn_Start.TabIndex = 2;
            this.btn_Start.Text = "开始";
            this.btn_Start.UseVisualStyleBackColor = true;
            this.btn_Start.Click += new System.EventHandler(this.btn_Start_Click);
            // 
            // btn_Stop
            // 
            this.btn_Stop.Location = new System.Drawing.Point(550, 61);
            this.btn_Stop.Name = "btn_Stop";
            this.btn_Stop.Size = new System.Drawing.Size(96, 43);
            this.btn_Stop.TabIndex = 2;
            this.btn_Stop.Text = "暂停";
            this.btn_Stop.UseVisualStyleBackColor = true;
            this.btn_Stop.Click += new System.EventHandler(this.btn_Stop_Click);
            // 
            // btn_Login
            // 
            this.btn_Login.Location = new System.Drawing.Point(550, 6);
            this.btn_Login.Name = "btn_Login";
            this.btn_Login.Size = new System.Drawing.Size(96, 49);
            this.btn_Login.TabIndex = 3;
            this.btn_Login.Text = "登录";
            this.btn_Login.UseVisualStyleBackColor = true;
            this.btn_Login.Click += new System.EventHandler(this.btn_Login_Click);
            // 
            // WatchOldResumeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(658, 676);
            this.Controls.Add(this.btn_Login);
            this.Controls.Add(this.btn_Stop);
            this.Controls.Add(this.btn_Start);
            this.Controls.Add(this.btn_Connection);
            this.Controls.Add(this.tbx_Log);
            this.Controls.Add(this.webBrowser);
            this.Name = "WatchOldResumeForm";
            this.Text = "Watch旧系统智联简历";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.WatchOldResumeForm_FormClosing);
            this.Load += new System.EventHandler(this.WatchOldResumeForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox tbx_Log;
        private System.Windows.Forms.WebBrowser webBrowser;
        private System.Windows.Forms.Button btn_Connection;
        private System.Windows.Forms.Button btn_Start;
        private System.Windows.Forms.Button btn_Stop;
        private System.Windows.Forms.Button btn_Login;
    }
}