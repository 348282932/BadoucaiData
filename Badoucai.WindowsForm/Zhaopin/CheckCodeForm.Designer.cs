namespace Badoucai.WindowsForm.Zhaopin
{
    partial class CheckCodeForm
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
            this.btn_ReferenceCheckCode = new System.Windows.Forms.Button();
            this.pic_Body = new System.Windows.Forms.PictureBox();
            this.pic_Header = new System.Windows.Forms.PictureBox();
            this.btn_Checking = new System.Windows.Forms.Button();
            this.btn_Connecting = new System.Windows.Forms.Button();
            this.lbl_WaitCount = new System.Windows.Forms.Label();
            this.lbl_Tip = new System.Windows.Forms.Label();
            this.btn_GetCheckCode = new System.Windows.Forms.Button();
            this.lbl_timer = new System.Windows.Forms.Label();
            this.tab_Rank = new System.Windows.Forms.TabControl();
            this.tabPage_Today = new System.Windows.Forms.TabPage();
            this.tabPage_Total = new System.Windows.Forms.TabPage();
            this.lbx_TodayRank = new System.Windows.Forms.ListBox();
            this.lbx_TotalRank = new System.Windows.Forms.ListBox();
            this.lbl_HandleUser = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pic_Body)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_Header)).BeginInit();
            this.tab_Rank.SuspendLayout();
            this.tabPage_Today.SuspendLayout();
            this.tabPage_Total.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_ReferenceCheckCode
            // 
            this.btn_ReferenceCheckCode.Location = new System.Drawing.Point(317, 123);
            this.btn_ReferenceCheckCode.Name = "btn_ReferenceCheckCode";
            this.btn_ReferenceCheckCode.Size = new System.Drawing.Size(100, 40);
            this.btn_ReferenceCheckCode.TabIndex = 2;
            this.btn_ReferenceCheckCode.Text = "刷新";
            this.btn_ReferenceCheckCode.UseVisualStyleBackColor = true;
            this.btn_ReferenceCheckCode.Click += new System.EventHandler(this.btn_ReferenceCheckCode_Click);
            // 
            // pic_Body
            // 
            this.pic_Body.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pic_Body.Location = new System.Drawing.Point(27, 33);
            this.pic_Body.Name = "pic_Body";
            this.pic_Body.Size = new System.Drawing.Size(280, 130);
            this.pic_Body.TabIndex = 3;
            this.pic_Body.TabStop = false;
            this.pic_Body.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pic_Body_MouseClick);
            // 
            // pic_Header
            // 
            this.pic_Header.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pic_Header.Location = new System.Drawing.Point(317, 33);
            this.pic_Header.Name = "pic_Header";
            this.pic_Header.Size = new System.Drawing.Size(100, 40);
            this.pic_Header.TabIndex = 3;
            this.pic_Header.TabStop = false;
            // 
            // btn_Checking
            // 
            this.btn_Checking.BackColor = System.Drawing.Color.Khaki;
            this.btn_Checking.Location = new System.Drawing.Point(423, 79);
            this.btn_Checking.Name = "btn_Checking";
            this.btn_Checking.Size = new System.Drawing.Size(100, 84);
            this.btn_Checking.TabIndex = 2;
            this.btn_Checking.Text = "验证";
            this.btn_Checking.UseVisualStyleBackColor = false;
            this.btn_Checking.Click += new System.EventHandler(this.btn_Checking_Click);
            // 
            // btn_Connecting
            // 
            this.btn_Connecting.Location = new System.Drawing.Point(423, 33);
            this.btn_Connecting.Name = "btn_Connecting";
            this.btn_Connecting.Size = new System.Drawing.Size(100, 40);
            this.btn_Connecting.TabIndex = 2;
            this.btn_Connecting.Text = "连接服务器";
            this.btn_Connecting.UseVisualStyleBackColor = true;
            this.btn_Connecting.Click += new System.EventHandler(this.btn_Connecting_Click);
            // 
            // lbl_WaitCount
            // 
            this.lbl_WaitCount.AutoSize = true;
            this.lbl_WaitCount.Location = new System.Drawing.Point(25, 177);
            this.lbl_WaitCount.Name = "lbl_WaitCount";
            this.lbl_WaitCount.Size = new System.Drawing.Size(53, 12);
            this.lbl_WaitCount.TabIndex = 1;
            this.lbl_WaitCount.Text = "待处理：";
            // 
            // lbl_Tip
            // 
            this.lbl_Tip.AutoSize = true;
            this.lbl_Tip.Location = new System.Drawing.Point(125, 177);
            this.lbl_Tip.Name = "lbl_Tip";
            this.lbl_Tip.Size = new System.Drawing.Size(41, 12);
            this.lbl_Tip.TabIndex = 1;
            this.lbl_Tip.Text = "提示：";
            // 
            // btn_GetCheckCode
            // 
            this.btn_GetCheckCode.Location = new System.Drawing.Point(317, 79);
            this.btn_GetCheckCode.Name = "btn_GetCheckCode";
            this.btn_GetCheckCode.Size = new System.Drawing.Size(100, 38);
            this.btn_GetCheckCode.TabIndex = 2;
            this.btn_GetCheckCode.Text = "获取";
            this.btn_GetCheckCode.UseVisualStyleBackColor = true;
            this.btn_GetCheckCode.Click += new System.EventHandler(this.btn_GetCheckCode_Click);
            // 
            // lbl_timer
            // 
            this.lbl_timer.AutoSize = true;
            this.lbl_timer.Location = new System.Drawing.Point(447, 178);
            this.lbl_timer.Name = "lbl_timer";
            this.lbl_timer.Size = new System.Drawing.Size(53, 12);
            this.lbl_timer.TabIndex = 1;
            this.lbl_timer.Text = "倒计时：";
            // 
            // tab_Rank
            // 
            this.tab_Rank.Controls.Add(this.tabPage_Today);
            this.tab_Rank.Controls.Add(this.tabPage_Total);
            this.tab_Rank.Location = new System.Drawing.Point(529, 33);
            this.tab_Rank.Name = "tab_Rank";
            this.tab_Rank.SelectedIndex = 0;
            this.tab_Rank.Size = new System.Drawing.Size(149, 130);
            this.tab_Rank.TabIndex = 4;
            // 
            // tabPage_Today
            // 
            this.tabPage_Today.Controls.Add(this.lbx_TodayRank);
            this.tabPage_Today.Location = new System.Drawing.Point(4, 22);
            this.tabPage_Today.Name = "tabPage_Today";
            this.tabPage_Today.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_Today.Size = new System.Drawing.Size(141, 104);
            this.tabPage_Today.TabIndex = 0;
            this.tabPage_Today.Text = "日排行";
            this.tabPage_Today.UseVisualStyleBackColor = true;
            // 
            // tabPage_Total
            // 
            this.tabPage_Total.Controls.Add(this.lbx_TotalRank);
            this.tabPage_Total.Location = new System.Drawing.Point(4, 22);
            this.tabPage_Total.Name = "tabPage_Total";
            this.tabPage_Total.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_Total.Size = new System.Drawing.Size(141, 104);
            this.tabPage_Total.TabIndex = 1;
            this.tabPage_Total.Text = "总排行";
            this.tabPage_Total.UseVisualStyleBackColor = true;
            // 
            // lbx_TodayRank
            // 
            this.lbx_TodayRank.FormattingEnabled = true;
            this.lbx_TodayRank.ItemHeight = 12;
            this.lbx_TodayRank.Location = new System.Drawing.Point(0, 1);
            this.lbx_TodayRank.Name = "lbx_TodayRank";
            this.lbx_TodayRank.Size = new System.Drawing.Size(141, 100);
            this.lbx_TodayRank.TabIndex = 0;
            // 
            // lbx_TotalRank
            // 
            this.lbx_TotalRank.FormattingEnabled = true;
            this.lbx_TotalRank.ItemHeight = 12;
            this.lbx_TotalRank.Location = new System.Drawing.Point(0, 1);
            this.lbx_TotalRank.Name = "lbx_TotalRank";
            this.lbx_TotalRank.Size = new System.Drawing.Size(141, 100);
            this.lbx_TotalRank.TabIndex = 0;
            // 
            // lbl_HandleUser
            // 
            this.lbl_HandleUser.AutoSize = true;
            this.lbl_HandleUser.Location = new System.Drawing.Point(25, 9);
            this.lbl_HandleUser.Name = "lbl_HandleUser";
            this.lbl_HandleUser.Size = new System.Drawing.Size(65, 12);
            this.lbl_HandleUser.TabIndex = 1;
            this.lbl_HandleUser.Text = "当前用户：";
            // 
            // CheckCodeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(697, 204);
            this.Controls.Add(this.tab_Rank);
            this.Controls.Add(this.pic_Header);
            this.Controls.Add(this.pic_Body);
            this.Controls.Add(this.btn_Connecting);
            this.Controls.Add(this.btn_Checking);
            this.Controls.Add(this.btn_GetCheckCode);
            this.Controls.Add(this.btn_ReferenceCheckCode);
            this.Controls.Add(this.lbl_timer);
            this.Controls.Add(this.lbl_Tip);
            this.Controls.Add(this.lbl_HandleUser);
            this.Controls.Add(this.lbl_WaitCount);
            this.Name = "CheckCodeForm";
            this.Text = "验证码打码程序";
            this.Load += new System.EventHandler(this.CheckCodeForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pic_Body)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pic_Header)).EndInit();
            this.tab_Rank.ResumeLayout(false);
            this.tabPage_Today.ResumeLayout(false);
            this.tabPage_Total.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_ReferenceCheckCode;
        private System.Windows.Forms.PictureBox pic_Body;
        private System.Windows.Forms.PictureBox pic_Header;
        private System.Windows.Forms.Button btn_Checking;
        private System.Windows.Forms.Button btn_Connecting;
        private System.Windows.Forms.Label lbl_WaitCount;
        private System.Windows.Forms.Label lbl_Tip;
        private System.Windows.Forms.Button btn_GetCheckCode;
        private System.Windows.Forms.Label lbl_timer;
        private System.Windows.Forms.TabControl tab_Rank;
        private System.Windows.Forms.TabPage tabPage_Today;
        private System.Windows.Forms.TabPage tabPage_Total;
        private System.Windows.Forms.ListBox lbx_TodayRank;
        private System.Windows.Forms.ListBox lbx_TotalRank;
        private System.Windows.Forms.Label lbl_HandleUser;
    }
}