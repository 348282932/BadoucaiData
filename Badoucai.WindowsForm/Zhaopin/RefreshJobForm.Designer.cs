namespace Badoucai.WindowsForm.Zhaopin
{
    partial class RefreshJobForm
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
            this.btn_Down = new System.Windows.Forms.Button();
            this.tbx_Cookie = new System.Windows.Forms.TextBox();
            this.tbx_Log = new System.Windows.Forms.TextBox();
            this.lbl_Cookie = new System.Windows.Forms.Label();
            this.btn_Up = new System.Windows.Forms.Button();
            this.lbl_CompanyName = new System.Windows.Forms.Label();
            this.btn_GetCookie = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Down
            // 
            this.btn_Down.Location = new System.Drawing.Point(397, 12);
            this.btn_Down.Name = "btn_Down";
            this.btn_Down.Size = new System.Drawing.Size(61, 23);
            this.btn_Down.TabIndex = 0;
            this.btn_Down.Text = "下线";
            this.btn_Down.UseVisualStyleBackColor = true;
            this.btn_Down.Click += new System.EventHandler(this.btn_Down_Click);
            // 
            // tbx_Cookie
            // 
            this.tbx_Cookie.Location = new System.Drawing.Point(72, 14);
            this.tbx_Cookie.Name = "tbx_Cookie";
            this.tbx_Cookie.Size = new System.Drawing.Size(250, 21);
            this.tbx_Cookie.TabIndex = 1;
            // 
            // tbx_Log
            // 
            this.tbx_Log.Location = new System.Drawing.Point(12, 52);
            this.tbx_Log.Multiline = true;
            this.tbx_Log.Name = "tbx_Log";
            this.tbx_Log.Size = new System.Drawing.Size(512, 200);
            this.tbx_Log.TabIndex = 1;
            // 
            // lbl_Cookie
            // 
            this.lbl_Cookie.AutoSize = true;
            this.lbl_Cookie.Location = new System.Drawing.Point(13, 19);
            this.lbl_Cookie.Name = "lbl_Cookie";
            this.lbl_Cookie.Size = new System.Drawing.Size(53, 12);
            this.lbl_Cookie.TabIndex = 2;
            this.lbl_Cookie.Text = "Cookie：";
            // 
            // btn_Up
            // 
            this.btn_Up.Location = new System.Drawing.Point(464, 12);
            this.btn_Up.Name = "btn_Up";
            this.btn_Up.Size = new System.Drawing.Size(60, 23);
            this.btn_Up.TabIndex = 0;
            this.btn_Up.Text = "上线";
            this.btn_Up.UseVisualStyleBackColor = true;
            this.btn_Up.Click += new System.EventHandler(this.btn_Up_Click);
            // 
            // lbl_CompanyName
            // 
            this.lbl_CompanyName.AutoSize = true;
            this.lbl_CompanyName.Location = new System.Drawing.Point(13, 263);
            this.lbl_CompanyName.Name = "lbl_CompanyName";
            this.lbl_CompanyName.Size = new System.Drawing.Size(65, 12);
            this.lbl_CompanyName.TabIndex = 3;
            this.lbl_CompanyName.Text = "当前公司：";
            // 
            // btn_GetCookie
            // 
            this.btn_GetCookie.Location = new System.Drawing.Point(330, 12);
            this.btn_GetCookie.Name = "btn_GetCookie";
            this.btn_GetCookie.Size = new System.Drawing.Size(61, 23);
            this.btn_GetCookie.TabIndex = 0;
            this.btn_GetCookie.Text = "全自动";
            this.btn_GetCookie.UseVisualStyleBackColor = true;
            this.btn_GetCookie.Click += new System.EventHandler(this.btn_GetCookie_Click);
            // 
            // RefreshJobForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(536, 284);
            this.Controls.Add(this.lbl_CompanyName);
            this.Controls.Add(this.lbl_Cookie);
            this.Controls.Add(this.tbx_Log);
            this.Controls.Add(this.tbx_Cookie);
            this.Controls.Add(this.btn_Up);
            this.Controls.Add(this.btn_GetCookie);
            this.Controls.Add(this.btn_Down);
            this.Name = "RefreshJobForm";
            this.Text = "智联职位刷新（新系统）";
            this.Load += new System.EventHandler(this.RefreshJobForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Down;
        private System.Windows.Forms.TextBox tbx_Cookie;
        private System.Windows.Forms.TextBox tbx_Log;
        private System.Windows.Forms.Label lbl_Cookie;
        private System.Windows.Forms.Button btn_Up;
        private System.Windows.Forms.Label lbl_CompanyName;
        private System.Windows.Forms.Button btn_GetCookie;
    }
}