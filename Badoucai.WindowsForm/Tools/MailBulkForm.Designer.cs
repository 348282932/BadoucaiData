namespace Badoucai.WindowsForm.Tools
{
    partial class MailBulkForm
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
            this.lbl_Recipient = new System.Windows.Forms.Label();
            this.tbx_Recipient = new System.Windows.Forms.TextBox();
            this.lbl_Subject = new System.Windows.Forms.Label();
            this.tbx_Subject = new System.Windows.Forms.TextBox();
            this.lbl_Body = new System.Windows.Forms.Label();
            this.tbx_Body = new System.Windows.Forms.TextBox();
            this.btn_Send = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.btn_AddAnnex = new System.Windows.Forms.Button();
            this.lbl_Annex = new System.Windows.Forms.Label();
            this.tbx_Schedule = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lbl_Recipient
            // 
            this.lbl_Recipient.AutoSize = true;
            this.lbl_Recipient.Location = new System.Drawing.Point(12, 37);
            this.lbl_Recipient.Name = "lbl_Recipient";
            this.lbl_Recipient.Size = new System.Drawing.Size(53, 12);
            this.lbl_Recipient.TabIndex = 0;
            this.lbl_Recipient.Text = "收件人：";
            // 
            // tbx_Recipient
            // 
            this.tbx_Recipient.Location = new System.Drawing.Point(71, 34);
            this.tbx_Recipient.Name = "tbx_Recipient";
            this.tbx_Recipient.Size = new System.Drawing.Size(448, 21);
            this.tbx_Recipient.TabIndex = 1;
            // 
            // lbl_Subject
            // 
            this.lbl_Subject.AutoSize = true;
            this.lbl_Subject.Location = new System.Drawing.Point(24, 80);
            this.lbl_Subject.Name = "lbl_Subject";
            this.lbl_Subject.Size = new System.Drawing.Size(41, 12);
            this.lbl_Subject.TabIndex = 0;
            this.lbl_Subject.Text = "主题：";
            // 
            // tbx_Subject
            // 
            this.tbx_Subject.Location = new System.Drawing.Point(71, 77);
            this.tbx_Subject.Name = "tbx_Subject";
            this.tbx_Subject.Size = new System.Drawing.Size(448, 21);
            this.tbx_Subject.TabIndex = 1;
            // 
            // lbl_Body
            // 
            this.lbl_Body.AutoSize = true;
            this.lbl_Body.Location = new System.Drawing.Point(24, 128);
            this.lbl_Body.Name = "lbl_Body";
            this.lbl_Body.Size = new System.Drawing.Size(41, 12);
            this.lbl_Body.TabIndex = 0;
            this.lbl_Body.Text = "正文：";
            // 
            // tbx_Body
            // 
            this.tbx_Body.Location = new System.Drawing.Point(71, 125);
            this.tbx_Body.Multiline = true;
            this.tbx_Body.Name = "tbx_Body";
            this.tbx_Body.Size = new System.Drawing.Size(448, 195);
            this.tbx_Body.TabIndex = 1;
            // 
            // btn_Send
            // 
            this.btn_Send.Location = new System.Drawing.Point(422, 432);
            this.btn_Send.Name = "btn_Send";
            this.btn_Send.Size = new System.Drawing.Size(97, 49);
            this.btn_Send.TabIndex = 2;
            this.btn_Send.Text = "发送";
            this.btn_Send.UseVisualStyleBackColor = true;
            this.btn_Send.Click += new System.EventHandler(this.btn_Send_Click);
            // 
            // btn_AddAnnex
            // 
            this.btn_AddAnnex.Location = new System.Drawing.Point(71, 336);
            this.btn_AddAnnex.Name = "btn_AddAnnex";
            this.btn_AddAnnex.Size = new System.Drawing.Size(85, 24);
            this.btn_AddAnnex.TabIndex = 2;
            this.btn_AddAnnex.Text = "添加附件";
            this.btn_AddAnnex.UseVisualStyleBackColor = true;
            this.btn_AddAnnex.Click += new System.EventHandler(this.btn_AddAnnex_Click);
            // 
            // lbl_Annex
            // 
            this.lbl_Annex.AutoSize = true;
            this.lbl_Annex.Location = new System.Drawing.Point(180, 342);
            this.lbl_Annex.Name = "lbl_Annex";
            this.lbl_Annex.Size = new System.Drawing.Size(83, 12);
            this.lbl_Annex.TabIndex = 0;
            this.lbl_Annex.Text = "未选择附件...";
            // 
            // tbx_Schedule
            // 
            this.tbx_Schedule.Location = new System.Drawing.Point(73, 379);
            this.tbx_Schedule.Multiline = true;
            this.tbx_Schedule.Name = "tbx_Schedule";
            this.tbx_Schedule.Size = new System.Drawing.Size(311, 102);
            this.tbx_Schedule.TabIndex = 4;
            // 
            // MailBulkForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(567, 507);
            this.Controls.Add(this.tbx_Schedule);
            this.Controls.Add(this.btn_AddAnnex);
            this.Controls.Add(this.btn_Send);
            this.Controls.Add(this.tbx_Body);
            this.Controls.Add(this.tbx_Subject);
            this.Controls.Add(this.tbx_Recipient);
            this.Controls.Add(this.lbl_Annex);
            this.Controls.Add(this.lbl_Body);
            this.Controls.Add(this.lbl_Subject);
            this.Controls.Add(this.lbl_Recipient);
            this.Name = "MailBulkForm";
            this.Text = "MailBulkForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_Recipient;
        private System.Windows.Forms.TextBox tbx_Recipient;
        private System.Windows.Forms.Label lbl_Subject;
        private System.Windows.Forms.TextBox tbx_Subject;
        private System.Windows.Forms.Label lbl_Body;
        private System.Windows.Forms.TextBox tbx_Body;
        private System.Windows.Forms.Button btn_Send;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Button btn_AddAnnex;
        private System.Windows.Forms.Label lbl_Annex;
        private System.Windows.Forms.TextBox tbx_Schedule;
    }
}