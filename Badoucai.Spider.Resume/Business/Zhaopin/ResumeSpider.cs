using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Badoucai.Business.Zhaopin;
using Badoucai.Library;
using Newtonsoft.Json;
using System.Configuration;
using System.Linq;
using System.Threading;
using Badoucai.EntityFramework.MySql;

namespace Badoucai.Spider.Resume.Business.Zhaopin
{
    public class ResumeSpider
    {
        private static readonly ConcurrentQueue<KeyValuePair<int, string>> cookieQueue = new ConcurrentQueue<KeyValuePair<int, string>>();

        private static readonly List<Task> tasks = new List<Task>();

        private static readonly int taskCount = Convert.ToInt32(ConfigurationManager.AppSettings["TaskCount"]);

        private static void DownloadForTalentFolder()
        {
            var business = new SpiderResumeBusiness();

            Console.WriteLine("开始获取 Cookie ...");

            var cookies = business.GetCookies();

            foreach (var item in cookies)
            {
                cookieQueue.Enqueue(item);
            }

            for (var i = 0; i < taskCount; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        KeyValuePair<int, string> cookie;

                        if (!cookieQueue.TryDequeue(out cookie)) break;

                        try
                        {
                            var token = cookie.Value.Substring(cookie.Value.IndexOf("at=", StringComparison.Ordinal) + 3, 32);

                            Console.WriteLine($"Cookie 队列剩余：{cookieQueue.Count}, 当前 Token：{token}");

                            var cookieContainer = cookie.Value.Serialize("zhaopin.com");

                            DataResult<string> requestResult;

                            dynamic content;

                            dynamic summary;

                            int count;

                            int pageNumber;

                            int begin;

                            int end;

                            var isBreak = false;

                            #region 下载简历库简历

                            foreach (var key in new[] { "deal", "commu", "interview", "noSuit" })
                            {
                                var isContinue = false;

                                requestResult = business.GetResumes(cookieContainer, 0, 1, key, token);

                                if (!requestResult.IsSuccess) break;

                                content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                                if ((int)content.code != 1)
                                {
                                    LogFactory.Warn($"简历列表下载异常！message：{content.message} companyId：{cookie.Key}");

                                    if ((int)content.code == 6001) // 登录过期
                                    {
                                        isBreak = true;

                                        using (var db = new MangningXssDBEntities())
                                        {
                                            var staff = db.ZhaopinStaff.FirstOrDefault(f => f.CompanyId == cookie.Key);

                                            if (staff != null)
                                            {
                                                staff.Cookie = null;

                                                db.SaveChanges();
                                            }
                                        }

                                        break;
                                    }

                                    continue;
                                }

                                count = (int)content.data[key].numFound;

                                //Console.WriteLine($"CompanyId：{cookie.Key}，分类：{key} 简历数：{count}");

                                if (count == 0) continue;

                                if(business.IsDownloadByDeliveryId((long)content.data[key].results[0].id, key)) continue;

                                pageNumber = (int)Math.Ceiling(count / 90.0);

                                while (true)
                                {
                                    requestResult = business.GetResumes(cookieContainer, count - 1, 90, key, token);

                                    if (!requestResult.IsSuccess)
                                    {
                                        isContinue = true;

                                        break;
                                    }

                                    content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                                    if ((int)content.data[key].results.Count != 1)
                                    {
                                        var numFound = (int)content.data[key].numFound;

                                        if (numFound == count) break;

                                        count = numFound;

                                        continue;
                                    }

                                    break;
                                }

                                if(isContinue) continue;

                                summary = content.data[key].results[(int)content.data[key].results.Count - 1];

                                if (!business.IsDownloadByDeliveryId((long)summary.id, key))
                                {
                                    business.DownloadResumes(cookieContainer, pageNumber - 1, key, cookie.Key, business.GetResumes, token);

                                    continue;
                                }

                                #region 中分法查找新纪录的位置

                                begin = 0;

                                end = pageNumber - 1;
                                
                                while (begin <= end)
                                {
                                    //Console.WriteLine($"Begin：{begin}，End：{end}");

                                    Thread.Sleep(10);

                                    var current = (begin + end) >> 1;

                                    if (begin == end)
                                    {
                                        business.DownloadResumes(cookieContainer, current, key, cookie.Key, business.GetResumes, token);

                                        break;
                                    }

                                    requestResult = business.GetResumes(cookieContainer, current * 90, 90, key, token);
                                    
                                    if (!requestResult.IsSuccess) break;

                                    content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                                    if ((int)content.code != 1)
                                    {
                                        LogFactory.Warn($"简历列表下载异常！message：{content.message} companyId：{cookie.Key}");

                                        if ((int)content.code == 6001) // 登录过期
                                        {
                                            isBreak = true;

                                            using (var db = new MangningXssDBEntities())
                                            {
                                                var staff = db.ZhaopinStaff.FirstOrDefault(f => f.CompanyId == cookie.Key);

                                                if (staff != null)
                                                {
                                                    staff.Cookie = null;

                                                    db.SaveChanges();
                                                }
                                            }

                                            break;
                                        }

                                        continue;
                                    }

                                    count = (int)content.data[key].numFound;

                                    if (pageNumber != (int)Math.Ceiling(count / 90.0))
                                    {
                                        pageNumber = (int)Math.Ceiling(count / 90.0);

                                        end = pageNumber - 1;

                                        continue;
                                    }

                                    var summaries = content.data[key].results;

                                    var size = summaries.Count;

                                    // 如果当前页的第一条记录是否下载过，下载过则向前搜索,
                                    // 否则继续判断最后一条是否下载过，如下载过，则断点在
                                    // 当前页,否则向后搜索

                                    if (business.IsDownloadByDeliveryId((long)summaries[0].id, key))
                                    {
                                        end = current;
                                    }
                                    else
                                    {
                                        if (business.IsDownloadByDeliveryId((long)summaries[size - 1].id, key))
                                        {
                                            business.DownloadResumes(cookieContainer, current, key, cookie.Key, business.GetResumes, token);

                                            break;
                                        }

                                        begin = current + 1;
                                    }
                                }

                                #endregion
                            }

                            #endregion

                            if(isBreak) continue;

                            #region 下载回收站的简历

                            requestResult = business.GetRecycleResumes(cookieContainer, 0, 1, "recycle", token);

                            if (!requestResult.IsSuccess) break;

                            content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                            count = (int)content.data.numFound;

                            //Console.WriteLine($"CompanyId：{cookie.Key}，分类：Recycle 简历数：{count}");

                            if (count == 0) continue;

                            if (business.IsDownloadByDeliveryId((long)content.data.results[0].id, "recycle")) continue;

                            pageNumber = (int)Math.Ceiling(count / 90.0);

                            while (true)
                            {
                                requestResult = business.GetRecycleResumes(cookieContainer, count - 1, 90, "recycle", token);

                                if (!requestResult.IsSuccess) break;

                                content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                                if ((int)content.data.results.Count != 1)
                                {
                                    var numFound = (int)content.data.numFound;

                                    if (numFound == count) break;

                                    count = numFound;

                                    continue;
                                }

                                break;
                            }
                            summary = content.data.results[(int)content.data.results.Count - 1];

                            if (!business.IsDownloadByDeliveryId((long)summary.id, "recycle"))
                            {
                                business.DownloadResumes(cookieContainer, pageNumber - 1, "recycle", cookie.Key, business.GetRecycleResumes, token);

                                continue;
                            }

                            #region 中分法查找新纪录的位置

                            begin = 0;

                            end = pageNumber - 1;

                            while (begin <= end)
                            {
                                //Console.WriteLine($"Begin：{begin}，End：{end}");

                                Thread.Sleep(10);

                                var current = (begin + end) >> 1;

                                if (begin == end)
                                {
                                    business.DownloadResumes(cookieContainer, current, "recycle", cookie.Key, business.GetRecycleResumes, token);

                                    break;
                                }

                                requestResult = business.GetRecycleResumes(cookieContainer, current * 90, 90, "recycle", token);

                                if (!requestResult.IsSuccess) break;

                                content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                                count = (int)content.data.numFound;

                                if (pageNumber != (int)Math.Ceiling(count / 90.0))
                                {
                                    pageNumber = (int)Math.Ceiling(count / 90.0);

                                    end = pageNumber - 1;

                                    continue;
                                }

                                var summaries = content.data.results;

                                var size = summaries.Count;

                                // 如果当前页的第一条记录是否下载过，下载过则向前搜索,
                                // 否则继续判断最后一条是否下载过，如下载过，则断点在
                                // 当前页,否则向后搜索

                                if (business.IsDownloadByDeliveryId((long)summaries[0].id, "recycle"))
                                {
                                    end = current;
                                }
                                else
                                {
                                    if (business.IsDownloadByDeliveryId((long)summaries[size - 1].id, "recycle"))
                                    {
                                        business.DownloadResumes(cookieContainer, current, "recycle", cookie.Key, business.GetRecycleResumes, token);

                                        break;
                                    }

                                    begin = current + 1;
                                }
                            }

                                #endregion

                                #endregion

                            while (true)
                            {
                                if (SpiderResumeBusiness.downQueue.Count != 0)
                                {
                                    Thread.Sleep(1000);

                                    continue;
                                }

                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            while (true)
                            {
                                if (ex.InnerException == null) break;

                                ex = ex.InnerException;
                            }

                            LogFactory.Warn($"下载简历异常！ CompanyId：{cookie.Key} 跳过该 Cookie, 异常信息：{ex.Message} 堆栈：{ex.StackTrace}");
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());
        }

        /// <summary>
        /// 通过ResumeNumber匹配旧库和线上库
        /// </summary>
        private static void MatchOldResumeLibrary()
        {
            var business = new OldResumeMatchBusiness();

            business.MatchOldResumeLibrary();

            business.Buquan();
        }

        private static void MatchZhaopinLibrary()
        {
            var business = new MatchZhaopinBusiness();

            business.MatchZhaopin();
        }

        private static void WatchNewZhaopinResume()
        {
            var business = new WatchNewResumeBusiness();

            business.Watch();
        }

        private static void WatchOldZhaopinResume()
        {
            var business = new WatchOldResumeBusiness();

            business.Watch();

            //business.Buquan();

        }

        private static void WatchSpecificZhaopinResume()
        {
            var business = new WatchSpecificResumeBusiness();

            business.Watch();
        }

        private static void MatchResumeLocation()
        {
            var business = new MatchResumeLocationBusiness();

            business.Match();
        }

        public static void Init()
        {
            MatchZhaopinLibrary();

            Task.WaitAll(new List<Task>
            {
                //new Action(WatchOldZhaopinResume).LoopStartTask(TimeSpan.FromSeconds(10),()=>false)
                //new Action(MatchResumeLocation).LoopStartTask(TimeSpan.FromSeconds(10),()=>false),
                new Action(DownloadForTalentFolder).LoopStartTask(TimeSpan.FromSeconds(10), () => SpiderResumeBusiness.downQueue.Count == 0 && SpiderResumeBusiness.uploadOssActionBlock.InputCount == 0)
            }.ToArray());
        }
    }
}