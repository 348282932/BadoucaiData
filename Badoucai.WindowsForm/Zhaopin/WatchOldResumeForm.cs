using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Aliyun.OSS;
using Badoucai.Business.Model;
using Badoucai.Business.Socket;
using Badoucai.Business.Zhaopin;
using Badoucai.Library;

namespace Badoucai.WindowsForm.Zhaopin
{
    public partial class WatchOldResumeForm : Form
    {
        private readonly WatchOldResumeBusiness watchBusiness = new WatchOldResumeBusiness();

        private readonly MatchResumeLocationBusiness matchBusiness = new MatchResumeLocationBusiness();

        private readonly CheckCodeBusiness checkCodeBusiness = new CheckCodeBusiness();

        private static readonly ConcurrentQueue<string> cacheQueue = new ConcurrentQueue<string>(); 

        private static readonly string newBucket = ConfigurationManager.AppSettings["Oss.New.Bucket"];

        private static OssClient newOss;

        private static string account;

        private static string password;

        private static short cleaningId;

        private Socket socket;

        private int companyId;

        private bool isListCompleted;

        private int watchCount;

        private bool isCodeChecked = true;

        private bool isStop;

        private bool isResert;

        public WatchOldResumeForm()
        {
            InitializeComponent();

            newOss = new OssClient(
                ConfigurationManager.AppSettings["Oss.New.Url"],
                ConfigurationManager.AppSettings["Oss.New.KeyId"],
                ConfigurationManager.AppSettings["Oss.New.KeySecret"]);
        }

        private void WatchOldResumeForm_Load(object sender, EventArgs e)
        {
            //new CheckCodeForm().Show();

            //new CheckCodeSchedulingForm().Show();

            this.webBrowser.DocumentCompleted += webBrowser_DocumentCompleted;

            this.webBrowser.ScriptErrorsSuppressed = true;

            this.btn_Start.Enabled = false;

            this.btn_Stop.Enabled = false;

            this.btn_Login.Enabled = false;

            var config = this.watchBusiness.GetSingleProcedure();

            if (config == null)
            {
                Program.SetLog(this.tbx_Log,"没有可用的帐号！");

                return;
            }

            account = config.Account;

            password = config.Password;

            cleaningId = config.Id;

            this.Text = account;

            this.RunAsync(this.SaveCacheByOss);

            btn_Connection_Click(sender, e);
        }

        private void btn_Connection_Click(object sender, EventArgs e)
        {
            webBrowser.Navigate("https://rd2.zhaopin.com/s/homepage.asp");

            this.RunAsync(this.ConnectServer);
        }

        private void btn_Login_Click(object sender, EventArgs e)
        {
            var isChecked = false;

            this.RunAsync(() =>
            {
                var index = 0;

                object obj;

                while (!isChecked)
                {
                    this.RunInMainthread(() =>
                    {
                        index++;

                        if (this.webBrowser.Document?.GetElementById("CheckCodeCapt")?.GetAttribute("value") == "验证通过")
                        {
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

                            isChecked = true;
                        }
                    });
                    
                    Thread.Sleep(500);
                }
            });
        }

        private void btn_Start_Click(object sender, EventArgs e)
        {
            if (isStop)
            {
                this.AsyncSetLog(this.tbx_Log, "触发stop = false");

                isStop = false;

                return;
            }

            isResert = false;

            this.RunAsync(this.Search);
        }

        private void btn_Stop_Click(object sender, EventArgs e)
        {
            this.AsyncSetLog(this.tbx_Log, "触发stop = true");

            this.btn_Stop.Enabled = false;

            this.btn_Start.Enabled = true;

            isStop = true;
        }

        /// <summary>
        /// 上传验证码图片到OSS
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="Id"></param>
        [Obsolete("登录无法自动验证时使用")]
        public static void UploadCheckCodeImageToOss(byte[] imageBytes, short Id)
        {
            while (true)
            {
                try
                {
                    using (var stream = new MemoryStream(imageBytes))
                    {
                        newOss.PutObject(newBucket, $"CheckCode/{Id}.jpeg", stream);

                        newOss.SetObjectAcl(newBucket, $"CheckCode/{Id}.jpeg", CannedAccessControlList.PublicRead);
                    }

                    break;
                }
                catch (Exception ex)
                {
                    LogFactory.Warn($"验证码图片上传OSS异常！异常信息：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        private void ConnectServer()
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var port = Convert.ToInt32(ConfigurationManager.AppSettings["ListenerPort"]);

            var ipAddress = ConfigurationManager.AppSettings["ListenerIpAddress"];

            try
            {
                socket.Connect(ipAddress, port);
            }
            catch (Exception ex)
            {
                this.RunInMainthread(() =>
                {
                    MessageBox.Show("连接服务器异常！" + ex.Message);
                });

                socket.Dispose();

                return;
            }

            this.RunInMainthread(() =>
            {
                this.btn_Connection.Text = "已连接";

                this.btn_Connection.Enabled = false;

                this.btn_Login.Enabled = true;

                this.btn_Start.Enabled = true;
            });

            this.RunAsync(this.ListenerServer);

            this.RunAsync(this.InitProcedure);

            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(30));

                try
                {
                    socket.Send(new HeartbeatPackage().Serialize());

                    LogFactory.Debug($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} 发出心跳，程序ID => {account}");
                }
                catch (Exception)
                {
                    this.RunInMainthread(() =>
                    {
                        this.btn_Start.Enabled = false;

                        this.btn_Stop.Enabled = false;

                        this.btn_Login.Enabled = false;

                        this.btn_Connection.Enabled = true;
                    });

                    this.socket.Dispose();

                    return;
                }
            }
        }

        /// <summary>
        /// 监听Socket端口
        /// </summary>
        private void ListenerServer()
        {
            var bytes = new byte[1024];

            while (true)
            {
                try
                {
                    if (this.socket.Available == 0)
                    {
                        Thread.Sleep(100);

                        continue;
                    }
                }
                catch (Exception)
                {
                    return;
                }

                socket.Receive(bytes, 0, 3, SocketFlags.None);

                var socketId = bytes[0];

                var length = (short)(bytes[1] << 8 | bytes[2]);

                using (var stream = new MemoryStream())
                {
                    stream.Write(bytes, 0, 3);

                    for (var i = 3; i < length; i += bytes.Length)
                    {
                        var readLength = length - i > bytes.Length ? bytes.Length : length - i;

                        var len = 0;

                        while (len != readLength)
                        {
                            len += socket.Receive(bytes, len, readLength - len, SocketFlags.None);
                        }

                        stream.Write(bytes, 0, readLength);
                    }

                    var streamBytes = stream.ToArray();

                    if (socketId == 0x03)
                    {
                        var package = new CheckedResultPackage().DeSerialize(streamBytes);

                        if (package.Status == 1)
                        {
                            isCodeChecked = true;
                        }

                        continue;
                    }

                    if (socketId == 0x07)
                    {
                        isStop = true;

                        continue;
                    }

                    if (socketId == 0x08)
                    {
                        isStop = false;
                    }
                }
            }
        }

        /// <summary>
        /// 初始化帐号
        /// </summary>
        private void InitProcedure()
        {
            try
            {
                var accountBytes = Encoding.UTF8.GetBytes(account);

                var passwordBytes = Encoding.UTF8.GetBytes(password);

                socket.Send(new CleaningProcedurePackage
                {
                    Id = 0x04,
                    Length = (byte)(7 + accountBytes.Length + passwordBytes.Length),
                    CleaningId = cleaningId,
                    AccountLength = (byte)accountBytes.Length,
                    Account = accountBytes,
                    PasswordLength = (byte)passwordBytes.Length,
                    Password = passwordBytes
                }.Serialize());
            }
            catch (Exception)
            {
                this.RunInMainthread(() =>
                {
                    this.btn_Start.Enabled = false;

                    this.btn_Connection.Enabled = true;
                });

                this.socket.Dispose();
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        private void Search()
        {
            while (true)
            {
                var condition = watchBusiness.GetSingleCondition(account);

                if (condition == null) break;

                var isSearchSuccess = true;

                this.Invoke((MethodInvoker)delegate
                {
                    try
                    {
                        var htmlEle = webBrowser.Document?.GetElementById("SF_1_1_9_" + condition.Gender);

                        htmlEle?.SetAttribute("checked", "checked");

                        htmlEle = webBrowser.Document?.GetElementById("SF_1_1_7");

                        htmlEle?.Children[0].SetAttribute("value", "4,9");

                        htmlEle = webBrowser.Document?.GetElementById("SF_1_1_8_min");

                        htmlEle?.SetAttribute("value", $"{condition.Age}");

                        htmlEle = webBrowser.Document?.GetElementById("SF_1_1_8_max");

                        htmlEle?.SetAttribute("value", $"{condition.Age}");

                        htmlEle = webBrowser.Document?.GetElementById("SF_1_1_5_min");

                        htmlEle?.Children[0].SetAttribute("value", $"{condition.Degrees}");

                        htmlEle?.Children[0].SetAttribute("selected", "selected");

                        htmlEle = webBrowser.Document?.GetElementById("SF_1_1_5_max");

                        htmlEle?.Children[0].SetAttribute("value", $"{condition.Degrees}");

                        htmlEle?.Children[0].SetAttribute("selected", "selected");

                        htmlEle = webBrowser.Document?.GetElementById("SF_1_1_4_min");

                        htmlEle?.Children[0].SetAttribute("value", $"{condition.WorkYears}");

                        htmlEle?.Children[0].SetAttribute("selected", "selected");

                        htmlEle = webBrowser.Document?.GetElementById("SF_1_1_4_max");

                        htmlEle?.Children[0].SetAttribute("value", $"{condition.WorkYears}");

                        htmlEle?.Children[0].SetAttribute("selected", "selected");

                        htmlEle = webBrowser.Document?.GetElementById("SF_1_1_6");

                        htmlEle?.SetAttribute("value", "");

                        htmlEle = this.webBrowser.Document?.GetElementById("searchSubmit")?.Children[0];

                        Thread.Sleep(500);

                        isListCompleted = false;

                        htmlEle?.InvokeMember("click");
                    }
                    catch (Exception)
                    {
                        Program.SetLog(this.tbx_Log, "搜索条件初始化异常！刷新Dom！");

                        isSearchSuccess = false;
                    }
                });

                if (!isSearchSuccess)
                {
                    this.btn_Start.Enabled = true;

                    isListCompleted = false;

                    webBrowser.Navigate("https://rdsearch.zhaopin.com/Home/SearchByCustom?source=rd");

                    return;
                }

                while (true)
                {
                    if(isListCompleted) break;

                    if (this.isResert) return;

                    this.AsyncSetLog(this.tbx_Log, "等待查询结果页响应！");

                    if (!this.isCodeChecked)
                    {
                        while (true)
                        {
                            if (this.isResert) return;

                            if (this.isCodeChecked)
                            {
                                this.btn_Start.Enabled = true;

                                isListCompleted = false;

                                webBrowser.Navigate("https://rdsearch.zhaopin.com/Home/SearchByCustom?source=rd");

                                return;
                            }

                            Thread.Sleep(1000);
                        }
                    }

                    Thread.Sleep(1000);
                }

                isListCompleted = false;

                Match match = null;

                this.Invoke((MethodInvoker) delegate
                {
                    match = Regex.Match(webBrowser.DocumentText, "(?s)<span>(\\d+)</span>份简历.+?<span id=\"rd-resumelist-pageNum\">1/(\\d+)</span>");
                });

                if (!match.Success) continue;

                var resumeCount = Convert.ToInt32(match.Result("$1"));

                this.AsyncSetLog(this.tbx_Log, $"共 {resumeCount} 份简历");

                //watchBusiness.SetSearchStatus(condition.Id, 1, resumeCount);

                var pageTotal = Convert.ToInt32(match.Result("$2"));

                for (var i = 1; i < pageTotal + 1; i++)
                {
                    MatchCollection matchs = null;

                    this.Invoke((MethodInvoker)delegate
                    {
                        if (condition.LastWatchPage > i)
                        {
                            i = condition.LastWatchPage;

                            this.webBrowser.Document?.InvokeScript("eval", new object[] { "$(\".rd-resumelist-page\")[0].defaultValue = " + i });
                        }

                        if (companyId == 0) match = Regex.Match(webBrowser.DocumentText, "companyId\".+?value=\"(.+?)\"");

                        matchs = Regex.Matches(webBrowser.DocumentText, "rg=\"\\d+\".+?RedirectToRd/(.+?)','(.+?)','(.+?)',this");
                    });

                    if (match == null || !match.Success) break;

                    if (companyId == 0) companyId = Convert.ToInt32(match.Result("$1"));

                    this.AsyncSetLog(this.tbx_Log,$"当前页匹配结果 => {matchs.Count}");

                    foreach (Match matchResult in matchs)
                    {
                        WatchDetail(matchResult.Result("$1"), matchResult.Result("$2"), matchResult.Result("$3"));
                    }

                    if (i != pageTotal)
                    {
                        isListCompleted = false;

                        this.Invoke((MethodInvoker)delegate
                        {
                            this.webBrowser.Document?.InvokeScript("eval", new object[] { "$('.rd-resumelist-img').eq(0).trigger('click')" });

                            //var nextUrl = this.webBrowser.Document?.GetElementById("rd-resumelist-pageNum")?.Parent?.Children[3].GetAttribute("href");

                            //this.webBrowser.Document?.InvokeScript("getJump", new object[] { nextUrl });

                        });

                        while (true)
                        {
                            if (isListCompleted) break;

                            if (this.isResert) return;

                            this.AsyncSetLog(this.tbx_Log, "等待下一页响应！");

                            if (!this.isCodeChecked)
                            {
                                while (true)
                                {
                                    if(this.isResert) return;

                                    if (this.isCodeChecked)
                                    {
                                        this.btn_Start.Enabled = true;

                                        isListCompleted = false;

                                        webBrowser.Navigate("https://rdsearch.zhaopin.com/Home/SearchByCustom?source=rd");

                                        return;
                                    }

                                    Thread.Sleep(1000);
                                }
                            }

                            Thread.Sleep(1000);
                        }

                        if (i < pageTotal) watchBusiness.SetSearchStatus(condition.Id, 0, resumeCount, i + 1);

                        isListCompleted = false;
                    }
                }

                watchBusiness.SetSearchStatus(condition.Id, 1, resumeCount, pageTotal);
            }
        }

        /// <summary>
        /// 查看详情
        /// </summary>
        /// <param name="resumeAction"></param>
        /// <param name="t"></param>
        /// <param name="k"></param>
        private void WatchDetail(string resumeAction, string t, string k)
        {
            while (true)
            {
                if (!this.isStop) break;

                this.AsyncSetLog(this.tbx_Log, "等待启动！");

                Thread.Sleep(5000);
            }

            var number = resumeAction.Substring(0, 10);

            var isWatched = this.watchBusiness.QueryResumeIsExists(number);

            if (isWatched)
            {
                this.AsyncSetLog(this.tbx_Log, $"{number} 已看过！");

                return;
            }

            while (true)
            {
                CookieContainer cookie = null;

                this.RunInMainthread(() =>
                {
                    cookie = this.webBrowser.Document?.Cookie.Serialize(".zhaopin.com");

                }, false);

                var documentText = RequestFactory.QueryRequest($"https://rd.zhaopin.com/resumepreview/resume/viewone/2/{resumeAction}&t={t}&k={k}&v=", "", RequestEnum.POST, cookie).Data;

                if (string.IsNullOrEmpty(documentText))
                {
                    this.AsyncSetLog(this.tbx_Log, "查看详情异常！");

                    continue;
                }

                var match = Regex.Match(documentText, "<input.+?resumeUserId.+?value=\"(.+?)\"");

                if (!match.Success)
                {
                    if (!documentText.Contains("点击刷新图片"))
                    {
                        this.AsyncSetLog(this.tbx_Log, $"{number} 匹配详情异常!");

                        LogFactory.Warn("匹配匹配详情异常! 详情页Dom =>" + documentText);

                        return;
                    }

                    this.AsyncSetLog(this.tbx_Log, "请输入验证码！");

                    this.isCodeChecked = false;

                    this.Invoke((MethodInvoker)delegate
                    {
                        if (this.webBrowser.Document == null) return;

                        var cookieBytes = Encoding.UTF8.GetBytes(this.webBrowser.Document.Cookie);

                        var accountBytes = Encoding.UTF8.GetBytes(account);

                        socket.Send(new CheckCodePackage
                        {
                            Id = 0x05,
                            Length = (short)(cookieBytes.Length + accountBytes.Length + 15),
                            Cookie = cookieBytes,
                            Status = 0,
                            Account = accountBytes,
                            Type = 1
                        }.Serialize());
                    });

                    while (true)
                    {
                        if (this.isCodeChecked) break;

                        this.AsyncSetLog(this.tbx_Log, "等待验证！");

                        Thread.Sleep(1000);
                    }

                    continue;
                }

                cacheQueue.Enqueue(documentText);

                break;
            }
        }

        /// <summary>
        /// 保存简历缓存到OSS队列
        /// </summary>
        private void SaveCacheByOss()
        {
            while (true)
            {
                string documentText;

                if (!cacheQueue.TryDequeue(out documentText)) continue;

                var savePath = $@"D:\Badoucai\OldZhaopinResume\{DateTime.Now:yyyyMMdd}";

                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                var resumeNumber = Regex.Match(documentText, "<input.+?name=\"extId\".+?value=\"(.+?)\"").Result("$1").Substring(0, 10);

                var modifyTimeMatch = Regex.Match(documentText, "resumeUpdateTime\">(.+?)</strong>");

                if(!modifyTimeMatch.Success) continue;

                var model = new ResumeMatchResult
                {
                    Cellphone = null,
                    Email = null,
                    ModifyTime = DateTime.Parse(modifyTimeMatch.Result("$1")),
                    Name = Regex.Match(documentText, "<input.+?name=\"tt_username\".+?value=\"(.+?)\"").Result("$1"),
                    ResumeId = Convert.ToInt32(Regex.Match(documentText, "<input.+?name=\"resume_id\".+?value=\"(.+?)\"").Result("$1")),
                    ResumeNumber = resumeNumber,
                    UserExtId = null,
                    UserId = Convert.ToInt32(Regex.Match(documentText, "<input.+?name=\"resumeUserId\".+?value=\"(.+?)\"").Result("$1")),
                    Path = $@"{savePath}\{resumeNumber}.txt",
                    CompanyId = companyId
                };

                File.WriteAllText($@"{savePath}\{resumeNumber}.txt", documentText);

                UploadResumeToOss($@"{savePath}\{resumeNumber}.txt");

                this.matchBusiness.SaveMatchedCache(model);

                this.AsyncSetLog(this.tbx_Log, $"ResumeNumber => {resumeNumber}{Environment.NewLine}WatchCount =>{++watchCount}");
            }
        }

        /// <summary>
        /// 上传简历到OSS
        /// </summary>
        /// <param name="path"></param>
        private static void UploadResumeToOss(string path)
        {
            while (true)
            {
                try
                {
                    using (var stream = new MemoryStream(GZip.Compress(File.ReadAllBytes(path))))
                    {
                        var businessId = Path.GetFileNameWithoutExtension(path);

                        newOss.PutObject(newBucket, $"OldZhaopinCache/{businessId}", stream);

                        Console.WriteLine($"上传成功！ path:{path}");

                        File.Delete(path);
                    }

                    break;
                }
                catch (Exception ex)
                {
                    LogFactory.Warn($"缓存简历上传OSS异常！异常信息：{ex.Message}");
                }
            }
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser.ReadyState != WebBrowserReadyState.Complete) return;

            if (e.Url.AbsoluteUri.Contains("https://passport.zhaopin.com/org/login"))
            {
                this.webBrowser.Document?.GetElementById("loginName")?.SetAttribute("value", account);

                this.webBrowser.Document?.GetElementById("password")?.SetAttribute("value", password);

                this.webBrowser.Document?.GetElementById("checkCodeCapt")?.InvokeMember("click");

                btn_Login_Click(sender, e);

                return;
            }

            if (e.Url.AbsoluteUri.Contains("https://rd2.zhaopin.com/s/homepage.asp"))
            {
                webBrowser.Navigate("https://rdsearch.zhaopin.com/Home/SearchByCustom?source=rd");

                //this.btn_Start.Enabled = true;

                return;
            }

            if (e.Url.AbsoluteUri.Contains("https://rdsearch.zhaopin.com/Home/ResultForCustom"))
            {
                isListCompleted = true;

                return;
            }

            if (e.Url.AbsoluteUri.Contains("https://rdsearch.zhaopin.com/Home/SearchByCustom?source=rd"))
            {
                if (this.btn_Start.Enabled)
                {
                    this.btn_Stop.Enabled = true;

                    this.btn_Start.Enabled = false;

                    btn_Start_Click(sender, e);
                }

                isListCompleted = true;

                return;
            }

            if (e.Url.AbsoluteUri.Contains("https://rd.zhaopin.com/resumepreview/resume/validateuser"))
            {
                this.isCodeChecked = false;

                if (this.webBrowser.Document == null) return;

                var cookieBytes = Encoding.UTF8.GetBytes(this.webBrowser.Document.Cookie);

                var accountBytes = Encoding.UTF8.GetBytes(account);

                socket.Send(new CheckCodePackage
                {
                    Id = 0x05,
                    Length = (short)(cookieBytes.Length + accountBytes.Length + 15),
                    Cookie = cookieBytes,
                    Status = 0,
                    Account = accountBytes,
                    Type = 1
                }.Serialize());

                Task.Run(() => WaitChecked());
            }

            if (e.Url.AbsoluteUri.Contains("https://rdsearch.zhaopin.com/Home/undefined"))
            {
                this.btn_Start.Enabled = true;

                isResert = true;

                isListCompleted = false;

                webBrowser.Navigate("https://rdsearch.zhaopin.com/Home/SearchByCustom?source=rd");
            }
        }

        private void WaitChecked()
        {
            while (true)
            {
                if (this.isCodeChecked) break;

                this.AsyncSetLog(this.tbx_Log, "等待输入验证码！");

                Thread.Sleep(1000);
            }

            //webBrowser.Navigate(HttpUtility.UrlDecode(Regex.Match(url, "url=(\\S+)").Result("$1")));
        }

        private void WatchOldResumeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            checkCodeBusiness.CleaningEnd(account);
        }
    }
}
