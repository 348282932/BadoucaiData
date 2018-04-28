using System;
using System.Windows.Forms;
using Badoucai.WindowsForm.Zhaopin;
using Badoucai.Library;
using Badoucai.WindowsForm.Tools;

namespace Badoucai.WindowsForm
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SpiderDodiForm());
        }

        public delegate void SetLogCallBack(TextBox textbox, string content);

        public static void SetLog(TextBox textbox, string content)
        {
            try
            {
                if (textbox.InvokeRequired)
                {
                    while (!textbox.IsHandleCreated)
                    {
                        if (textbox.Disposing || textbox.IsDisposed) return;
                    }

                    textbox.BeginInvoke(new SetLogCallBack(SetLog));
                }
                else
                {
                    if (textbox.Text.Length > 2000)
                    {
                        textbox.Text = content + Environment.NewLine;
                    }
                    else
                    {
                        textbox.AppendText(content + Environment.NewLine);
                    }

                }
            }
            catch (Exception ex)
            {
                LogFactory.Error(ex.Message + ex.StackTrace);
            }
        }

        public static void RunAsync(this Form form, Action action, Action callBackAction = null)
        {
            ((Action)action.Invoke).BeginInvoke(a =>
            {
                if (callBackAction != null) form.BeginInvoke((Action)callBackAction.Invoke);
            }, null);
        }

        public static void RunInMainthread(this Form form, Action action, bool isAsync = true)
        {
            if(isAsync)
                form.BeginInvoke((Action)action.Invoke);
            else
                form.Invoke((Action)action.Invoke);
        }

        public static void AsyncSetLog(this Form form, TextBox textbox, string content)
        {
            RunInMainthread(form, () =>
            {
                SetLog(textbox, content);
            });
        }
    }
}
