namespace Badoucai.WindowsForm.Tools
{
    partial class ProgramCollectionForm
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
            this.btn_DownloadNewZPResume = new System.Windows.Forms.Button();
            this.btn_Statistics = new System.Windows.Forms.Button();
            this.btn_zhaopin_Resume = new System.Windows.Forms.Button();
            this.btn_Export = new System.Windows.Forms.Button();
            this.btn_SearchOldResume = new System.Windows.Forms.Button();
            this.btn_ImprotOldResume = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbx_Log
            // 
            this.tbx_Log.Location = new System.Drawing.Point(12, 126);
            this.tbx_Log.Multiline = true;
            this.tbx_Log.Name = "tbx_Log";
            this.tbx_Log.Size = new System.Drawing.Size(436, 172);
            this.tbx_Log.TabIndex = 0;
            // 
            // btn_DownloadNewZPResume
            // 
            this.btn_DownloadNewZPResume.Location = new System.Drawing.Point(29, 23);
            this.btn_DownloadNewZPResume.Name = "btn_DownloadNewZPResume";
            this.btn_DownloadNewZPResume.Size = new System.Drawing.Size(109, 33);
            this.btn_DownloadNewZPResume.TabIndex = 1;
            this.btn_DownloadNewZPResume.Text = "新智联简历下载";
            this.btn_DownloadNewZPResume.UseVisualStyleBackColor = true;
            this.btn_DownloadNewZPResume.Click += new System.EventHandler(this.btn_DownloadNewZPResume_Click);
            // 
            // btn_Statistics
            // 
            this.btn_Statistics.Location = new System.Drawing.Point(176, 23);
            this.btn_Statistics.Name = "btn_Statistics";
            this.btn_Statistics.Size = new System.Drawing.Size(109, 33);
            this.btn_Statistics.TabIndex = 1;
            this.btn_Statistics.Text = "统计报表";
            this.btn_Statistics.UseVisualStyleBackColor = true;
            this.btn_Statistics.Click += new System.EventHandler(this.btn_Statistics_Click);
            // 
            // btn_zhaopin_Resume
            // 
            this.btn_zhaopin_Resume.Location = new System.Drawing.Point(29, 76);
            this.btn_zhaopin_Resume.Name = "btn_zhaopin_Resume";
            this.btn_zhaopin_Resume.Size = new System.Drawing.Size(109, 33);
            this.btn_zhaopin_Resume.TabIndex = 1;
            this.btn_zhaopin_Resume.Text = "统计智联简历";
            this.btn_zhaopin_Resume.UseVisualStyleBackColor = true;
            this.btn_zhaopin_Resume.Click += new System.EventHandler(this.btn_zhaopin_Resume_Click);
            // 
            // btn_Export
            // 
            this.btn_Export.Location = new System.Drawing.Point(176, 76);
            this.btn_Export.Name = "btn_Export";
            this.btn_Export.Size = new System.Drawing.Size(109, 33);
            this.btn_Export.TabIndex = 1;
            this.btn_Export.Text = "导出电信移动";
            this.btn_Export.UseVisualStyleBackColor = true;
            this.btn_Export.Click += new System.EventHandler(this.btn_Export_Click);
            // 
            // btn_SearchOldResume
            // 
            this.btn_SearchOldResume.Location = new System.Drawing.Point(315, 23);
            this.btn_SearchOldResume.Name = "btn_SearchOldResume";
            this.btn_SearchOldResume.Size = new System.Drawing.Size(109, 33);
            this.btn_SearchOldResume.TabIndex = 1;
            this.btn_SearchOldResume.Text = "扫描旧库电信联通";
            this.btn_SearchOldResume.UseVisualStyleBackColor = true;
            this.btn_SearchOldResume.Click += new System.EventHandler(this.btn_SearchOldResume_Click);
            // 
            // btn_ImprotOldResume
            // 
            this.btn_ImprotOldResume.Location = new System.Drawing.Point(315, 76);
            this.btn_ImprotOldResume.Name = "btn_ImprotOldResume";
            this.btn_ImprotOldResume.Size = new System.Drawing.Size(109, 33);
            this.btn_ImprotOldResume.TabIndex = 1;
            this.btn_ImprotOldResume.Text = "导入旧库简历";
            this.btn_ImprotOldResume.UseVisualStyleBackColor = true;
            this.btn_ImprotOldResume.Click += new System.EventHandler(this.btn_ImprotOldResume_Click);
            // 
            // ProgramCollectionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(460, 311);
            this.Controls.Add(this.btn_Statistics);
            this.Controls.Add(this.btn_SearchOldResume);
            this.Controls.Add(this.btn_ImprotOldResume);
            this.Controls.Add(this.btn_Export);
            this.Controls.Add(this.btn_zhaopin_Resume);
            this.Controls.Add(this.btn_DownloadNewZPResume);
            this.Controls.Add(this.tbx_Log);
            this.Name = "ProgramCollectionForm";
            this.Text = "程序集合";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbx_Log;
        private System.Windows.Forms.Button btn_DownloadNewZPResume;
        private System.Windows.Forms.Button btn_Statistics;
        private System.Windows.Forms.Button btn_zhaopin_Resume;
        private System.Windows.Forms.Button btn_Export;
        private System.Windows.Forms.Button btn_SearchOldResume;
        private System.Windows.Forms.Button btn_ImprotOldResume;
    }
}