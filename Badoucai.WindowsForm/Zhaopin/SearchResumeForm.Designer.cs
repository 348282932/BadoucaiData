namespace Badoucai.WindowsForm.Zhaopin
{
    partial class SearchResumeForm
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
            this.components = new System.ComponentModel.Container();
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            this.lbl_SearchCount = new System.Windows.Forms.Label();
            this.btn_Start = new System.Windows.Forms.Button();
            this.btn_CreateSubAccount = new System.Windows.Forms.Button();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.lbl_SuccessCount = new System.Windows.Forms.Label();
            this.btn_Connecting = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // webBrowser
            // 
            this.webBrowser.Location = new System.Drawing.Point(12, 102);
            this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new System.Drawing.Size(700, 459);
            this.webBrowser.TabIndex = 0;
            // 
            // lbl_SearchCount
            // 
            this.lbl_SearchCount.AutoSize = true;
            this.lbl_SearchCount.Location = new System.Drawing.Point(56, 49);
            this.lbl_SearchCount.Name = "lbl_SearchCount";
            this.lbl_SearchCount.Size = new System.Drawing.Size(71, 12);
            this.lbl_SearchCount.TabIndex = 1;
            this.lbl_SearchCount.Text = "搜索次数：0";
            // 
            // btn_Start
            // 
            this.btn_Start.Location = new System.Drawing.Point(291, 36);
            this.btn_Start.Name = "btn_Start";
            this.btn_Start.Size = new System.Drawing.Size(92, 38);
            this.btn_Start.TabIndex = 2;
            this.btn_Start.Text = "开始";
            this.btn_Start.UseVisualStyleBackColor = true;
            this.btn_Start.Click += new System.EventHandler(this.btn_Start_Click);
            // 
            // btn_CreateSubAccount
            // 
            this.btn_CreateSubAccount.Location = new System.Drawing.Point(399, 36);
            this.btn_CreateSubAccount.Name = "btn_CreateSubAccount";
            this.btn_CreateSubAccount.Size = new System.Drawing.Size(92, 38);
            this.btn_CreateSubAccount.TabIndex = 2;
            this.btn_CreateSubAccount.Text = "设置登录信息";
            this.btn_CreateSubAccount.UseVisualStyleBackColor = true;
            this.btn_CreateSubAccount.Click += new System.EventHandler(this.btn_CreateSubAccount_Click);
            // 
            // timer
            // 
            this.timer.Interval = 1000;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // lbl_SuccessCount
            // 
            this.lbl_SuccessCount.AutoSize = true;
            this.lbl_SuccessCount.Location = new System.Drawing.Point(150, 49);
            this.lbl_SuccessCount.Name = "lbl_SuccessCount";
            this.lbl_SuccessCount.Size = new System.Drawing.Size(83, 12);
            this.lbl_SuccessCount.TabIndex = 1;
            this.lbl_SuccessCount.Text = "匹配成功数：0";
            // 
            // btn_Connecting
            // 
            this.btn_Connecting.Location = new System.Drawing.Point(506, 36);
            this.btn_Connecting.Name = "btn_Connecting";
            this.btn_Connecting.Size = new System.Drawing.Size(92, 38);
            this.btn_Connecting.TabIndex = 2;
            this.btn_Connecting.Text = "连接服务器";
            this.btn_Connecting.UseVisualStyleBackColor = true;
            this.btn_Connecting.Click += new System.EventHandler(this.btn_Connecting_Click);
            // 
            // SearchResumeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(724, 573);
            this.Controls.Add(this.btn_Connecting);
            this.Controls.Add(this.btn_CreateSubAccount);
            this.Controls.Add(this.btn_Start);
            this.Controls.Add(this.lbl_SuccessCount);
            this.Controls.Add(this.lbl_SearchCount);
            this.Controls.Add(this.webBrowser);
            this.Name = "SearchResumeForm";
            this.Text = "SearchResumeForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SearchResumeForm_FormClosing);
            this.Load += new System.EventHandler(this.SearchResumeForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowser;
        private System.Windows.Forms.Label lbl_SearchCount;
        private System.Windows.Forms.Button btn_Start;
        private System.Windows.Forms.Button btn_CreateSubAccount;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Label lbl_SuccessCount;
        private System.Windows.Forms.Button btn_Connecting;
    }
}