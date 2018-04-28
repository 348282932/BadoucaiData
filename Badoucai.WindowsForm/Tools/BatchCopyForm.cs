using System;
using System.Windows.Forms;
using Badoucai.Library;

namespace Badoucai.WindowsForm.Tools
{
    public partial class BatchCopyForm : Form
    {
        public BatchCopyForm()
        {
            InitializeComponent();
        }

        private void btn_Start_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.tbx_CopyNumber.Text))
            {
                Program.SetLog(this.tbx_Logger,"请输入拷贝份数！");

                return;
            }

            int copyNumber;

            if (!int.TryParse(this.tbx_CopyNumber.Text, out copyNumber))
            {
                Program.SetLog(this.tbx_Logger, "拷贝份数必须是数字！");

                return;
            }

            if (string.IsNullOrWhiteSpace(this.tbx_FromFolderPath.Text))
            {
                var folderBrowserDialog = new FolderBrowserDialog();

                if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

                this.tbx_FromFolderPath.Text = folderBrowserDialog.SelectedPath;
            }

            this.RunAsync(() =>
            {
                for (var i = 0; i < copyNumber; i++)
                {
                    BaseFanctory.CopyDir(this.tbx_FromFolderPath.Text, $"{this.tbx_FromFolderPath.Text}_{i + 1:000}");

                    this.AsyncSetLog(this.tbx_Logger, $"拷贝成功！第 {i + 1} 份");
                }

                this.AsyncSetLog(this.tbx_Logger, "拷贝完成！");
            });
        }
    }
}
