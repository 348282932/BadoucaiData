using Badoucai.EntityFramework.MySql;
using Fiddler;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Badoucai.WindowsForm.Zhaopin
{
    public partial class NewSystemLoginForm : Form
    {
        public NewSystemLoginForm()
        {
            InitializeComponent();
        }

        private static string account;

        private static string password;

        private static bool isWaitLogin;

        private static bool isLogined;

        private static readonly string checkCellphone = ConfigurationManager.AppSettings["CheckCellphone"];

        private void OldSystemLoginForm_Load(object sender, EventArgs e)
        {
            FiddlerApplication.BeforeRequest += oSessions =>
            {
                //if (oSessions.url.Contains("zhaopin")) oSessions["X-OverrideGateway"] = "39.108.161.230:8080";

                oSessions.bBufferResponse = true;

                if (oSessions.url.Contains("captcha.js"))
                {
                    if (oSessions.RequestHeaders.Exists("If-Modified-Since")) oSessions.RequestHeaders.Remove("If-Modified-Since");

                    if (oSessions.RequestHeaders.Exists("If-None-Match")) oSessions.RequestHeaders.Remove("If-None-Match");

                    if (oSessions.RequestHeaders.Exists("Accept-Encoding")) oSessions.RequestHeaders.Remove("Accept-Encoding");
                }

                if (oSessions.RequestHeaders.Exists("User-Agent")) oSessions.RequestHeaders["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36";
            };

            FiddlerApplication.BeforeResponse += oSessions =>
            {
                if (oSessions.url.Contains("captcha.js"))
                {
                    oSessions.ResponseHeaders["Cache-Control"] = "no-store";

                    var jsContent = oSessions.GetResponseBodyEncoding().GetString(oSessions.ResponseBody);

                    jsContent = "var hackStr = '';" + jsContent;

                    jsContent = jsContent.Replace("pData.join(\";\")", "hackStr");

                    jsContent = jsContent.Remove(jsContent.Length - 4);

                    jsContent += "this.hack = function (coordinate){ $(\"#captcha-submitCode\").removeClass(\"btn-disabled\");validate = true;hackStr = coordinate; $(\"#captcha-submitCode\").trigger(\"click\"); return true;}";

                    jsContent += "}\r\n";

                    jsContent += "function execHack(coordinate){this.captcha.hack(coordinate); return 1;}";

                    oSessions.ResponseBody = oSessions.GetResponseBodyEncoding().GetBytes(jsContent);
                }
            };

            if (!FiddlerApplication.IsStarted())
            {
                FiddlerApplication.Startup(8887, FiddlerCoreStartupFlags.Default);
            }

            this.webBrowser.DocumentCompleted += webBrowser_DocumentCompleted;

            this.webBrowser.ScriptErrorsSuppressed = true;

            this.RunAsync(() =>
            {
                while (true)
                {
                    List<ZhaopinStaff> accounts;

                    using (var db = new MangningXssDBEntities())
                    {
                        //var companyArray = db.ZhaoPinCompany.AsNoTracking().Where(w => w.Source.Contains("MANUAL")).Select(s => s.Id).ToArray();

                        //accounts = db.ZhaopinStaff.Where(f => (companyArray.Contains(f.CompanyId) || f.Source.Contains("5.5")) && string.IsNullOrEmpty(f.Cookie) ).ToList();

                        var staffIdArray = new[] { 705826281, 683974003, 705834336, 705675698, 700680503, 700537915, 705680163, 698198504 };

                        accounts = db.ZhaopinStaff.AsNoTracking().Where(w => (staffIdArray.Contains(w.Id) || w.Source.Contains("5.5")) && string.IsNullOrEmpty(w.Cookie)).ToList();
                    }

                    this.AsyncSetLog(this.tbx_Log, $"共 {accounts.Count} 个号准备登录！");

                    if(accounts.Count == 0) Thread.Sleep(1000);

                    foreach (var item in accounts)
                    {
                        if (item.Id == 705675698) item.Username = "mangning_2";

                        account = item.Username;

                        password = item.Password;

                        webBrowser.Navigate("https://passport.zhaopin.com/org/login");

                        while (true)
                        {
                            if (!isWaitLogin)
                            {
                                Thread.Sleep(1000);

                                continue;
                            }

                            var index = 0;

                            object obj;

                            var isChecked = false;

                            while (!isChecked)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    index++;

                                    if (this.webBrowser.Document?.GetElementById("CheckCodeCapt")?.GetAttribute("value") == "验证通过")
                                    {
                                        Thread.Sleep(3000);

                                        this.webBrowser.Document?.GetElementById("loginbutton")?.InvokeMember("click");

                                        isChecked = true;

                                        return;
                                    }

                                    if (index % 2 != 0)
                                    {
                                        obj = this.webBrowser.Document?.InvokeScript("execHack", new object[] { "66,76;173,44;239,80" });
                                    }
                                    else
                                    {
                                        obj = this.webBrowser.Document?.InvokeScript("execHack", new object[] { "44,44;190,37;122,48" });
                                    }

                                    if (obj == null)
                                    {
                                        this.AsyncSetLog(this.tbx_Log, "调用验证 JS 异常");
                                    }

                                });

                                Thread.Sleep(500);
                            }

                            //Thread.Sleep(5000);

                            while (true)
                            {
                                if (!isLogined)
                                {
                                    Thread.Sleep(1000);

                                    continue;
                                }

                                break;
                            }

                            this.Invoke((MethodInvoker)delegate
                            {
                                item.Cookie = webBrowser.Document?.Cookie;

                                var document = webBrowser.Document;

                                document?.ExecCommand("ClearAuthenticationCache", false, null);
                            });

                            this.AsyncSetLog(this.tbx_Log, $"{account} 获取Cookie成功！");

                            using (var db = new MangningXssDBEntities())
                            {
                                var pro = db.ZhaopinStaff.FirstOrDefault(f => f.Id == item.Id);

                                if (pro != null) pro.Cookie = item.Cookie;

                                db.SaveChanges();
                            }

                            isWaitLogin = false;

                            isLogined = false;

                            break;
                        }
                    }
                }
            });
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser.ReadyState != WebBrowserReadyState.Complete) return;

            if (e.Url.AbsoluteUri.Contains("https://passport.zhaopin.com/org/login"))
            {
                this.AsyncSetLog(this.tbx_Log, e.Url.AbsoluteUri);

                this.webBrowser.Document?.GetElementById("loginName")?.SetAttribute("value", account);

                this.webBrowser.Document?.GetElementById("password")?.SetAttribute("value", password);

                Thread.Sleep(1000);

                this.AsyncSetLog(this.tbx_Log, "waiting...");

                this.webBrowser.Document?.GetElementById("checkCodeCapt")?.InvokeMember("click");

                isWaitLogin = true;

                return;
            }

            if (e.Url.AbsoluteUri == "https://ihr.zhaopin.com/")
            {
                isLogined = true;

                return;
            }

            if (e.Url.AbsoluteUri == "https://rd5.zhaopin.com/")
            {
                isLogined = true;

                return;
            }

            if (e.Url.AbsoluteUri.Contains("https://passport.zhaopin.com/org/verifyMobile"))
            {
                this.webBrowser.Document?.GetElementById("vmobile")?.SetAttribute("value", checkCellphone);

                this.webBrowser.Document?.GetElementById("verifyCode")?.SetAttribute("value", this.textBox1.Text);

                Thread.Sleep(3000);

                this.webBrowser.Document?.GetElementById("confirm_btn")?.InvokeMember("click");
            }
        }

        private void OldSystemLoginForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            FiddlerApplication.Shutdown();
        }
    }
}
