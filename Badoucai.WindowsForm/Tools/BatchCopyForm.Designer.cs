namespace Badoucai.WindowsForm.Tools
{
    partial class BatchCopyForm
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
            this.btn_Start = new System.Windows.Forms.Button();
            this.tbx_FromFolderPath = new System.Windows.Forms.TextBox();
            this.tbx_Logger = new System.Windows.Forms.TextBox();
            this.tbx_CopyNumber = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btn_Start
            // 
            this.btn_Start.Location = new System.Drawing.Point(194, 12);
            this.btn_Start.Name = "btn_Start";
            this.btn_Start.Size = new System.Drawing.Size(75, 23);
            this.btn_Start.TabIndex = 0;
            this.btn_Start.Text = "开始拷贝";
            this.btn_Start.UseVisualStyleBackColor = true;
            this.btn_Start.Click += new System.EventHandler(this.btn_Start_Click);
            // 
            // tbx_FromFolderPath
            // 
            this.tbx_FromFolderPath.Location = new System.Drawing.Point(12, 14);
            this.tbx_FromFolderPath.Name = "tbx_FromFolderPath";
            this.tbx_FromFolderPath.Size = new System.Drawing.Size(135, 21);
            this.tbx_FromFolderPath.TabIndex = 1;
            // 
            // tbx_Logger
            // 
            this.tbx_Logger.Location = new System.Drawing.Point(12, 41);
            this.tbx_Logger.Multiline = true;
            this.tbx_Logger.Name = "tbx_Logger";
            this.tbx_Logger.Size = new System.Drawing.Size(257, 145);
            this.tbx_Logger.TabIndex = 1;
            // 
            // tbx_CopyNumber
            // 
            this.tbx_CopyNumber.Location = new System.Drawing.Point(153, 14);
            this.tbx_CopyNumber.Name = "tbx_CopyNumber";
            this.tbx_CopyNumber.Size = new System.Drawing.Size(35, 21);
            this.tbx_CopyNumber.TabIndex = 1;
            this.tbx_CopyNumber.Text = "1";
            // 
            // BatchCopyForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(281, 198);
            this.Controls.Add(this.tbx_Logger);
            this.Controls.Add(this.tbx_CopyNumber);
            this.Controls.Add(this.tbx_FromFolderPath);
            this.Controls.Add(this.btn_Start);
            this.Name = "BatchCopyForm";
            this.Text = "BatchCopyForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_Start;
        private System.Windows.Forms.TextBox tbx_FromFolderPath;
        private System.Windows.Forms.TextBox tbx_Logger;
        private System.Windows.Forms.TextBox tbx_CopyNumber;
    }
}