namespace Badoucai.WindowsForm.Tools
{
    partial class SpiderDodiForm
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
            this.btn_Download = new System.Windows.Forms.Button();
            this.tbx_Log = new System.Windows.Forms.TextBox();
            this.btn_Warehousing = new System.Windows.Forms.Button();
            this.cbx_UpdateBySZ = new System.Windows.Forms.CheckBox();
            this.btn_SpiderDetail = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btn_Download
            // 
            this.btn_Download.Location = new System.Drawing.Point(336, 29);
            this.btn_Download.Name = "btn_Download";
            this.btn_Download.Size = new System.Drawing.Size(160, 55);
            this.btn_Download.TabIndex = 0;
            this.btn_Download.Text = "开始下载";
            this.btn_Download.UseVisualStyleBackColor = true;
            this.btn_Download.Click += new System.EventHandler(this.btn_Download_Click);
            // 
            // tbx_Log
            // 
            this.tbx_Log.Location = new System.Drawing.Point(26, 29);
            this.tbx_Log.Multiline = true;
            this.tbx_Log.Name = "tbx_Log";
            this.tbx_Log.Size = new System.Drawing.Size(283, 174);
            this.tbx_Log.TabIndex = 1;
            // 
            // btn_Warehousing
            // 
            this.btn_Warehousing.Location = new System.Drawing.Point(336, 102);
            this.btn_Warehousing.Name = "btn_Warehousing";
            this.btn_Warehousing.Size = new System.Drawing.Size(160, 55);
            this.btn_Warehousing.TabIndex = 0;
            this.btn_Warehousing.Text = "开始入库";
            this.btn_Warehousing.UseVisualStyleBackColor = true;
            this.btn_Warehousing.Click += new System.EventHandler(this.btn_Warehousing_Click);
            // 
            // cbx_UpdateBySZ
            // 
            this.cbx_UpdateBySZ.AutoSize = true;
            this.cbx_UpdateBySZ.Location = new System.Drawing.Point(336, 187);
            this.cbx_UpdateBySZ.Name = "cbx_UpdateBySZ";
            this.cbx_UpdateBySZ.Size = new System.Drawing.Size(72, 16);
            this.cbx_UpdateBySZ.TabIndex = 2;
            this.cbx_UpdateBySZ.Text = "更新深圳";
            this.cbx_UpdateBySZ.UseVisualStyleBackColor = true;
            // 
            // btn_SpiderDetail
            // 
            this.btn_SpiderDetail.Location = new System.Drawing.Point(511, 29);
            this.btn_SpiderDetail.Name = "btn_SpiderDetail";
            this.btn_SpiderDetail.Size = new System.Drawing.Size(160, 55);
            this.btn_SpiderDetail.TabIndex = 0;
            this.btn_SpiderDetail.Text = "提取详情";
            this.btn_SpiderDetail.UseVisualStyleBackColor = true;
            this.btn_SpiderDetail.Click += new System.EventHandler(this.btn_SpiderDetail_Click);
            // 
            // SpiderDodiForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(683, 232);
            this.Controls.Add(this.cbx_UpdateBySZ);
            this.Controls.Add(this.tbx_Log);
            this.Controls.Add(this.btn_Warehousing);
            this.Controls.Add(this.btn_SpiderDetail);
            this.Controls.Add(this.btn_Download);
            this.Name = "SpiderDodiForm";
            this.Text = "多迪下载程序";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Download;
        private System.Windows.Forms.TextBox tbx_Log;
        private System.Windows.Forms.Button btn_Warehousing;
        private System.Windows.Forms.CheckBox cbx_UpdateBySZ;
        private System.Windows.Forms.Button btn_SpiderDetail;
    }
}