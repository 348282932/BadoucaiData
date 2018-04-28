namespace Badoucai.WindowsForm._51Job
{
    partial class MatchLocalResumeForm
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
            this.btn_Choose = new System.Windows.Forms.Button();
            this.lbl_lable_1 = new System.Windows.Forms.Label();
            this.tbx_FilesPath = new System.Windows.Forms.TextBox();
            this.btn_StartDeCompression = new System.Windows.Forms.Button();
            this.lbl_label_2 = new System.Windows.Forms.Label();
            this.tbx_Cookie = new System.Windows.Forms.TextBox();
            this.btn_StartMatch = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // tbx_Log
            // 
            this.tbx_Log.Location = new System.Drawing.Point(12, 95);
            this.tbx_Log.Multiline = true;
            this.tbx_Log.Name = "tbx_Log";
            this.tbx_Log.Size = new System.Drawing.Size(473, 179);
            this.tbx_Log.TabIndex = 0;
            // 
            // btn_Choose
            // 
            this.btn_Choose.Location = new System.Drawing.Point(323, 23);
            this.btn_Choose.Name = "btn_Choose";
            this.btn_Choose.Size = new System.Drawing.Size(56, 23);
            this.btn_Choose.TabIndex = 1;
            this.btn_Choose.Text = "选择...";
            this.btn_Choose.UseVisualStyleBackColor = true;
            this.btn_Choose.Click += new System.EventHandler(this.btn_Choose_Click);
            // 
            // lbl_lable_1
            // 
            this.lbl_lable_1.AutoSize = true;
            this.lbl_lable_1.Location = new System.Drawing.Point(12, 28);
            this.lbl_lable_1.Name = "lbl_lable_1";
            this.lbl_lable_1.Size = new System.Drawing.Size(65, 12);
            this.lbl_lable_1.TabIndex = 2;
            this.lbl_lable_1.Text = "文件路径：";
            // 
            // tbx_FilesPath
            // 
            this.tbx_FilesPath.Location = new System.Drawing.Point(83, 23);
            this.tbx_FilesPath.Name = "tbx_FilesPath";
            this.tbx_FilesPath.Size = new System.Drawing.Size(234, 21);
            this.tbx_FilesPath.TabIndex = 3;
            // 
            // btn_StartDeCompression
            // 
            this.btn_StartDeCompression.Location = new System.Drawing.Point(385, 12);
            this.btn_StartDeCompression.Name = "btn_StartDeCompression";
            this.btn_StartDeCompression.Size = new System.Drawing.Size(100, 40);
            this.btn_StartDeCompression.TabIndex = 4;
            this.btn_StartDeCompression.Text = "开始解压";
            this.btn_StartDeCompression.UseVisualStyleBackColor = true;
            this.btn_StartDeCompression.Click += new System.EventHandler(this.btn_StartDeCompression_Click);
            // 
            // lbl_label_2
            // 
            this.lbl_label_2.AutoSize = true;
            this.lbl_label_2.Location = new System.Drawing.Point(24, 67);
            this.lbl_label_2.Name = "lbl_label_2";
            this.lbl_label_2.Size = new System.Drawing.Size(53, 12);
            this.lbl_label_2.TabIndex = 2;
            this.lbl_label_2.Text = "Cookie：";
            // 
            // tbx_Cookie
            // 
            this.tbx_Cookie.Location = new System.Drawing.Point(83, 64);
            this.tbx_Cookie.Name = "tbx_Cookie";
            this.tbx_Cookie.Size = new System.Drawing.Size(296, 21);
            this.tbx_Cookie.TabIndex = 3;
            // 
            // btn_StartMatch
            // 
            this.btn_StartMatch.Location = new System.Drawing.Point(385, 53);
            this.btn_StartMatch.Name = "btn_StartMatch";
            this.btn_StartMatch.Size = new System.Drawing.Size(100, 40);
            this.btn_StartMatch.TabIndex = 4;
            this.btn_StartMatch.Text = "开始匹配";
            this.btn_StartMatch.UseVisualStyleBackColor = true;
            this.btn_StartMatch.Click += new System.EventHandler(this.btn_StartMatch_Click);
            // 
            // MatchLocalResumeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(498, 285);
            this.Controls.Add(this.btn_StartMatch);
            this.Controls.Add(this.btn_StartDeCompression);
            this.Controls.Add(this.tbx_Cookie);
            this.Controls.Add(this.tbx_FilesPath);
            this.Controls.Add(this.lbl_label_2);
            this.Controls.Add(this.lbl_lable_1);
            this.Controls.Add(this.btn_Choose);
            this.Controls.Add(this.tbx_Log);
            this.Name = "MatchLocalResumeForm";
            this.Text = "MatchLocalResumeForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbx_Log;
        private System.Windows.Forms.Button btn_Choose;
        private System.Windows.Forms.Label lbl_lable_1;
        private System.Windows.Forms.TextBox tbx_FilesPath;
        private System.Windows.Forms.Button btn_StartDeCompression;
        private System.Windows.Forms.Label lbl_label_2;
        private System.Windows.Forms.TextBox tbx_Cookie;
        private System.Windows.Forms.Button btn_StartMatch;
    }
}