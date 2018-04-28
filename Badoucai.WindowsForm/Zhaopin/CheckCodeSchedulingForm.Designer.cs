namespace Badoucai.WindowsForm.Zhaopin
{
    partial class CheckCodeSchedulingForm
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
            this.lbl_WaitCount = new System.Windows.Forms.Label();
            this.lbl_ProcessingCount = new System.Windows.Forms.Label();
            this.lbl_OnlineCount = new System.Windows.Forms.Label();
            this.btn_Start = new System.Windows.Forms.Button();
            this.tir_Refrence = new System.Windows.Forms.Timer(this.components);
            this.btn_Stop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lbl_WaitCount
            // 
            this.lbl_WaitCount.AutoSize = true;
            this.lbl_WaitCount.Location = new System.Drawing.Point(64, 36);
            this.lbl_WaitCount.Name = "lbl_WaitCount";
            this.lbl_WaitCount.Size = new System.Drawing.Size(59, 12);
            this.lbl_WaitCount.TabIndex = 0;
            this.lbl_WaitCount.Text = "待处理：0";
            // 
            // lbl_ProcessingCount
            // 
            this.lbl_ProcessingCount.AutoSize = true;
            this.lbl_ProcessingCount.Location = new System.Drawing.Point(64, 64);
            this.lbl_ProcessingCount.Name = "lbl_ProcessingCount";
            this.lbl_ProcessingCount.Size = new System.Drawing.Size(59, 12);
            this.lbl_ProcessingCount.TabIndex = 0;
            this.lbl_ProcessingCount.Text = "处理中：0";
            // 
            // lbl_OnlineCount
            // 
            this.lbl_OnlineCount.AutoSize = true;
            this.lbl_OnlineCount.Location = new System.Drawing.Point(64, 93);
            this.lbl_OnlineCount.Name = "lbl_OnlineCount";
            this.lbl_OnlineCount.Size = new System.Drawing.Size(107, 12);
            this.lbl_OnlineCount.TabIndex = 0;
            this.lbl_OnlineCount.Text = "清洗程序在线数：0";
            // 
            // btn_Start
            // 
            this.btn_Start.Location = new System.Drawing.Point(230, 36);
            this.btn_Start.Name = "btn_Start";
            this.btn_Start.Size = new System.Drawing.Size(66, 69);
            this.btn_Start.TabIndex = 1;
            this.btn_Start.Text = "启动";
            this.btn_Start.UseVisualStyleBackColor = true;
            this.btn_Start.Click += new System.EventHandler(this.btn_Start_Click);
            // 
            // tir_Refrence
            // 
            this.tir_Refrence.Interval = 3000;
            this.tir_Refrence.Tick += new System.EventHandler(this.tir_Refrence_Tick);
            // 
            // btn_Stop
            // 
            this.btn_Stop.Location = new System.Drawing.Point(321, 36);
            this.btn_Stop.Name = "btn_Stop";
            this.btn_Stop.Size = new System.Drawing.Size(66, 69);
            this.btn_Stop.TabIndex = 1;
            this.btn_Stop.Text = "暂停清洗";
            this.btn_Stop.UseVisualStyleBackColor = true;
            this.btn_Stop.Click += new System.EventHandler(this.btn_Stop_Click);
            // 
            // CheckCodeSchedulingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(417, 136);
            this.Controls.Add(this.btn_Stop);
            this.Controls.Add(this.btn_Start);
            this.Controls.Add(this.lbl_OnlineCount);
            this.Controls.Add(this.lbl_ProcessingCount);
            this.Controls.Add(this.lbl_WaitCount);
            this.Name = "CheckCodeSchedulingForm";
            this.Text = "CheckCodeSchedulingForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CheckCodeSchedulingForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbl_WaitCount;
        private System.Windows.Forms.Label lbl_ProcessingCount;
        private System.Windows.Forms.Label lbl_OnlineCount;
        private System.Windows.Forms.Button btn_Start;
        private System.Windows.Forms.Timer tir_Refrence;
        private System.Windows.Forms.Button btn_Stop;
    }
}