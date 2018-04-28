using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Badoucai.Library;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Badoucai.WindowsForm.Tools
{
    public partial class MailBulkForm : Form
    {
        public MailBulkForm()
        {
            InitializeComponent();
        }

        private void btn_AddAnnex_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                this.lbl_Annex.Text = openFileDialog.FileName;
            }
        }

        private void btn_Send_Click(object sender, EventArgs e)
        {
            var jArray = JsonConvert.DeserializeObject(this.tbx_Recipient.Text) as JArray;

            if(jArray == null) return;

            //var recipients = this.tbx_Recipient.Text.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

            this.btn_Send.Text = "正在发送...";

            this.btn_Send.Enabled = false;

            Program.SetLog(this.tbx_Schedule, "正在发送...");

            //Task.Run(() => Send(jArray));

            RunAsync(() => Send(jArray), () =>
            {
                this.btn_Send.Text = "发送";

                this.btn_Send.Enabled = true;

                Program.SetLog(this.tbx_Schedule, "发送完成！");
            });
        }

        private void Send(JArray jArray)
        {
            var count = 0;

            foreach (var recipient in jArray)
            {
                var emailFactory = new EmailFactory((string)recipient["email"]);

                var body = (string)recipient["name"];

                var ascii = (int)Convert.ToChar(body.Substring(0, 1).ToLower());

                if (ascii < 97 || ascii > 122) body = body.Substring(0, 1);

                emailFactory.Body = $"{body} {this.tbx_Body.Text}";

                emailFactory.Subject = this.tbx_Subject.Text;

                emailFactory.IsBodyHtml = false;

                if (this.lbl_Annex.Text != "未选择附件...") emailFactory.Attachments(this.lbl_Annex.Text);

                try
                {
                    emailFactory.Send();

                    RunInMainthread(() =>
                    {
                        Program.SetLog(this.tbx_Schedule, $"发送成功！{(string)recipient["email"]} {++count} / {jArray.Count}");
                    });
                }
                catch (Exception ex)
                {
                    RunInMainthread(() =>
                    {
                        Program.SetLog(this.tbx_Schedule, $"发送失败！{ex.Message} {(string)recipient["email"]} {++count} / {jArray.Count}");
                    });
                }
            }
        }

        // 异步线程
        public void RunAsync(Action action, Action callBackAction = null)
        {
            ((Action)action.Invoke).BeginInvoke(a =>
            {
                if (callBackAction != null) this.BeginInvoke((Action)callBackAction.Invoke);
            }, null);
        }

        public void RunInMainthread(Action action)
        {
            this.BeginInvoke((Action)action.Invoke);
        }
    }
}
