using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.IO;
using System.IO.Compression;
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
using System.Runtime.InteropServices;
using System.Reflection;
using Fiddler;

namespace Badoucai.WindowsForm.Zhaopin
{
    public partial class SearchResumeForm : Form
    {
        private static readonly ConcurrentQueue<SearchResumeModel> resumeQueue = new ConcurrentQueue<SearchResumeModel>();

        private readonly MatchResumeLocationBusiness business = new MatchResumeLocationBusiness();

        private static SearchResumeModel searchResume;

        private Socket socket;

        private static readonly short cleaningId = Convert.ToInt16(ConfigurationManager.AppSettings["CleaningId"]);

        private static string userName = "iu27839953jq";

        private static string password = "sjbs168a";

        private static bool isListCompleted;

        private static bool isDetailCompleted;

        private static bool isSearchCompleted;

        

        private static int searchCount = -1;

        private static int successCount;

        private static bool isWaitCheck;

        private static OssClient newOss;

        private static readonly string newBucket = ConfigurationManager.AppSettings["Oss.New.Bucket"];

        private static bool isStop;

        private static bool checkResult;

        private int companyId;

        public SearchResumeForm()
        {
            InitializeComponent();

            webBrowser.ScriptErrorsSuppressed = true;

            this.webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;

            newOss = new OssClient(
                ConfigurationManager.AppSettings["Oss.New.Url"],
                ConfigurationManager.AppSettings["Oss.New.KeyId"],
                ConfigurationManager.AppSettings["Oss.New.KeySecret"]);

            FiddlerApplication.BeforeRequest += oSessions =>
            {
                //if (oSessions.url.Contains("zhaopin")) oSessions["X-OverrideGateway"] = $"{webProxyIp}:{webProxyPort}";

                oSessions.bBufferResponse = true;

                if (oSessions.url.Contains("captcha.js"))
                {
                    if (oSessions.RequestHeaders.Exists("If-Modified-Since")) oSessions.RequestHeaders.Remove("If-Modified-Since");

                    if (oSessions.RequestHeaders.Exists("If-None-Match")) oSessions.RequestHeaders.Remove("If-None-Match");

                    if (oSessions.RequestHeaders.Exists("Accept-Encoding")) oSessions.RequestHeaders.Remove("Accept-Encoding");
                }

                if (oSessions.RequestHeaders.Exists("User-Agent")) oSessions.RequestHeaders["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36";
            };

            if (!FiddlerApplication.IsStarted())
            {
                FiddlerApplication.Startup(8887, FiddlerCoreStartupFlags.Default);
            }
        }

        private void SearchResumeForm_Load(object sender, EventArgs e)
        {
            this.btn_Start.Enabled = false;

            userName = ConfigurationManager.AppSettings["Account"];

            password = ConfigurationManager.AppSettings["Password"];

            this.Text = ConfigurationManager.AppSettings["CleaningId"];

            webBrowser.Navigate("https://passport.zhaopin.com/org/login");
        }

        private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser.ReadyState != WebBrowserReadyState.Complete) return;

            if (e.Url.AbsoluteUri.Contains("https://rd2.zhaopin.com/s/homepage.asp"))
            {
                webBrowser.Navigate("https://rdsearch.zhaopin.com/Home/SearchByCustom?source=rd");
            }

            if (e.Url.AbsoluteUri.Contains("https://rdsearch.zhaopin.com/Home/SearchByCustom"))
            {
                isSearchCompleted = true;
            }

            if (e.Url.AbsoluteUri.Contains("https://rd.zhaopin.com/resumepreview/resume/validateuser"))
            {
                while (true)
                {
                    if (!isWaitCheck)
                    {
                        if(this.webBrowser.Document?.Cookie == null) break;

                        var cookieBytes = Encoding.UTF8.GetBytes(this.webBrowser.Document.Cookie);

                        var accountBytes = Encoding.UTF8.GetBytes(userName);

                        socket.Send(new CheckCodePackage
                        {
                            Id = 0x05,
                            Length = (short)(cookieBytes.Length + 15),
                            Cookie = cookieBytes,
                            Status = 0,
                            Account = accountBytes,
                            Type = 1
                        }.Serialize());

                        isWaitCheck = true;
                    }
                    else
                    {
                        if (checkResult)
                        {
                            isWaitCheck = false;

                            break;
                        }

                        Thread.Sleep(1000);
                    }
                }

                webBrowser.Navigate(HttpUtility.UrlDecode(Regex.Match(e.Url.AbsoluteUri, "url=(\\S+)").Result("$1")));

                return;
            }

            if (e.Url.AbsoluteUri.Contains("https://rdsearch.zhaopin.com/Home/ResultForCustom"))
            {
                var match = Regex.Match(webBrowser.DocumentText, "共<span>(\\d+)</span>份简历");

                if (!match.Success)
                {
                    isListCompleted = true;

                    return;
                }

                var count = Convert.ToInt32(match.Result("$1"));

                if (count == 0 || count > 30)
                {
                    isListCompleted = true;

                    return;
                }

                if (companyId == 0)
                {
                    match = Regex.Match(webBrowser.DocumentText, "<input.+?name=\"companyId\".+?value=\"(.+?)\"");

                    companyId = Convert.ToInt32(match.Result("$1"));
                }

                Task.Run(() =>
                {
                    MatchCollection matchs = null;

                    this.Invoke((MethodInvoker)delegate
                    {
                        matchs = Regex.Matches(webBrowser.DocumentText, "rg=\"\\d+\".+?RedirectToRd/(.+?)','(.+?)','(.+?)',this");
                    });

                    foreach (Match matchResult in matchs)
                    {
                        var resumeCache = business.GetResumeOfCache(matchResult.Result("$1").Substring(0, 10));

                        if (resumeCache != null)
                        {
                            if (searchResume.SearchResumeId.Substring(2, 8) == match.Result("$1").Substring(1))
                            {
                                var model = new ResumeMatchResult
                                {
                                    Cellphone = searchResume.Cellphone,
                                    Email = searchResume.Email,
                                    ModifyTime = resumeCache.ModifyTime,
                                    Name = resumeCache.Name,
                                    ResumeId = resumeCache.ResumeId,
                                    ResumeNumber = resumeCache.ResumeNumber,
                                    UserExtId = searchResume.SearchResumeId,
                                    UserId = resumeCache.UserId,
                                    CompanyId = companyId
                                };

                                business.SaveMatchedCache(model);

                                business.ChangeResumeStatus(searchResume.SearchResumeId, true);

                                isListCompleted = true;

                                searchResume.IsMatched = true;

                                successCount++;

                                isDetailCompleted = true;
                            }
                        }
                        else
                        {
                            //Thread.Sleep(2000);

                            var detailUrl = $"https://rd.zhaopin.com/resumepreview/resume/viewone/2/{matchResult.Result("$1")}&t={matchResult.Result("$2")}&k={matchResult.Result("$3")}&v=";

                            isDetailCompleted = false;

                            webBrowser.Navigate(detailUrl);

                            SpinWait.SpinUntil(() => isDetailCompleted, TimeSpan.FromSeconds(30));

                            isDetailCompleted = false;
                        }

                        if (isListCompleted) break;
                    }

                    isListCompleted = true;
                });

            }

            if (e.Url.AbsoluteUri.Contains("https://rd.zhaopin.com/resumepreview/resume/viewone"))
            {
                var match = Regex.Match(webBrowser.DocumentText, "<input.+?resumeUserId.+?value=\"(.+?)\"");

                if (!match.Success)
                {
                    isDetailCompleted = true;

                    return;
                }

                var savePath = $@"D:\Badoucai\OldZhaopinResume\{DateTime.Now:yyyyMMdd}";

                if (!Directory.Exists(savePath))
                {
                    Directory.CreateDirectory(savePath);
                }

                var resumeNumber = Regex.Match(webBrowser.DocumentText, "<input.+?name=\"extId\".+?value=\"(.+?)\"").Result("$1").Substring(0, 10);

                var model = new ResumeMatchResult
                {
                    Cellphone = searchResume.Cellphone,
                    Email = searchResume.Email,
                    ModifyTime = DateTime.Parse(Regex.Match(webBrowser.DocumentText, "resumeUpdateTime\">(.+?)</strong>").Result("$1")),
                    Name = Regex.Match(webBrowser.DocumentText, "<input.+?name=\"tt_username\".+?value=\"(.+?)\"").Result("$1"),
                    ResumeId = Convert.ToInt32(Regex.Match(webBrowser.DocumentText, "<input.+?name=\"resume_id\".+?value=\"(.+?)\"").Result("$1")),
                    ResumeNumber = resumeNumber,
                    UserExtId = searchResume.SearchResumeId,
                    UserId = Convert.ToInt32(Regex.Match(webBrowser.DocumentText, "<input.+?name=\"resumeUserId\".+?value=\"(.+?)\"").Result("$1")),
                    Path = $@"{savePath}\{resumeNumber}.txt",
                    CompanyId = companyId
                };

                File.WriteAllText($@"{savePath}\{resumeNumber}.txt", this.webBrowser.DocumentText);

                UploadResumeToOss($@"{savePath}\{resumeNumber}.txt");

                if (searchResume.SearchResumeId.Substring(2, 8) == match.Result("$1").Substring(1))
                {
                    business.SaveMatchedCache(model);

                    business.ChangeResumeStatus(searchResume.SearchResumeId, true);

                    isListCompleted = true;

                    searchResume.IsMatched = true;

                    successCount++;
                }
                else
                {
                    model.Cellphone = null;

                    model.Email = null;

                    model.UserExtId = null;

                    business.SaveMatchedCache(model);
                }

                isDetailCompleted = true;
            }
        }

        private void btn_Start_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (!resumeQueue.IsEmpty)
                    {
                        Thread.Sleep(1000);

                        continue;
                    }

                    var list = business.GetOldResumes();

                    list.ForEach(f =>
                    {
                        resumeQueue.Enqueue(f);
                    });
                }
            });

            Task.Run(() =>
            {
                this.SearchHandler();
            });

            this.timer.Start();

            this.btn_Start.Enabled = false;
        }

        private void SearchHandler()
        {
            isStop = false;

            while (true)
            {
                if (isStop)
                {
                    Thread.Sleep(1000);

                    continue;
                }

                if(!resumeQueue.TryDequeue(out searchResume)) continue;

                foreach (var companyName in searchResume.Companys)
                {
                    //Thread.Sleep(2000);

                    if (!webBrowser.Url.AbsoluteUri.Contains("https://rdsearch.zhaopin.com/Home/ResultForCustom"))
                    {
                        isSearchCompleted = false;

                        this.Invoke((MethodInvoker)delegate
                        {
                            webBrowser.Navigate("https://rdsearch.zhaopin.com/Home/SearchByCustom?source=rd");
                        });

                        var times = 0;

                        while (true)
                        {
                            times++;

                            Thread.Sleep(100);

                            if (times > 100)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    webBrowser.Navigate("https://rdsearch.zhaopin.com/Home/SearchByCustom?source=rd");
                                });

                                times = 0;
                            }

                            if (isSearchCompleted) break;
                        }

                        SpinWait.SpinUntil(() => isSearchCompleted);

                        isSearchCompleted = false;
                    }

                    this.Invoke((MethodInvoker)delegate
                    {
                        var htmlEle = webBrowser.Document?.GetElementById("SF_1_1_25");

                        if (htmlEle == null) return;

                        htmlEle.InnerText = companyName;

                        if (searchResume.Gender == "男")
                        {
                            htmlEle = webBrowser.Document?.GetElementById("SF_1_1_9_1");
                        }
                        else
                        {
                            htmlEle = webBrowser.Document?.GetElementById("SF_1_1_9_2");
                        }

                        if (htmlEle == null) return;

                        htmlEle.SetAttribute("checked", "checked");

                        htmlEle = this.webBrowser.Document?.GetElementById("searchSubmit")?.Children[0];

                        if (htmlEle == null) return;

                        Thread.Sleep(500);

                        isListCompleted = false;

                        htmlEle.InvokeMember("click");
                    });

                    searchCount++;

                    //while (true)
                    //{
                    //    Thread.Sleep(100);

                    //    if(isListCompleted) break;
                    //}

                    SpinWait.SpinUntil(() => isListCompleted, TimeSpan.FromMinutes(1));

                    isListCompleted = false;

                    if (searchResume.IsMatched) break;
                }

                if (!searchResume.IsMatched) this.business.ChangeResumeStatus(searchResume.SearchResumeId, false);
            }
        }
        private static int createSubAccount = 0;

        private void btn_CreateSubAccount_Click(object sender, EventArgs e)
        {
            //this.webBrowser.Document?.GetElementById("loginname")?.SetAttribute("value", userName);

            //this.webBrowser.Document?.GetElementById("password")?.SetAttribute("value", password);

            createSubAccount++;

            userName = "qhzl_" + createSubAccount.ToString("D3");

            password = "PVLy5rT5";

            this.webBrowser.Document?.GetElementsByTagName("input")["passportname"]?.SetAttribute("value", userName);
            this.webBrowser.Document?.GetElementsByTagName("input")["staffname"]?.SetAttribute("value", userName);
            this.webBrowser.Document?.GetElementsByTagName("input")["email"]?.SetAttribute("value", "3511354@qq.com");
            this.webBrowser.Document?.GetElementsByTagName("input")["password"]?.SetAttribute("value", password);
            this.webBrowser.Document?.GetElementsByTagName("input")["rePassword"]?.SetAttribute("value", password);
            this.webBrowser.Document?.GetElementsByTagName("input")["staff-role"]?.SetAttribute("value", "admin");

            //this.webBrowser.Document?.GetElementById("loginname")?.SetAttribute("value", userName);

            //this.webBrowser.Document?.GetElementsByTagName("input")["password"]?.SetAttribute("value", password);

            //this.webBrowser.Document?.GetElementsByTagName("input")["passwordconfirm"]?.SetAttribute("value", password);

            //this.webBrowser.Document?.GetElementsByTagName("input")["email1"]?.SetAttribute("value", "12528428@qq.com");

            //this.webBrowser.Document?.GetElementById("29820471-checkbox")?.SetAttribute("checked", "checked");

            //var submitEle = this.webBrowser.Document?.GetElementsByTagName("input")["submit"];

            //if (submitEle == null) return;

            //submitEle.InvokeMember("click");
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            this.lbl_SearchCount.Text = "搜索次数：" + searchCount;

            this.lbl_SuccessCount.Text = "匹配成功数：" + successCount;
        }

        private void btn_Connecting_Click(object sender, EventArgs e)
        {
            this.RunAsync(StartSocketClient);

            Task.Run(() =>
            {
                try
                {
                    ListenerServer();
                }
                catch (Exception)
                {
                    this.btn_Connecting.Enabled = true;

                    isStop = true;

                    // ignored
                }
            });
        }

        private void StartSocketClient()
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

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromMinutes(2));

                    try
                    {
                        socket.Send(new HeartbeatPackage().Serialize());
                    }
                    catch (Exception)
                    {
                        this.RunInMainthread(() =>
                        {
                            this.btn_Start.Enabled = false;

                            this.btn_Connecting.Enabled = true;
                        });

                        this.socket.Dispose();

                        return;
                    }
                }
            });

            try
            {
                var accountBytes = Encoding.UTF8.GetBytes(userName);

                var passwordBytes = Encoding.UTF8.GetBytes(password);

                socket.Send(new CleaningProcedurePackage
                {
                    Id = 0x04,
                    Length = (byte)(7 + accountBytes.Length + passwordBytes.Length),
                    CleaningId = Convert.ToInt16(ConfigurationManager.AppSettings["CleaningId"]),
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

                    this.btn_Connecting.Enabled = true;
                });

                this.socket.Dispose();

                return;
            }

            this.RunInMainthread(() =>
            {
                this.btn_Connecting.Text = "已连接";

                this.btn_Connecting.Enabled = false;

                this.btn_Start.Enabled = true;
            });
        }

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

        private void ListenerServer()
        {
            var bytes = new byte[1024];

            while (true)
            {
                if (this.socket.Available == 0)
                {
                    Thread.Sleep(100);

                    continue;
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
                            checkResult = true;
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
        //[DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
        //private static extern int UrlMkSetSessionOption(int dwOption, string pBuffer, int dwBufferLength, int dwReserved);
        //const int URLMON_OPTION_USERAGENT = 0x10000001;
        ///// <summary> 
        ///// 修改UserAgent 
        ///// </summary> 
        //public static void ChangeUserAgent(string userAgent)
        //{
        //    UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, userAgent, userAgent.Length, 0);
        //}

        private void SearchResumeForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            FiddlerApplication.Shutdown();
        }
    }
}
