namespace Badoucai.WindowsForm.Zhaopin
{
    partial class DownloadResumeByOldSystemForm
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
            this.lbl_Cookie = new System.Windows.Forms.Label();
            this.tbx_Cookie = new System.Windows.Forms.TextBox();
            this.btn_StartDownload = new System.Windows.Forms.Button();
            this.lbl_FileSavePath = new System.Windows.Forms.Label();
            this.tbx_FileSavePath = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // tbx_Log
            // 
            this.tbx_Log.Location = new System.Drawing.Point(12, 80);
            this.tbx_Log.Multiline = true;
            this.tbx_Log.Name = "tbx_Log";
            this.tbx_Log.Size = new System.Drawing.Size(474, 250);
            this.tbx_Log.TabIndex = 0;
            // 
            // lbl_Cookie
            // 
            this.lbl_Cookie.AutoSize = true;
            this.lbl_Cookie.Location = new System.Drawing.Point(12, 24);
            this.lbl_Cookie.Name = "lbl_Cookie";
            this.lbl_Cookie.Size = new System.Drawing.Size(53, 12);
            this.lbl_Cookie.TabIndex = 1;
            this.lbl_Cookie.Text = "Cookie：";
            // 
            // tbx_Cookie
            // 
            this.tbx_Cookie.Location = new System.Drawing.Point(71, 21);
            this.tbx_Cookie.Name = "tbx_Cookie";
            this.tbx_Cookie.Size = new System.Drawing.Size(318, 21);
            this.tbx_Cookie.TabIndex = 2;
            // 
            // btn_StartDownload
            // 
            this.btn_StartDownload.Location = new System.Drawing.Point(395, 12);
            this.btn_StartDownload.Name = "btn_StartDownload";
            this.btn_StartDownload.Size = new System.Drawing.Size(91, 62);
            this.btn_StartDownload.TabIndex = 5;
            this.btn_StartDownload.Text = "开始下载";
            this.btn_StartDownload.UseVisualStyleBackColor = true;
            this.btn_StartDownload.Click += new System.EventHandler(this.btn_StartDownload_Click);
            // 
            // lbl_FileSavePath
            // 
            this.lbl_FileSavePath.AutoSize = true;
            this.lbl_FileSavePath.Location = new System.Drawing.Point(24, 51);
            this.lbl_FileSavePath.Name = "lbl_FileSavePath";
            this.lbl_FileSavePath.Size = new System.Drawing.Size(41, 12);
            this.lbl_FileSavePath.TabIndex = 6;
            this.lbl_FileSavePath.Text = "路径：";
            // 
            // tbx_FileSavePath
            // 
            this.tbx_FileSavePath.Location = new System.Drawing.Point(71, 48);
            this.tbx_FileSavePath.Name = "tbx_FileSavePath";
            this.tbx_FileSavePath.Size = new System.Drawing.Size(318, 21);
            this.tbx_FileSavePath.TabIndex = 4;
            // 
            // DownloadResumeByOldSystemForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(498, 342);
            this.Controls.Add(this.lbl_FileSavePath);
            this.Controls.Add(this.btn_StartDownload);
            this.Controls.Add(this.tbx_FileSavePath);
            this.Controls.Add(this.tbx_Cookie);
            this.Controls.Add(this.lbl_Cookie);
            this.Controls.Add(this.tbx_Log);
            this.Name = "DownloadResumeByOldSystemForm";
            this.Text = "DownloadResumeByOldSystemForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbx_Log;
        private System.Windows.Forms.Label lbl_Cookie;
        private System.Windows.Forms.TextBox tbx_Cookie;
        private System.Windows.Forms.Button btn_StartDownload;
        private System.Windows.Forms.Label lbl_FileSavePath;
        private System.Windows.Forms.TextBox tbx_FileSavePath;
    }
}