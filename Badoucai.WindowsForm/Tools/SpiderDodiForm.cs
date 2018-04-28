using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.Migrations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Badoucai.Business.Dodi;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using HtmlAgilityPack;

namespace Badoucai.WindowsForm.Tools
{
    public partial class SpiderDodiForm : Form
    {
        public SpiderDodiForm()
        {
            InitializeComponent();
        }

        private void btn_Download_Click(object sender, EventArgs e)
        {
            this.btn_Download.Text = "正在下载...";

            this.btn_Download.Enabled = false;

            RunAsync(this.DownloadBusiness);

            //RunAsync(this.DownloadAnonymousResumes);
        }

        public void DownloadAnonymousResumes()
        {
            //const string cookie = "PHPSESSID=cl79f305e8fv4kfq5eijcf3376; Hm_lvt_3c8ecbfa472e76b0340d7a701a04197e=1509065458,1509099221,1509324837,1509411160; Hm_lpvt_3c8ecbfa472e76b0340d7a701a04197e=1509427603; Hm_lvt_407473d433e871de861cf818aa1405a1=1509065458,1509099221,1509324837,1509411160; Hm_lpvt_407473d433e871de861cf818aa1405a1=1509427603; think_language=zh-cn";
            const string cookie = "UM_distinctid =15cf6de66fd4fc-0e30531a6-4349052c-13c680-15cf6de66fe4e6; Hm_lvt_e1afad506a9557a8f31d1de1999fcd1a=1498790390; 58tj_uuid=02d03c66-977d-44ab-8029-1bee93cbd6b6; new_uv=1; als=0; PHPSESSID=4r9h0edodfm96ttb032od6q8k2; Example_auth=09fdPd2UmwZG%2BnjYqr0CL%2FCKLkFCYXqSs7tPUqs9pswDpjwzf1FPP32GVy1y; Hm_lvt_1360b6fe7fa346ff51189adc58afb874=1507432911,1507510849,1507684063,1507856431; Hm_lpvt_1360b6fe7fa346ff51189adc58afb874=1507882066";

            var cookieContainer = cookie.Serialize("120.77.152.11");

            var queue = new ConcurrentQueue<int>();

            Task.Run(() =>
            {
                for (var i = 1; i < 1; i++)
                {
                    var response = HttpClientFactory.RequestForString($"http://120.77.152.11/index.php?m=leads&listrows=100&p={i}", HttpMethod.Get, null, cookieContainer);

                    if (!response.IsSuccess)
                    {
                        RunInMainthread(() =>
                        {
                            Program.SetLog(this.tbx_Log, $"请求失败！{response.ErrorMsg}");
                        });

                        continue;
                    }

                    var matchs = Regex.Matches(response.Data, "<a href=\"/index\\.php\\?m=leads&a=view&id=(\\d+)\">");

                    foreach (Match match in matchs)
                    {
                        queue.Enqueue(Convert.ToInt32(match.Result("$1")));
                    }
                }
            });

            var taskList = new List<Task>();

            for (var j = 0; j < 1; j++)
            {
                taskList.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        int i;

                        if (!queue.TryDequeue(out i)) continue;

                        var response = HttpClientFactory.RequestForString($"http://120.77.152.11/index.php?m=leads&a=view&id={i}", HttpMethod.Get, null, cookieContainer);

                        if (!response.IsSuccess)
                        {
                            RunInMainthread(() =>
                            {
                                Program.SetLog(this.tbx_Log, $"ID:{i} 请求失败！{response.ErrorMsg}");
                            });

                            LogFactory.Warn($"ID:{i} 请求失败！{response.ErrorMsg}");

                            continue;
                        }

                        const string path = @"D:\Badoucai\AnonymousResumes";

                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        File.WriteAllText($@"{path}\Mian_{i}.txt", response.Data);

                        RunInMainthread(() =>
                        {
                            Program.SetLog(this.tbx_Log, $"ID:{i} 下载成功！{response.ErrorMsg}");
                        });
                    }
                }));
            }

            Task.WaitAll(taskList.ToArray());
        }

        public void DownloadBusiness()
        {
            const string cookie = "PHPSESSID=j0lklef94l9akqabg41n3nqd93; Example_auth=f9d7XYivszgUGkEXygbytRrg8EzZWngyS25FZaKx1OSub%2FhBVliH; Hm_lvt_1360b6fe7fa346ff51189adc58afb874=1507336367,1507510768,1507596684,1507682510; Hm_lpvt_1360b6fe7fa346ff51189adc58afb874=1507705480";

            //const string cookie = "UM_distinctid =15cf6de66fd4fc-0e30531a6-4349052c-13c680-15cf6de66fe4e6; Hm_lvt_e1afad506a9557a8f31d1de1999fcd1a=1498790390; 58tj_uuid=02d03c66-977d-44ab-8029-1bee93cbd6b6; new_uv=1; als=0; PHPSESSID=4r9h0edodfm96ttb032od6q8k2; Example_auth=09fdPd2UmwZG%2BnjYqr0CL%2FCKLkFCYXqSs7tPUqs9pswDpjwzf1FPP32GVy1y; Hm_lvt_1360b6fe7fa346ff51189adc58afb874=1507432911,1507510849,1507684063,1507856431; Hm_lpvt_1360b6fe7fa346ff51189adc58afb874=1507882066";

            var queue = new ConcurrentQueue<int>();

            //2329979 2366914 2338074 

            Task.Run(() =>
            {
                if (!cbx_UpdateBySZ.Checked)
                {
                    int maxId;

                    using (var db = new MangningXssDBEntities())
                    {
                        maxId = db.DodiBusiness.Max(x => x.Id) + 1;
                    }

                    var maxIdTemp = maxId + 300000;

                    for (var i = maxId; i < maxIdTemp; i++)
                    {
                        queue.Enqueue(i);
                    }
                }
                else
                {
                    using (var db = new MangningXssDBEntities())
                    {
                        var query = from a in db.DodiBusiness
                                    join b in db.DodiUserInfomation on a.Id equals b.BusinessId
                                    where a.BranchOffice.Contains("深圳") && a.CreateTime > new DateTime(2017, 10, 24) && !b.IsPost
                                    select a.Id;

                        var businessIdArr = query.ToArray();

                        foreach (var id in businessIdArr)
                        {
                            queue.Enqueue(id);
                        }
                    }
                }
            });

            var cookieContainer = cookie.Serialize("crm.dodi.cn");

            HttpClientFactory.RequestForString($"http://crm.dodi.cn/index.php/Notice/index", HttpMethod.Get, null, cookieContainer);

            var taskList = new List<Task>();

            for (var j = 0; j < 1; j++)
            {
                taskList.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        int i;

                        if (!queue.TryDequeue(out i)) continue;

                        var response = HttpClientFactory.RequestForString($"http://crm.dodi.cn/index.php/Main/khxxy/business_id/{i}/source/false_note", HttpMethod.Get, null, cookieContainer);

                        if (!response.IsSuccess)
                        {
                            RunInMainthread(() =>
                            {
                                Program.SetLog(this.tbx_Log, $"ID:{i} 请求失败！{response.ErrorMsg}");
                            });

                            LogFactory.Warn($"ID:{i} 请求失败！{response.ErrorMsg}");

                            continue;
                        }

                        if (!response.Data.Contains("商 机 ID："))
                        {
                            RunInMainthread(() =>
                            {
                                Program.SetLog(this.tbx_Log, $"ID:{i} 商机为空！{response.ErrorMsg}");
                            });

                            LogFactory.Warn($"ID:{i} 商机为空！{response.ErrorMsg}");

                            continue;

                            //break;
                        }

                        var path = $@"D:\Badoucai\Dodi\{i.ToString().Substring(0, 4)}\{i}";

                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                        File.WriteAllText($@"{path}\Mian_{i}.txt", response.Data);

                        RunInMainthread(() =>
                        {
                            Program.SetLog(this.tbx_Log, $"ID:{i} 下载成功！{response.ErrorMsg}");
                        });

                        Thread.Sleep(500);
                    }
                }));
            }

            Task.WaitAll(taskList.ToArray());

            RunInMainthread(() =>
            {
                this.btn_Download.Text = "下载完成";

                this.btn_Download.Enabled = true;
            });
        }

        private void btn_Warehousing_Click(object sender, EventArgs e)
        {
            this.btn_Warehousing.Text = "正在导入...";

            RunAsync(this.ImportDodiInfo);

            this.btn_Warehousing.Text = "导入完成";
        }

        private void ImportDodiInfo()
        {
            var businessManagement = new BusinessManagement();

            //var document = new HtmlAgilityPack.HtmlDocument();

            //document.LoadHtml(File.ReadAllText(@"D:\Badoucai\Dodi\2363\2363038\Mian_2363038.txt"));

            //var userInfo = businessManagement.FormatUserInfomation(document);

            //var business = businessManagement.FormatBusiness(document);

            var pathQueue = new ConcurrentQueue<string>();

            var filePathQueue = new ConcurrentQueue<string>();

            var count = 0;

            var index = 0;

            var pathList = Directory.GetDirectories(@"D:\Badoucai\Dodi\").ToList();

            //pathList.Reverse();

            pathList.ForEach(t => pathQueue.Enqueue(t));

            Task.Run(() =>
            {
                while (true)
                {
                    if (filePathQueue.Count > 1000) continue;

                    string path;

                    if (!pathQueue.TryDequeue(out path)) continue;

                    var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

                    count += files.Length;

                    files.ToList().ForEach(t => filePathQueue.Enqueue(t));
                }
            });

            for (var i = 0; i < 16; i++)
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        string path;

                        if (!filePathQueue.TryDequeue(out path)) continue;

                        try
                        {
                            var document = new HtmlAgilityPack.HtmlDocument();

                            document.LoadHtml(File.ReadAllText(path));

                            try
                            {
                                var userInfo = businessManagement.FormatUserInfomation(document);

                                var business = businessManagement.FormatBusiness(document);

                                using (var db = new MangningXssDBEntities())
                                {
                                    db.DodiUserInfomation.AddOrUpdate(a => a.Id, userInfo);

                                    db.DodiBusiness.AddOrUpdate(a => a.Id, business);

                                    try
                                    {
                                        db.TransactionSaveChanges();
                                    }
                                    catch (Exception ex)
                                    {
                                        while (true)
                                        {
                                            if (ex.InnerException == null) break;

                                            ex = ex.InnerException;
                                        }

                                        Program.SetLog(this.tbx_Log, $"多迪信息SaveChanges异常！异常文件路径：{path}, {ex.Message}");

                                        LogFactory.Warn($"多迪信息SaveChanges异常！异常文件路径：{path}, {ex.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                continue;
                            }

                            var indexTemp = Interlocked.Increment(ref index);

                            var destPath = path.Replace("Dodi", "Dodi-Success");

                            var destDirectoryPath = Path.GetDirectoryName(destPath);

                            if (!string.IsNullOrEmpty(destDirectoryPath) && !Directory.Exists(destDirectoryPath))
                            {
                                Directory.CreateDirectory(destDirectoryPath);
                            }

                            if(File.Exists(destPath)) File.Delete(destPath);

                            File.Move(path, destPath);

                            RunInMainthread(() =>
                            {
                                Program.SetLog(this.tbx_Log, $"导入成功！进度：{indexTemp}/{count} {Path.GetFileNameWithoutExtension(path)}");
                            });
                        }
                        catch (Exception ex)
                        {
                            RunInMainthread(() =>
                            {
                                Program.SetLog(this.tbx_Log, $"多迪信息导入异常！异常文件路径：{path}, {ex.Message}");
                            });

                            LogFactory.Warn($"多迪信息导入异常！异常文件路径：{path}, {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                        }
                    }
                });
            }
        }

        private void btn_SpiderDetail_Click(object sender, EventArgs e)
        {
            RunAsync(this.ExportCD);
        }

        private void ExportCD()
        {
            var cellphones = File.ReadAllLines(@"D:\给过的手机号-副本.txt");

            using (var db = new MangningXssDBEntities())
            {
                var beginDataTime = DateTime.Today.AddDays(-4);

                var endDataTime = DateTime.Today.AddDays(1);

                var dataList = db.DodiBusiness
                    .Join(db.DodiUserInfomation, a => a.Id, b => b.BusinessId, (B, A) => new { B.Id, A.UserName, A.GraduatedSchool, B.BranchOffice, A.Email, A.Cellphone, B.CreateTime, B.Sources, A.JobName, B.PromoteBrand, A.ProfessionalTitle })
                    .Where(w => /*!w.BranchOffice.StartsWith("北京") && !w.BranchOffice.StartsWith("上海") && !w.BranchOffice.StartsWith("广州") && */ w.BranchOffice.StartsWith("成都") && w.CreateTime > beginDataTime && w.CreateTime < endDataTime)
                    .Select(s=>new { s.Id, s.UserName ,s.Cellphone, s.Email,s.Sources, s.CreateTime, s.JobName, s.ProfessionalTitle })
                    .ToList();

                const string cookie = "PHPSESSID=j0lklef94l9akqabg41n3nqd93; Example_auth=f9d7XYivszgUGkEXygbytRrg8EzZWngyS25FZaKx1OSub%2FhBVliH; Hm_lvt_1360b6fe7fa346ff51189adc58afb874=1507336367,1507510768,1507596684,1507682510; Hm_lpvt_1360b6fe7fa346ff51189adc58afb874=1507705480";

                var cookieContainer = cookie.Serialize("crm.dodi.cn");

                HttpClientFactory.RequestForString("http://crm.dodi.cn/index.php/Notice/index", HttpMethod.Get, null, cookieContainer);

                var sb = new StringBuilder();

                sb.AppendLine("姓名\t手机\t邮箱\t年龄\t学历\t更新日期\t性别\t平台\t地点\t职位\t专业");

                var index = 0;

                foreach (var item in dataList)
                {
                     var response = HttpClientFactory.RequestForString($"http://crm.dodi.cn/index.php/Main/khxxy/business_id/{item.Id}/source/false_note", HttpMethod.Get, null, cookieContainer);

                    if (!response.IsSuccess)
                    {
                        RunInMainthread(() =>
                        {
                            Program.SetLog(this.tbx_Log, $"ID:{item.Id} 请求失败！{response.ErrorMsg}");
                        });

                        LogFactory.Warn($"ID:{item.Id} 请求失败！{response.ErrorMsg}");

                        continue;
                    }

                    if (!response.Data.Contains("商 机 ID："))
                    {
                        RunInMainthread(() =>
                        {
                            Program.SetLog(this.tbx_Log, $"ID:{item.Id} 商机为空！{response.ErrorMsg}");
                        });

                        LogFactory.Warn($"ID:{item.Id} 商机为空！{response.ErrorMsg}");

                        continue;
                    }

                    var match = Regex.Match(response.Data, @"resume_email\('(.*?)','(\d+)','(\d+)','(\d+)',(\d+)\)");

                    if (!match.Success)
                    {
                        RunInMainthread(() =>
                        {
                            Program.SetLog(this.tbx_Log, $"ID:{item.Id} 匹配详情失败！");
                        });

                        LogFactory.Warn($"ID:{item.Id} 匹配详情失败！");

                        continue;
                    }

                    var email = HttpUtility.UrlEncode(match.Result("$1"));
                    var phone = HttpUtility.UrlEncode(match.Result("$2"));
                    var email_id = HttpUtility.UrlEncode(match.Result("$3"));
                    var now_month = HttpUtility.UrlEncode(match.Result("$4"));
                    var school_id = HttpUtility.UrlEncode(match.Result("$5"));

                    if (cellphones.Contains(phone)) continue;

                    response = HttpClientFactory.RequestForString($"http://crm.dodi.cn/index.php/Main/email_body?email={email}&phone={phone}&email_id={email_id}&now_month={now_month}&school_id={school_id}", HttpMethod.Get, null, cookieContainer);

                    if (!response.IsSuccess)
                    {
                        RunInMainthread(() =>
                        {
                            Program.SetLog(this.tbx_Log, $"ID:{item.Id} 详情请求失败！{response.ErrorMsg}");
                        });

                        LogFactory.Warn($"ID:{item.Id} 详情请求失败！{response.ErrorMsg}");

                        continue;
                    }

                    //File.WriteAllText($@"D:\Business\{item.Id}.txt", response.Data);

                    var data = Regex.Unescape(response.Data);

                    var xbMatch = Regex.Match(data, "(男|女)");

                    if(xbMatch.Value.Trim() == "女") continue;

                    var ageMatch = Regex.Match(data, "((?<=[^0-9])[0-9]{2}岁|年龄\\s*[0-9]{2}(?=[^0-9]))");

                    if (!ageMatch.Success)
                    {
                        ageMatch = Regex.Match(data, "(19|20)[0-9]{2}(年|-)[0-9]{1,2}(月|)(?=[^0-9])");
                    }
                    
                    //var xlMatch = Regex.Match(data, "(高中|初中|小学|大专|中专)");

                    var xlMatch = Regex.Match(data, "(大专|本科|硕士|博士|MBA)");

                    //var date = item.CreateTime < DateTime.Today.AddDays(-3) ? item.CreateTime?.AddDays(2) : item.CreateTime;

                    sb.AppendLine($"{item.UserName}\t{item.Cellphone}\t{item.Email}\t{ageMatch.Value}\t{xlMatch.Value}\t{item.CreateTime?.ToString("yyyy-MM-dd")}\t{xbMatch.Value}\t{item.Sources.Substring(item.Sources.LastIndexOf("_", StringComparison.Ordinal) + 1)}\t成都\t{item.JobName}\t{item.ProfessionalTitle}");

                    //sb.AppendLine($"{item.Cellphone}");

                    //if (index % 1000 == 0)
                    //{
                    //    File.WriteAllText(@"D:\非北上广.txt", sb.ToString());
                    //}

                    RunInMainthread(() =>
                    {
                        Program.SetLog(this.tbx_Log, $"{Interlocked.Increment(ref index)}/{dataList.Count}");
                    });
                };

                File.WriteAllText(@"D:\成都大专以上.txt", sb.ToString());
            }
        }

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
