using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Newtonsoft.Json;

namespace Badoucai.Service
{
    public class SearchResumeByConditionThread : BaseThread
    {
        private static readonly ConcurrentQueue<ZhaopinSearchCondition> conditionQueue = new ConcurrentQueue<ZhaopinSearchCondition>();

        private static readonly ConcurrentQueue<CookieContainer> cookieQueue = new ConcurrentQueue<CookieContainer>();

        private static readonly string city = HttpUtility.UrlEncode(ConfigurationManager.AppSettings["UpdateCity"]);

        private static readonly string date = ConfigurationManager.AppSettings["UpdateDate"];

        private static readonly string keyword = HttpUtility.UrlEncode(ConfigurationManager.AppSettings["Keyword"]);

        private static readonly string position = HttpUtility.UrlEncode(ConfigurationManager.AppSettings["Position"]);

        private static readonly string industry = HttpUtility.UrlEncode(ConfigurationManager.AppSettings["Industry"]);

        private static readonly int refreshCount = Convert.ToInt32(ConfigurationManager.AppSettings["RefreshCount"]);

        private static readonly string path = ConfigurationManager.AppSettings["File.ImportPath"];

        private static readonly StringBuilder sb = new StringBuilder();

        private static int count;

        private static int totalCount;

        public override Thread Create()
        {
            return new Thread(() =>
            {
                try
                {
                    LoadCondition();

                    LoadCookies();

                    var tasks = new List<Task>();

                    for (var i = 0; i < 4; i++)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            while (true)
                            {
                                CookieContainer cookieContainer;

                                if (!cookieQueue.TryDequeue(out cookieContainer)) break;

                                SearchResumes(cookieContainer);

                                if (count >= refreshCount) break;
                            }
                        }));
                    }

                    Task.WaitAll(tasks.ToArray());

                    File.WriteAllText("UpdateResumeIdArray.txt", sb.ToString());

                    Console.WriteLine($"{DateTime.Now} > Completion ! Total = {totalCount}, Update Count = {count}.");
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            })
            {
                IsBackground = true
            };
        }

        /// <summary>
        /// 加载搜索条件
        /// </summary>
        private static void LoadCondition()
        {
            for (short gender = 1; gender <= 2; gender++)
            {
                for (short age = 18; age <= 50; age++)
                {
                    for (short degrees = 0; degrees <= 15; degrees++)
                    {
                        conditionQueue.Enqueue(new ZhaopinSearchCondition
                        {
                            Age = age,
                            Degrees = degrees,
                            Gender = gender
                        });
                    }
                }
            }
        }

        /// <summary>
        /// 加载搜索帐号
        /// </summary>
        private static void LoadCookies()
        {
            using (var db = new MangningXssDBEntities())
            {
                var list = db.ZhaopinCleaningProcedure.Where(w => w.IsEnable && !w.IsOnline && !string.IsNullOrEmpty(w.Cookie));

                foreach (var item in list)
                {
                    cookieQueue.Enqueue(item.Cookie.Serialize(".zhaopin.com"));
                }
            }
        }

        /// <summary>
        /// 按条件搜索简历
        /// </summary>
        /// <param name="cookieContainer"></param>
        private static void SearchResumes(CookieContainer cookieContainer)
        {
            while (true)
            {
                ZhaopinSearchCondition condition;

                if (count >= refreshCount) break;

                if (!conditionQueue.TryDequeue(out condition))
                {
                    Thread.Sleep(100);

                    break;
                }

                var pageIndex = 1;

                var pageTotal = 1;

                var total = 0;

                while (pageIndex < pageTotal + 1)
                {
                    if (count >= refreshCount) break;

                    // 2041：宝安  635：南京  653：杭州  639：苏州 765：深圳

                    var param = string.IsNullOrEmpty(keyword) ? string.Empty : $"SF_1_1_1={keyword}&";

                    param += string.IsNullOrEmpty(position) ? string.Empty : $"SF_1_1_2={position}&";

                    param += string.IsNullOrEmpty(industry) ? string.Empty : $"SF_1_1_3={industry}&";

                    var url = $"https://ihrsearch.zhaopin.com/Home/ResultForCustom?{param}SF_1_1_6={city}&SF_1_1_8={condition.Age}%2C{condition.Age}&SF_1_1_9={condition.Gender}&SF_1_1_5={condition.Degrees}%2C{condition.Degrees}&orderBy=BIRTH_YEAR%2C0&SF_1_1_27=0&SF_1_1_7={date}%2C9&exclude=1&pageIndex={pageIndex}&pageSize=60";

                    var requestResult = RequestFactory.QueryRequest(url, cookieContainer: cookieContainer, referer: url);

                    if (!requestResult.IsSuccess)
                    {
                        Trace.TraceError($"{DateTime.Now} > Search Error ! Message = {requestResult.ErrorMsg} Condition = {JsonConvert.SerializeObject(condition)}.");

                        continue;
                    }

                    if (pageTotal == 1)
                    {
                        var match = Regex.Match(requestResult.Data, "(?s)<span>(\\d+)</span>份简历.+?rd-resumelist-pageNum\">1/(\\d+)</span>");

                        if (!match.Success)
                        {
                            if (requestResult.Data.Contains("text/javascript\" r='m'"))
                            {
                                Trace.TraceWarning($"{DateTime.Now} > Cookie Expired !");
                            }
                            else
                            {
                                Trace.TraceWarning($"{DateTime.Now} > Condition search error ! Page content = {requestResult.Data}");
                            }

                            return;
                        }

                        total = Convert.ToInt32(match.Result("$1"));

                        pageTotal = Convert.ToInt32(match.Result("$2"));

                        totalCount += total;

                        if (total == 0)
                        {
                            Console.WriteLine($"{DateTime.Now} > Search results are empty !");

                            break;
                        }
                    }

                    Console.WriteLine($"{DateTime.Now} > 第 {pageIndex} 页  共 {pageTotal} 页 {total} 个结果");

                    pageIndex++;

                    var matchs = Regex.Matches(requestResult.Data, "(?s)RedirectToRd/([^\r\n]+?)','([^\r\n]+?)','([^\r\n]+?)',this\\);this.+?(\\d{2}-\\d{2}-\\d{2})");

                    foreach (Match item in matchs)
                    {
                        try
                        {
                            if (count >= refreshCount) break;

                            var number = item.Result("$1").Substring(0, 10);

                            DateTime updateDateTime;

                            if (!DateTime.TryParse(item.Result("$4"), out updateDateTime)) continue;

                            using (var db = new MangningXssDBEntities())
                            {
                                var resume = db.ZhaopinResume.FirstOrDefault(f => f.RandomNumber == number);

                                if (resume != null)
                                {
                                    if (resume.Flag == 15)
                                    {
                                        #region 刷新简历更新时间

                                        //if (resume.RefreshTime != null && updateDateTime.Date == resume.RefreshTime.Value.Date) continue;

                                        resume.RefreshTime = updateDateTime;

                                        resume.Flag = 14;

                                        var filePath = $@"D:\Badoucai\Resume\LocationJson\{resume.Id}.json";

                                        using (var stream = new MemoryStream())
                                        {
                                            if (!mangningOssClient.DoesObjectExist(mangningBucketName, $"Zhaopin/Resume/{resume.Id}")) continue;

                                            var bytes = new byte[1024];

                                            int len;

                                            var streamContent = mangningOssClient.GetObject(mangningBucketName, $"Zhaopin/Resume/{resume.Id}").Content;

                                            while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
                                            {
                                                stream.Write(bytes, 0, len);
                                            }

                                            var resumeContent = Encoding.UTF8.GetString(GZip.Decompress(stream.ToArray()));

                                            var jsonObj = JsonConvert.DeserializeObject<dynamic>(resumeContent);

                                            dynamic detialJSonStr;

                                            try
                                            {
                                                detialJSonStr = jsonObj.detialJSonStr;

                                                if (!string.IsNullOrEmpty((string)jsonObj.detialJSonStr.DateModified))
                                                {
                                                    jsonObj.detialJSonStr = JsonConvert.SerializeObject(jsonObj.detialJSonStr);
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                detialJSonStr = JsonConvert.DeserializeObject<dynamic>((string)jsonObj.detialJSonStr);
                                            }

                                            detialJSonStr.DateLastReleased = updateDateTime;

                                            detialJSonStr.DateModified = updateDateTime;

                                            jsonObj.detialJSonStr = detialJSonStr;

                                            var newResumeContent = JsonConvert.SerializeObject(jsonObj);

                                            using (var jsonStream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(newResumeContent))))
                                            {
                                                mangningOssClient.PutObject(mangningBucketName, $"Zhaopin/Resume/{resume.Id}", jsonStream);
                                            }

                                            File.WriteAllText(filePath, newResumeContent);
                                        }

                                        #endregion
                                    }

                                    if (resume.Flag == 2)
                                    {
                                        var user = db.ZhaopinUser.FirstOrDefault(f => f.Id == resume.UserId);

                                        if(user == null) continue;

                                        WatchResumeDetail(item, cookieContainer, user.Cellphone, user.Email);
                                    }
                                }
                                else
                                {
                                    var incompleteResume = db.ZhaopinIncompleteResume.FirstOrDefault(f => f.ResumeNumber == number);

                                    if (incompleteResume == null) continue;

                                    if (WatchResumeDetail(item, cookieContainer, incompleteResume.Cellphone, incompleteResume.Email))
                                    {
                                        incompleteResume.CompletionTime = DateTime.Now;
                                    }
                                }

                                db.SaveChanges();

                                Interlocked.Increment(ref count);

                                if (resume != null) sb.AppendLine(resume.Id.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError(ex.ToString());
                        }
                    }

                    Console.WriteLine($"{DateTime.Now} > Total = {totalCount}, Update Count = {count}, Cookie Count = {cookieQueue.Count}, Condition Count = {conditionQueue.Count}. ");
                }
            }
        }

        /// <summary>
        /// 查看简历详情补全简历
        /// </summary>
        /// <param name="match"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="cellphone"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        private static bool WatchResumeDetail(Match match, CookieContainer cookieContainer, string cellphone, string email)
        {
            var number = match.Result("$1").Substring(0, 10);

            var numberParam = match.Result("$1").Substring(0, match.Result("$1").IndexOf("?", StringComparison.Ordinal));

            var requestResult = RequestFactory.QueryRequest($"http://ihr.zhaopin.com/resumesearch/getresumedetial.do?resumeNo={numberParam}&searchresume=1&resumeSource=1&keyword=%E8%AF%B7%E8%BE%93%E5%85%A5%E7%AE%80%E5%8E%86%E5%85%B3%E9%94%AE%E8%AF%8D%EF%BC%8C%E5%A4%9A%E5%85%B3%E9%94%AE%E8%AF%8D%E5%8F%AF%E7%94%A8%E7%A9%BA%E6%A0%BC%E5%88%86%E9%9A%94&t={match.Result("$2")}&k={match.Result("$3")}&v=undefined&version=3&openFrom=1", cookieContainer: cookieContainer);

            if (!requestResult.IsSuccess)
            {
                Trace.WriteLine($"{DateTime.Now} > Watching error ! Message = {requestResult.ErrorMsg}, ResumeNumber = {number}");

                return false;
            }

            try
            {
                var jsonObj = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                if ((int)jsonObj["code"] != 1)
                {
                    if (((string)jsonObj["message"]).Contains("当日查看简历已达上限"))
                    {
                        Trace.WriteLine($"{DateTime.Now} > Watching failure ! Message = {(string)jsonObj["message"]}, ResumeNumber = {number}");
                    }

                    return false;
                }

                var resumeData = jsonObj.data;

                resumeData.userDetials.mobilePhone = cellphone;

                resumeData.userDetials.email = email;

                var resumeDetail = JsonConvert.DeserializeObject(resumeData.detialJSonStr.ToString());

                var resumeId = resumeData.resumeId != null ? (int)resumeData.resumeId : resumeDetail.ResumeId != null ? (int)resumeDetail.ResumeId : 0;

                File.WriteAllText($"{path}{resumeId}.json",JsonConvert.SerializeObject(resumeData));

                Console.WriteLine($"{DateTime.Now} > Completion success ! ResumeNumber = {number}.");

                return true;
            }
            catch (Exception ex)
            {
                while (true)
                {
                    if (ex.InnerException == null) break;

                    ex = ex.InnerException;
                }

                Trace.WriteLine($"{DateTime.Now} > Watching exception ! Message = {ex.Message}, ResumeNumber = {number}");

                return false;
            }
        }
    }
}