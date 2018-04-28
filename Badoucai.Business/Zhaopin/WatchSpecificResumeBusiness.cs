using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Newtonsoft.Json;

namespace Badoucai.Business.Zhaopin
{
    public class WatchSpecificResumeBusiness
    {
        private static readonly HashSet<string> hashSet = new HashSet<string>();

        private static readonly ConcurrentQueue<dynamic> queue = new ConcurrentQueue<dynamic>();

        public void Watch()
        {
            var cookieQueue = new ConcurrentQueue<KeyValuePair<KeyValuePair<int, string>, CookieContainer>>();

            using (var db = new MangningXssDBEntities())
            {
                var companyArr = db.ZhaoPinCompany.Where(w => w.Source.Contains("MANUAL")).Select(s => s.Id).ToArray();

                var paramArr = db.ZhaopinStaff.Where(w => companyArr.Any(a => a == w.CompanyId) && !string.IsNullOrEmpty(w.Cookie)).Select(s => new { s.CompanyId, s.Username, s.Cookie }).ToArray();

                foreach (var item in paramArr)
                {
                    cookieQueue.Enqueue(new KeyValuePair<KeyValuePair<int, string>, CookieContainer>(new KeyValuePair<int, string>(item.CompanyId, item.Username), item.Cookie.Serialize(".zhaopin.com")));
                }
            }

            using (var db = new MangningXssDBEntities())
            {
                var list = db.ZhaopinCleaningProcedure.Where(w => w.IsEnable && !string.IsNullOrEmpty(w.Cookie));

                foreach (var item in list)
                {
                    cookieQueue.Enqueue(new KeyValuePair<KeyValuePair<int, string>, CookieContainer>(new KeyValuePair<int, string>(0, item.Account), item.Cookie.Serialize(".zhaopin.com")));
                }
            }

            #region 插入查询条件

            for (var degrees = 5; degrees < 16; degrees += 2)
            {
                for (var age = 20; age <= 28; age++)
                {
                    queue.Enqueue(new
                    {
                        Degrees = degrees,
                        Age = age
                    });
                }
            }

            #endregion

            Console.WriteLine($"已获取 Cookie 数 => {cookieQueue.Count}");

            for (var j = 0; j < 4; j++)
            {
                Task.Run(() =>
                {
                    try
                    {
                        while (true)
                        {
                            KeyValuePair<KeyValuePair<int, string>, CookieContainer> temp;

                            if (!cookieQueue.TryDequeue(out temp))
                            {
                                Thread.Sleep(1000);

                                continue;
                            }

                            while (true)
                            {
                                dynamic condition;

                                if (!queue.TryDequeue(out condition))
                                {
                                    Console.WriteLine($"Company => {temp.Key.Value} 找不到可搜索的条件！");

                                    Thread.Sleep(1000);
                                    continue;
                                }

                                var pageIndex = 1;

                                var pageTotal = 1;

                                var total = 0;

                                var isbreak = false;

                                while (pageIndex < pageTotal + 1)
                                {
                                    var url = $"https://ihrsearch.zhaopin.com/Home/ResultForCustom?SF_1_1_7=1%2C9&SF_1_1_5={condition.Degrees}%2C{condition.Degrees}&SF_1_1_18=530%3B854%3B531&SF_1_1_8={condition.Age}%2C{condition.Age}&SF_1_1_27=0&orderBy=DATE_MODIFIED,1&pageSize=60&exclude=1";

                                    var requestResult = RequestFactory.QueryRequest(url, cookieContainer: temp.Value, referer: url);

                                    if (!requestResult.IsSuccess)
                                    {
                                        LogFactory.Warn($"Company => {temp.Key.Value} 条件搜索异常！异常原因=>{requestResult.ErrorMsg} Condition=>{JsonConvert.SerializeObject(condition)}");

                                        continue;
                                    }

                                    if (pageTotal == 1)
                                    {
                                        var match = Regex.Match(requestResult.Data, "(?s)<span>(\\d+)</span>份简历.+?rd-resumelist-pageNum\">1/(\\d+)</span>");

                                        if (!match.Success)
                                        {
                                            if (requestResult.Data.Contains("text/javascript\" r='m'"))
                                            {
                                                LogFactory.Warn($"Cookie 过期，过期用户 => {temp.Key.Value}");
                                            }
                                            else
                                            {
                                                LogFactory.Warn($"Company => {temp.Key.Value} 条件搜索异常！返回页面解析异常！{requestResult.Data}");
                                            }

                                            isbreak = true;

                                            break;
                                        }

                                        total = Convert.ToInt32(match.Result("$1"));

                                        pageTotal = Convert.ToInt32(match.Result("$2"));

                                        if (total == 0)
                                        {
                                            Console.WriteLine("该条件无搜索结果！");

                                            break;
                                        }
                                    }

                                    Console.WriteLine($"Company => {temp.Key.Value} 第 {pageIndex} 页  共 {pageTotal} 页 {total} 个结果");

                                    pageIndex++;

                                    var matchs = Regex.Matches(requestResult.Data, "(?s)RedirectToRd/([^\r\n]+?)','([^\r\n]+?)','([^\r\n]+?)',this\\);this.+?(\\d{2}-\\d{2}-\\d{2})");

                                    using (var db = new MangningXssDBEntities())
                                    {
                                        foreach (Match item in matchs)
                                        {
                                            var number = item.Result("$1").Substring(0, 10);

                                            var numberParam = item.Result("$1").Substring(0, item.Result("$1").IndexOf("?", StringComparison.Ordinal));

                                            if (!hashSet.Add(number)) continue;

                                            db.ZhaopinResumeNumber.Add(new ZhaopinResumeNumber { ResumeNumber = number });

                                            var resume = db.ZhaopinResume.FirstOrDefault(f => f.RandomNumber == number);

                                            if(resume == null) continue;

                                            var user = db.ZhaopinUser.FirstOrDefault(f => f.Id == resume.UserId && !string.IsNullOrEmpty(f.Cellphone));

                                            if(user == null) continue;

                                            requestResult = RequestFactory.QueryRequest($"http://ihr.zhaopin.com/resumesearch/getresumedetial.do?resumeNo={numberParam}&searchresume=1&resumeSource=1&keyword=%E8%AF%B7%E8%BE%93%E5%85%A5%E7%AE%80%E5%8E%86%E5%85%B3%E9%94%AE%E8%AF%8D%EF%BC%8C%E5%A4%9A%E5%85%B3%E9%94%AE%E8%AF%8D%E5%8F%AF%E7%94%A8%E7%A9%BA%E6%A0%BC%E5%88%86%E9%9A%94&t={item.Result("$2")}&k={item.Result("$3")}&v=undefined&version=3&openFrom=1", cookieContainer: temp.Value);

                                            if (!requestResult.IsSuccess)
                                            {
                                                LogFactory.Warn($"Company => {temp.Key.Value} 简历详情查看异常！异常原因=>{requestResult.ErrorMsg} ResumeNumber=>{number}");

                                                continue;
                                            }

                                            try
                                            {
                                                var jsonObj = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                                                if ((int)jsonObj["code"] != 1)
                                                {
                                                    LogFactory.Warn($"Company => {temp.Key.Value} ResumeNumber => {number} 查看详情异常 信息：{(string)jsonObj["message"]}");

                                                    if (((string)jsonObj["message"]).Contains("当日查看简历已达上限"))
                                                    {
                                                        isbreak = true;

                                                        break;
                                                    }

                                                    continue;
                                                }

                                                var resumeData = jsonObj.data;

                                                resumeData.userDetials.mobilePhone = user.Cellphone;

                                                resumeData.userDetials.email = user.Email;

                                                File.WriteAllText($@"D:\ResumeBusiness\{number}.json", JsonConvert.SerializeObject(resumeData));

                                                Console.WriteLine($"Company => {temp.Key.Value} 查看简历成功！ResumeNumber => {number}");
                                            }
                                            catch (Exception ex)
                                            {
                                                while (true)
                                                {
                                                    if (ex.InnerException == null) break;

                                                    ex = ex.InnerException;
                                                }

                                                LogFactory.Error($"保存简历信息异常！异常信息：{ex.Message} 响应信息：{requestResult.Data}");
                                            }
                                        }

                                        db.SaveChanges();
                                    }

                                    if (isbreak) break;
                                }

                                if (isbreak) break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        while (true)
                        {
                            if (ex.InnerException == null) break;

                            ex = ex.InnerException;
                        }

                        LogFactory.Error($"Watch异常！异常信息：{ex.Message}  {ex.StackTrace}");
                    }
                });
            }
        }

    }
}