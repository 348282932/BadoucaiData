using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Windows.Forms;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Newtonsoft.Json;

namespace Badoucai.WindowsForm.Zhaopin
{
    public partial class RefreshJobForm : Form
    {
        public RefreshJobForm()
        {
            InitializeComponent();
        }

        private static readonly Dictionary<string, string> jobDictionary = new Dictionary<string, string>();

        private static readonly Queue<KeyValuePair<string, string>> queue = new Queue<KeyValuePair<string, string>>();

        private static readonly int time = Convert.ToInt32(ConfigurationManager.AppSettings["Interval"]);

        private static CookieContainer cookieContainer = new CookieContainer();

        private static readonly Queue<ZhaopinStaff> staffQueue = new Queue<ZhaopinStaff>();

        private static string token = string.Empty;

        private void btn_Down_Click(object sender, EventArgs e)
        {
            this.RunAsync(DownJob);
        }

        private void DownJob()
        {
            if (string.IsNullOrEmpty(this.tbx_Cookie.Text))
            {
                this.AsyncSetLog(this.tbx_Log, "请录入 Cookie ！");

                return;
            }

            jobDictionary.Clear();

            cookieContainer = this.tbx_Cookie.Text.Serialize(".zhaopin.com");

            var request = RequestFactory.QueryRequest("https://ihr.zhaopin.com/home/getUserinfo.do", cookieContainer: cookieContainer, requestType: RequestEnum.POST);

            var jsonObj = JsonConvert.DeserializeObject<dynamic>(request.Data);

            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    lbl_CompanyName.Text = jsonObj.companyname.ToString();
                });
            }
            catch (Exception)
            {
                this.AsyncSetLog(this.tbx_Log, $"{JsonConvert.SerializeObject(jsonObj)}");

                return;
            }

            token = this.tbx_Cookie.Text.Substring(this.tbx_Cookie.Text.IndexOf("at=", StringComparison.Ordinal) + 3, 32);

            var pageIndex = 1;

            var pageCount = 0;

            do
            {
                var url = $"https://ihr.zhaopin.com/api/job/list.do?access_token={token}&jobTitle=&status=30&reviewtype=0%2C20&pageIndex={pageIndex}&pageSize=10&jobStyle=0&isJudgerefreshing=1&_={BaseFanctory.GetUnixTimestamp()}246";

                var requestResult = HttpClientFactory.RequestForString(url, HttpMethod.Get, null, cookieContainer, "https://ihr.zhaopin.com/job/");

                if (!requestResult.IsSuccess)
                {
                    this.AsyncSetLog(this.tbx_Log, requestResult.ErrorMsg);

                    return;
                }

                Thread.Sleep(time);

                var result = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                if (result == null)
                {
                    this.AsyncSetLog(this.tbx_Log, "返回信息序列化异常！返回结果：" + requestResult.Data);

                    return;
                }

                if ((int)result.code != 200)
                {
                    this.AsyncSetLog(this.tbx_Log, (string)result.message);

                    return;
                }

                if (pageCount == 0) pageCount = (int)result.data.pager.pagecount;

                foreach (var item in result.data.list)
                {
                    if (jobDictionary.ContainsKey($"{(string)item.jobTitle}_{(string)item.cityName}"))
                    {
                        this.AsyncSetLog(this.tbx_Log, $"职位名称重复！跳过该职位,重复职位：{(string)item.jobTitle}_{(string)item.cityName}");

                        continue;
                    }

                    jobDictionary.Add($"{(string)item.jobTitle}_{(string)item.cityName}", (string)item.jobNumber);
                }

                this.AsyncSetLog(this.tbx_Log, "获取职位成功！职位数：" + jobDictionary.Count);
            }
            while (++pageIndex <= pageCount);

            this.AsyncSetLog(this.tbx_Log, "开始刷新职位！职位数：" + jobDictionary.Count);

            var dictionary = new Dictionary<string, string>
            {
                { "jobNumber", string.Join(",", jobDictionary.Values) }
            };

            var jobDownlineResult = HttpClientFactory.RequestForString("http://ihr.zhaopin.com/job/jobdownline.do?access_token=" + token, HttpMethod.Post, dictionary, cookieContainer, "http://ihr.zhaopin.com/job/");

            if (!jobDownlineResult.IsSuccess)
            {
                this.AsyncSetLog(this.tbx_Log, jobDownlineResult.ErrorMsg);

                return;
            }

            Thread.Sleep(time);

            var jobDownline = JsonConvert.DeserializeObject<dynamic>(jobDownlineResult.Data);

            if (jobDownline == null)
            {
                this.AsyncSetLog(this.tbx_Log, "返回信息序列化异常！返回结果：" + jobDownlineResult.Data);

                return;
            }

            if ((int)jobDownline.code != 200)
            {
                this.AsyncSetLog(this.tbx_Log, (string)jobDownline.message);

                return;
            }

            this.AsyncSetLog(this.tbx_Log, "职位下线成功！下线职位数：" + jobDictionary.Count);
        }

        private void btn_Up_Click(object sender, EventArgs e)
        {
            this.RunAsync(UpJob);
        }

        private void UpJob()
        {
            if (!jobDictionary.Any())
            {
                this.AsyncSetLog(this.tbx_Log, "请先下架职位！");

                return;
            }

            this.AsyncSetLog(this.tbx_Log, $"开始上架 {jobDictionary.Count} 个职位！");

            foreach (var job in jobDictionary)
            {
                queue.Enqueue(job);
            }

            while (true)
            {
                if(!queue.Any()) break;

                var job = queue.Dequeue();

                var jobOnlineResult = RequestFactory.QueryRequest("https://ihr.zhaopin.com/api/job/jobonline.do?access_token=" + token, "{\"jobnumbers\":\"" + job.Value + "\"}", RequestEnum.POST, cookieContainer, "http://ihr.zhaopin.com/job/", ContentTypeEnum.Json.Description());

                if (!jobOnlineResult.IsSuccess)
                {
                    this.AsyncSetLog(this.tbx_Log, jobOnlineResult.ErrorMsg);

                    queue.Enqueue(job);

                    continue;
                }

                Thread.Sleep(time);

                try
                {
                    var jobOnline = JsonConvert.DeserializeObject<dynamic>(jobOnlineResult.Data);

                    if (jobOnline == null)
                    {
                        this.AsyncSetLog(this.tbx_Log, "返回信息序列化异常！返回结果：" + jobOnlineResult.Data);

                        queue.Enqueue(job);

                        continue;
                    }

                    if ((int)jobOnline.code != 200)
                    {
                        this.AsyncSetLog(this.tbx_Log, (string)jobOnline.message);

                        //queue.Enqueue(job);

                        continue;
                    }
                }
                catch (Exception)
                {
                    this.AsyncSetLog(this.tbx_Log, "上架职位失败！返回结果：" + jobOnlineResult.Data);

                    queue.Enqueue(job);
                }

                this.AsyncSetLog(this.tbx_Log, $"职位：{job.Key} 上架成功！");

                jobDictionary.Remove(job.Key);
            }

            this.AsyncSetLog(this.tbx_Log, "刷新职位完成！");

        }

        private void RefreshJobForm_Load(object sender, EventArgs e)
        {
            using (var db = new MangningXssDBEntities())
            {
                var staffIdArray = new[] { 705826281, 683974003, 705834336, 705675698, 700680503, 700537915, 705680163, 698198504, 707297691, 706849229, 707195303, 707603919 };

                var newStaffs = db.ZhaopinStaff.AsNoTracking().Where(w => staffIdArray.Contains(w.Id) && !string.IsNullOrEmpty(w.Cookie) && !w.Source.Contains("5.5")).ToList();

                var oldStaffs = db.ZhaopinStaff.AsNoTracking().Where(w => staffIdArray.Contains(w.Id) && !string.IsNullOrEmpty(w.Cookie) && w.Source.Contains("5.5")).ToList();

                foreach (var staff in newStaffs)
                {
                    staffQueue.Enqueue(staff);
                }

                //foreach (var staff in oldStaffs)
                //{
                //    staffQueue.Enqueue(staff);
                //}

                this.AsyncSetLog(this.tbx_Log, $"{DateTime.Now} > Get Cookies Success ! Count = {staffQueue.Count}.");
            }
        }

        private void btn_GetCookie_Click(object sender, EventArgs e)
        {
            this.RunAsync(() =>
            {
                while (true)
                {
                    if (!staffQueue.Any())
                    {
                        this.AsyncSetLog(this.tbx_Log, $"{DateTime.Now} > Cookie queue is Empty !");

                        break;
                    }

                    var staff = staffQueue.Dequeue();

                    this.Invoke((MethodInvoker)delegate
                    {
                        this.tbx_Cookie.Text = staff.Cookie;
                    });
                    
                    this.AsyncSetLog(this.tbx_Log, $"{DateTime.Now} > Get Cookies Success ! Count = {staffQueue.Count}");

                    this.DownJob();

                    this.UpJob();
                }
            });
        }
    }
}
