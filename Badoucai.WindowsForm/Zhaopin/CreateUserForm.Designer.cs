namespace Badoucai.WindowsForm.Zhaopin
{
    partial class CreateUserForm
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
            this.lbl_Cellphone = new System.Windows.Forms.Label();
            this.tbx_Cellphone = new System.Windows.Forms.TextBox();
            this.lbl_CheckCode = new System.Windows.Forms.Label();
            this.tbx_CheckCode = new System.Windows.Forms.TextBox();
            this.btn_PushRegisterSMS = new System.Windows.Forms.Button();
            this.lbl_SMSCheckCode = new System.Windows.Forms.Label();
            this.tbx_SMSCheckCode = new System.Windows.Forms.TextBox();
            this.pic_CheckCode = new System.Windows.Forms.PictureBox();
            this.btn_Register = new System.Windows.Forms.Button();
            this.btn_PushBindSMS = new System.Windows.Forms.Button();
            this.btn_BindCellphone = new System.Windows.Forms.Button();
            this.tbx_Log = new System.Windows.Forms.TextBox();
            this.webBrowser = new System.Windows.Forms.WebBrowser();
            ((System.ComponentModel.ISupportInitialize)(this.pic_CheckCode)).BeginInit();
            this.SuspendLayout();
            // 
            // lbl_Cellphone
            // 
            this.lbl_Cellphone.AutoSize = true;
            this.lbl_Cellphone.Location = new System.Drawing.Point(62, 48);
            this.lbl_Cellphone.Name = "lbl_Cellphone";
            this.lbl_Cellphone.Size = new System.Drawing.Size(41, 12);
            this.lbl_Cellphone.TabIndex = 0;
            this.lbl_Cellphone.Text = "手机：";
            // 
            // tbx_Cellphone
            // 
            this.tbx_Cellphone.Location = new System.Drawing.Point(109, 44);
            this.tbx_Cellphone.Name = "tbx_Cellphone";
            this.tbx_Cellphone.Size = new System.Drawing.Size(115, 21);
            this.tbx_Cellphone.TabIndex = 1;
            this.tbx_Cellphone.Text = "18320762774";
            // 
            // lbl_CheckCode
            // 
            this.lbl_CheckCode.AutoSize = true;
            this.lbl_CheckCode.Location = new System.Drawing.Point(26, 90);
            this.lbl_CheckCode.Name = "lbl_CheckCode";
            this.lbl_CheckCode.Size = new System.Drawing.Size(77, 12);
            this.lbl_CheckCode.TabIndex = 0;
            this.lbl_CheckCode.Text = "图形验证码：";
            // 
            // tbx_CheckCode
            // 
            this.tbx_CheckCode.Location = new System.Drawing.Point(109, 83);
            this.tbx_CheckCode.Name = "tbx_CheckCode";
            this.tbx_CheckCode.Size = new System.Drawing.Size(115, 21);
            this.tbx_CheckCode.TabIndex = 1;
            // 
            // btn_PushRegisterSMS
            // 
            this.btn_PushRegisterSMS.Location = new System.Drawing.Point(250, 44);
            this.btn_PushRegisterSMS.Name = "btn_PushRegisterSMS";
            this.btn_PushRegisterSMS.Size = new System.Drawing.Size(66, 58);
            this.btn_PushRegisterSMS.TabIndex = 2;
            this.btn_PushRegisterSMS.Text = "加载注册页面";
            this.btn_PushRegisterSMS.UseVisualStyleBackColor = true;
            this.btn_PushRegisterSMS.Click += new System.EventHandler(this.btn_PushRegisterSMS_Click);
            // 
            // lbl_SMSCheckCode
            // 
            this.lbl_SMSCheckCode.AutoSize = true;
            this.lbl_SMSCheckCode.Location = new System.Drawing.Point(26, 191);
            this.lbl_SMSCheckCode.Name = "lbl_SMSCheckCode";
            this.lbl_SMSCheckCode.Size = new System.Drawing.Size(77, 12);
            this.lbl_SMSCheckCode.TabIndex = 0;
            this.lbl_SMSCheckCode.Text = "短信验证码：";
            // 
            // tbx_SMSCheckCode
            // 
            this.tbx_SMSCheckCode.Location = new System.Drawing.Point(109, 188);
            this.tbx_SMSCheckCode.Name = "tbx_SMSCheckCode";
            this.tbx_SMSCheckCode.Size = new System.Drawing.Size(115, 21);
            this.tbx_SMSCheckCode.TabIndex = 1;
            // 
            // pic_CheckCode
            // 
            this.pic_CheckCode.Location = new System.Drawing.Point(109, 120);
            this.pic_CheckCode.Name = "pic_CheckCode";
            this.pic_CheckCode.Size = new System.Drawing.Size(115, 53);
            this.pic_CheckCode.TabIndex = 3;
            this.pic_CheckCode.TabStop = false;
            // 
            // btn_Register
            // 
            this.btn_Register.Location = new System.Drawing.Point(250, 120);
            this.btn_Register.Name = "btn_Register";
            this.btn_Register.Size = new System.Drawing.Size(66, 89);
            this.btn_Register.TabIndex = 2;
            this.btn_Register.Text = "初始化简历";
            this.btn_Register.UseVisualStyleBackColor = true;
            this.btn_Register.Click += new System.EventHandler(this.btn_Register_Click);
            // 
            // btn_PushBindSMS
            // 
            this.btn_PushBindSMS.Location = new System.Drawing.Point(335, 44);
            this.btn_PushBindSMS.Name = "btn_PushBindSMS";
            this.btn_PushBindSMS.Size = new System.Drawing.Size(66, 58);
            this.btn_PushBindSMS.TabIndex = 2;
            this.btn_PushBindSMS.Text = "发送换绑短信";
            this.btn_PushBindSMS.UseVisualStyleBackColor = true;
            this.btn_PushBindSMS.Click += new System.EventHandler(this.btn_PushBindSMS_Click);
            // 
            // btn_BindCellphone
            // 
            this.btn_BindCellphone.Location = new System.Drawing.Point(335, 120);
            this.btn_BindCellphone.Name = "btn_BindCellphone";
            this.btn_BindCellphone.Size = new System.Drawing.Size(66, 89);
            this.btn_BindCellphone.TabIndex = 2;
            this.btn_BindCellphone.Text = "换绑";
            this.btn_BindCellphone.UseVisualStyleBackColor = true;
            this.btn_BindCellphone.Click += new System.EventHandler(this.btn_BindCellphone_Click);
            // 
            // tbx_Log
            // 
            this.tbx_Log.Location = new System.Drawing.Point(12, 235);
            this.tbx_Log.Multiline = true;
            this.tbx_Log.Name = "tbx_Log";
            this.tbx_Log.Size = new System.Drawing.Size(447, 466);
            this.tbx_Log.TabIndex = 4;
            // 
            // webBrowser
            // 
            this.webBrowser.Location = new System.Drawing.Point(483, 12);
            this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new System.Drawing.Size(997, 699);
            this.webBrowser.TabIndex = 5;
            // 
            // CreateUserForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1492, 713);
            this.Controls.Add(this.webBrowser);
            this.Controls.Add(this.tbx_Log);
            this.Controls.Add(this.pic_CheckCode);
            this.Controls.Add(this.btn_BindCellphone);
            this.Controls.Add(this.btn_PushBindSMS);
            this.Controls.Add(this.btn_Register);
            this.Controls.Add(this.btn_PushRegisterSMS);
            this.Controls.Add(this.tbx_SMSCheckCode);
            this.Controls.Add(this.lbl_SMSCheckCode);
            this.Controls.Add(this.tbx_CheckCode);
            this.Controls.Add(this.lbl_CheckCode);
            this.Controls.Add(this.tbx_Cellphone);
            this.Controls.Add(this.lbl_Cellphone);
            this.Name = "CreateUserForm";
            this.Text = "创建用户（智联）";
            ((System.ComponentModel.ISupportInitialize)(this.pic_CheckCode)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_Cellphone;
        private System.Windows.Forms.TextBox tbx_Cellphone;
        private System.Windows.Forms.Label lbl_CheckCode;
        private System.Windows.Forms.TextBox tbx_CheckCode;
        private System.Windows.Forms.Button btn_PushRegisterSMS;
        private System.Windows.Forms.Label lbl_SMSCheckCode;
        private System.Windows.Forms.TextBox tbx_SMSCheckCode;
        private System.Windows.Forms.PictureBox pic_CheckCode;
        private System.Windows.Forms.Button btn_Register;
        private System.Windows.Forms.Button btn_PushBindSMS;
        private System.Windows.Forms.Button btn_BindCellphone;
        private System.Windows.Forms.TextBox tbx_Log;
        private System.Windows.Forms.WebBrowser webBrowser;
    }
}