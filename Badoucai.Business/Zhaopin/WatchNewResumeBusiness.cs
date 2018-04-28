using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Badoucai.Business.Zhaopin
{
    public class WatchNewResumeBusiness
    {
        private static readonly object lockObj = new object();

        public void Watch()
        {
            var business = new WatchOldResumeBusiness();

            var cookieQueue = new ConcurrentQueue<KeyValuePair<int, CookieContainer>>();

            var companyDic = new ConcurrentDictionary<int, int>();

            using (var db = new MangningXssDBEntities())
            {
                var companyArr = db.ZhaoPinCompany.Where(w => w.Source.Contains("MANUAL")).Select(s => s.Id).ToArray();

                var paramArr = db.ZhaopinStaff.Where(w => companyArr.Any(a => a == w.CompanyId) && !string.IsNullOrEmpty(w.Cookie)).Select(s => new { s.CompanyId, s.Cookie }).ToArray();

                foreach (var item in paramArr)
                {
                    cookieQueue.Enqueue(new KeyValuePair<int, CookieContainer>(item.CompanyId, item.Cookie.Serialize(".zhaopin.com")));
                }
            }

            Console.WriteLine($"已获取 Cookie 数 => {cookieQueue.Count}");

            var degreesDic = new Dictionary<int, int>
            {
                { 1, 9 },
                { 2, 13 },
                { 3, 7 },
                { 4, 12 },
                { 5, 5 },
                { 7, 4 },
                { 9, 3 },
                { 11, 10 },
                { 13, 11 },
                { 15, 1 }
            };

            for (var j = 0; j < 4; j++)
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        KeyValuePair<int, CookieContainer> temp;

                        if (!cookieQueue.TryDequeue(out temp))
                        {
                            Thread.Sleep(1000);

                            continue;
                        }

                        companyDic.TryAdd(temp.Key, 0);

                        while (true)
                        {
                            var condition = GetSingleCondition(temp.Key.ToString());

                            if (condition == null)
                            {
                                Console.WriteLine($"CompanyId => {temp.Key} 找不到可搜索的条件！");

                                Thread.Sleep(1000);

                                continue;
                            }

                            var pageIndex = 0;

                            var pageTotal = 1;

                            var total = 0;

                            var workYears = condition.WorkYears == 0 ? "无" : $"{condition.WorkYears}年";

                            var isbreak = false;

                            while (pageIndex < pageTotal)
                            {
                                //var paramDictionary = new Dictionary<string, string>
                                //{
                                //    { "keywords", "的" },
                                //    { "startNum", $"{pageIndex * 100}" },
                                //    { "rowsCount", "100" },
                                //    { "sortColumnName", "sortUpDate" },
                                //    { "sortColumn", "sortUpDate desc" },
                                //    { "onlyHasImg", "false" },
                                //    { "anyKeyWord", "false" },
                                //    { "sex", $"{condition.Gender}" },
                                //    { "companyName", "" },
                                //    { "onlyLastWork", "false" },
                                //    { "ageStart", $"{condition.Age}" },
                                //    { "ageEnd",$"{condition.Age}" },
                                //    { "workYears",$"{condition.WorkYears}" },
                                //    { "edu",$"{degreesDic[condition.Degrees]}" },
                                //    { "upDate","7" }
                                //};

                                var param = $"keywords=%E7%9A%84&startNum={pageIndex * 100}&rowsCount=100&resumeGrade=&sortColumnName=sortUpDate&sortColumn=sortAge+asc&onlyHasImg=false&anyKeyWord=false&hopeWorkCity=&ageStart={condition.Age}&ageEnd={condition.Age}&workYears={condition.WorkYears}&liveCity=&sex={condition.Gender}&edu={degreesDic[condition.Degrees]}&upDate=3&companyName=&jobType=&desiredJobType=&industry=&desiredIndustry=&careerStatus=&desiredSalary=&langSkill=&hukouCity=&major=&onlyLastWork=false";

                                var requestResult = RequestFactory.QueryRequest("https://ihr.zhaopin.com/resumesearch/search.do", param, RequestEnum.POST, temp.Value);

                                //var requestResult = HttpClientFactory.RequestForString("https://ihr.zhaopin.com/resumesearch/search.do", HttpMethod.Post, paramDictionary, temp.Value);

                                if (!requestResult.IsSuccess)
                                {
                                    LogFactory.Warn($"CompanyId => {temp.Key} 条件搜索异常！异常原因=>{requestResult.ErrorMsg} CompanyId=>{temp.Key} Condition=>{JsonConvert.SerializeObject(condition)}");

                                    continue;
                                }

                                var jObject = JsonConvert.DeserializeObject(requestResult.Data) as JObject;

                                if (jObject == null)
                                {
                                    LogFactory.Warn($"CompanyId => {temp.Key} 条件搜索异常！返回页面解析异常！{requestResult.Data}");

                                    continue;
                                }

                                if (jObject["code"] != null)
                                {
                                    LogFactory.Warn($"CompanyId => {temp.Key} 搜索异常 异常信息：{(string)jObject["message"]}");

                                    isbreak = true;

                                    break;
                                }

                                if (pageTotal == 1)
                                {
                                    total = (int)jObject["numFound"];

                                    pageTotal = Convert.ToInt32(Math.Ceiling(total / 100.0));

                                    if (pageTotal > 40) pageTotal = 40;

                                    Console.WriteLine($"CompanyId=> {temp.Key} 当前条件 共{pageTotal}页 {total} 个结果");
                                }

                                pageIndex++;

                                Console.WriteLine($"CompanyId=> {temp.Key} 第 {pageIndex} 页  共 {pageTotal} 页");

                                var resumes = (JArray)jObject["results"];

                                var resumeList = resumes.Where(w => (string)w["workYearsName"] == workYears && (int)w["eduLevel"]["code"] == degreesDic[condition.Degrees]).ToList();

                                var count = resumeList.Count;

                                var index = 0;

                                foreach (var item in resumeList)
                                {
                                    var number = ((string)item["number"]).Substring(0, 10);

                                    if (business.QueryResumeIsExists(number))
                                    {
                                        Console.WriteLine($"CompanyId => {temp.Key} 简历 {number} 已看过！{++index}/{count}");

                                        continue;
                                    }

                                    requestResult = RequestFactory.QueryRequest($"http://ihr.zhaopin.com/resumesearch/getresumedetial.do?resumeNo={(string)item["id"]}_1&resumeSource=1&key=&{(string)item["valResumeTimeStr"]}", cookieContainer: temp.Value);

                                    if (!requestResult.IsSuccess)
                                    {
                                        LogFactory.Warn($"CompanyId => {temp.Key} 简历详情查看异常！异常原因=>{requestResult.ErrorMsg} ResumeNumber=>{number}");

                                        continue;
                                    }

                                    var jsonObj = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                                    if ((int)jsonObj["code"] != 1)
                                    {
                                        LogFactory.Warn($"CompanyId => {temp.Key} ResumeNumber => {number} 查看详情异常 信息：{(string)jsonObj["message"]}");

                                        if (((string)jsonObj["message"]).Contains("当日查看简历已达上限"))
                                        {
                                            isbreak = true;

                                            break;
                                        }

                                        continue;
                                    }

                                    var resumeData = jsonObj.data;

                                    var resumeDetail = JsonConvert.DeserializeObject(resumeData.detialJSonStr.ToString());

                                    var resumeId = resumeData.resumeId != null ? (int)resumeData.resumeId : resumeDetail.ResumeId != null ? (int)resumeDetail.ResumeId : 0;

                                    Console.WriteLine($"CompanyId => {temp.Key} 查看简历成功！查看简历份数：{++companyDic[temp.Key]} ResumeNumber => {number} {++index}/{count}");

                                    lock (lockObj)
                                    {
                                        using (var db = new MangningXssDBEntities())
                                        {
                                            var watched = db.ZhaopinWatchedResume.FirstOrDefault(f => f.Id == resumeId);

                                            if (watched == null)
                                            {
                                                db.ZhaopinWatchedResume.Add(new ZhaopinWatchedResume
                                                {
                                                    Id = resumeId,
                                                    ResumeNumber = number,
                                                    CompanyId = temp.Key,
                                                    WatchTime = DateTime.UtcNow
                                                });
                                            }

                                            db.SaveChanges();
                                        }
                                    }
                                }

                                if(isbreak) break;
                            }

                            if (isbreak) break;

                            business.SetSearchStatus(condition.Id, 1, total);
                        }
                    }
                });
            }
        }

        public ZhaopinSearchCondition GetSingleCondition(string accuont)
        {
            using (var db = new MangningXssDBEntities())
            {
                var condition = db.ZhaopinSearchCondition.FirstOrDefault(f => f.HandlerAccuont == accuont && f.Status == 0);

                if (condition != null) return condition;

                db.Database.ExecuteSqlCommand("UPDATE XSS_Zhaopin_SearchCondition SET HandlerAccuont = @accuont WHERE Status = 0 AND HandlerAccuont IS NULL LIMIT 1", new MySqlParameter("@accuont", accuont));

                return db.ZhaopinSearchCondition.FirstOrDefault(f => f.HandlerAccuont == accuont && f.Status == 0);
            }
        }
    }
}