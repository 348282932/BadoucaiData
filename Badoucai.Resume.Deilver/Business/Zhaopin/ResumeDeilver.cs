using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Badoucai.Resume.Deilver.Business.Zhaopin
{
    public class ResumeDeilver
    {
        private static readonly int singleUserDeilverCount = Convert.ToInt32(ConfigurationManager.AppSettings["DeilverCount"]);

        private static readonly short expectedDeilverCount = Convert.ToInt16(ConfigurationManager.AppSettings["ExpectedDeilverCount"]);

        private static readonly short deilverUserCount = Convert.ToInt16(ConfigurationManager.AppSettings["DeilverUserCount"]);

        private static readonly Dictionary<int, DateTime> checkRecordDictionary = new Dictionary<int, DateTime>();

        private static readonly Queue<dynamic> localCompanyInfoQueue = new Queue<dynamic>();

        /// <summary>
        /// 投递简历
        /// </summary>
        private static void DeilverTask()
        {
            var userResumeQueue = new Queue<Tuple<dynamic, int>>();

            var positionQueue = new Queue<dynamic>();

            #region 填充用户简历及职位队列

            using (var db = new MangningXssDBEntities())
            {
                var date = DateTime.Now.Date.AddHours(-8);

                var userResumeRecordList = db.ZhaopinDeilveryRecord.AsNoTracking().Where(w => w.CreateTime > date).GroupBy(g => g.UserId).Select(s => s.Key).ToList();

                if (userResumeRecordList.Any())
                {
                    var userResumes = db.ZhaopinResume
                        .Join(db.ZhaopinUser.Where(w => userResumeRecordList.Any(a=>a == w.Id) && !string.IsNullOrEmpty(w.Cookie) && w.Source.Contains("MANUAL") && string.IsNullOrEmpty(w.Status)), a => a.UserId, b => b.Id, (a, b) => new { b.Username, a.UserId, ResumeNumber = a.DeliveryNumber, b.Cookie })
                        .ToList();
                    foreach (var user in userResumes)
                    {
                        if (userResumeQueue.Count >= deilverUserCount) break;

                        var todayDeilverCount = db.ZhaopinDeilveryRecord.Count(w => w.UserId == user.UserId && w.CreateTime > date);

                        //if (string.IsNullOrEmpty(user.Cookie))
                        //{
                        //    LogFactory.Warn($"投递用户：{user.Username} Cookie 已过期！请录入有效 Cookie ！");

                        //    return;
                        //}

                        userResumeQueue.Enqueue(new Tuple<dynamic, int>(user, todayDeilverCount));
                    }
                }

                if (userResumeQueue.Count < deilverUserCount)
                {
                    var userResumeList = db.ZhaopinResume
                        .Join(db.ZhaopinUser.Where(w => userResumeRecordList.All(a => a != w.Id) && !string.IsNullOrEmpty(w.Cookie) && w.Source.Contains("MANUAL") && string.IsNullOrEmpty(w.Status)), a => a.UserId, b => b.Id, (a, b) => new { b.Username, a.UserId, ResumeNumber = a.DeliveryNumber, b.Cookie })
                        .ToList();

                    foreach (var f in userResumeList)
                    {
                        var todayDeilverCount = db.ZhaopinDeilveryRecord.Count(w => w.UserId == f.UserId && w.CreateTime > date);

                        if (todayDeilverCount < singleUserDeilverCount)
                        {
                            if (userResumeQueue.Count >= deilverUserCount) break;

                            int code;

                            if (CheckUserIsUseable(f, out code))
                            {
                                userResumeQueue.Enqueue(new Tuple<dynamic, int>(f, todayDeilverCount));
                            }
                            else
                            {
                                var user = db.ZhaopinUser.FirstOrDefault(df => df.Id == f.UserId);

                                if (user != null)
                                {
                                    user.Status = "BLOCKED";

                                    if(code == -1) user.Status = "ABNORMAL";

                                    user.UpdateTime = DateTime.UtcNow;

                                    db.SaveChanges();
                                }
                            }
                        }
                    }
                }

                if (!userResumeQueue.Any())
                {
                    Console.WriteLine("等待可用用户中....");

                    return;
                }

                if (userResumeQueue.All(a=>a.Item2 >= singleUserDeilverCount))
                {
                    var todayCount = db.ZhaopinDeilveryRecord.AsNoTracking().Count(c => c.CreateTime > date);

                    if (todayCount < singleUserDeilverCount * deilverUserCount)
                    {
                        Console.WriteLine("等待可用用户中....");

                        return;
                    }

                    Console.WriteLine("今日投递任务已完成....");

                    return;
                }

                var positionList = db.ZhaopinDeilverTask
                    .Where(w => w.Status == 0)
                    .Join(db.ZhaopinPosition, a => a.CompanyId, b => b.CompanyId, (a, b) => new { TaskId = a.Id, a.CompanyId, PositionId = b.Id, b.Number, b.CreateTime, b.IsEnable, b.Name })
                    .OrderByDescending(o => o.CreateTime)
                    .ToList();

                var deilverTaskList = db.ZhaopinDeilverTask.Where(w => w.Status == 0).OrderBy(o => o.Priority).ToList();

                date = DateTime.UtcNow.AddDays(-7);

                foreach (var f in deilverTaskList)
                {
                    var positions = positionList
                        .Where(w => w.TaskId == f.Id && w.IsEnable && w.CreateTime > date)
                        .OrderByDescending(o => o.CreateTime)
                        .Skip(f.ActualDeilverCount)
                        .Take(f.ExpectedDeilverCount - f.ActualDeilverCount)
                        .ToList();

                    if (!positions.Any())
                    {
                        f.Status = 2;

                        f.CompleteTime = DateTime.UtcNow;

                        continue;
                    }

                    positions = positions.Take(f.ExpectedDeilverCount - f.ActualDeilverCount).ToList();

                    positions.ForEach(pf =>
                    {
                        positionQueue.Enqueue(pf);
                    });
                }

                db.TransactionSaveChanges();
            }

            if (!positionQueue.Any())
            {
                Console.WriteLine("等待分配任务中....");

                return;
            }

            #endregion

            while (true)
            {
                if (!positionQueue.Any()) return;

                var position = positionQueue.Dequeue();

                if(!userResumeQueue.Any()) return;

                var userResume = userResumeQueue.Dequeue();

                #region 投递简历

                var userDeilverNumber = 0;

                do
                {
                    var deilverResult = RequestFactory.QueryRequest($"https://my.zhaopin.com/v5/FastApply/resumeinfo.aspx?t=3&j={position.Number}&j2=&so=&su=&ff=ssb&rv={userResume.Item1.ResumeNumber}_1&rl=1&cl=&sd=0&fd=&c=jsonp7u3t5v&_=1493174565322", cookieContainer: ((string)userResume.Item1.Cookie).Serialize("zhaopin.com"));

                    if (!deilverResult.IsSuccess)
                    {
                        LogFactory.Warn($"投递简历请求异常！简历编号：{userResume.Item1.ResumeNumber}，职位编号：{position.Number}，异常信息：{deilverResult.ErrorMsg}");

                        continue;
                    }

                    var message = JsonConvert.DeserializeObject<dynamic>(Regex.Match(deilverResult.Data, @"(\{.+?\})").Result("$1"));

                    if (message.loginstatus.ToString().Contains("7_Position"))
                    {
                        LogFactory.Warn($"该职位已下架！职位编号：{position.Number} Json:{JsonConvert.SerializeObject(message)}");

                        using (var db = new MangningXssDBEntities())
                        {
                            var positionId = (int)position.PositionId;

                            var positionDefault = db.ZhaopinPosition.FirstOrDefault(f => f.Id == positionId);

                            if (positionDefault != null)
                            {
                                positionDefault.IsEnable = false;
                            }
                            else
                            {
                                LogFactory.Warn("数据库未找到对应的职位！职位ID：" + positionId);
                            }

                            db.TransactionSaveChanges();
                        }

                        if(!positionQueue.Any()) return;

                        position = positionQueue.Dequeue();

                        continue;
                    }

                    if (message.loginstatus.ToString() != "6")
                    {
                        LogFactory.Warn($"用户登录 Cookie 失效！用户ID：{userResume.Item1.UserId} Json:{JsonConvert.SerializeObject(message)}");

                        using (var db = new MangningXssDBEntities())
                        {
                            var userId = (int)userResume.Item1.UserId;

                            var user = db.ZhaopinUser.FirstOrDefault(f => f.Id == userId);

                            if (user != null)
                            {
                                user.Cookie = "";

                                user.UpdateTime = DateTime.UtcNow;
                            }
                            else
                            {
                                LogFactory.Warn("数据库未找到对应的用户！用户ID：" + userId);
                            }

                            //db.TransactionSaveChanges();
                        }

                        if (!userResumeQueue.Any()) return;

                        userResume = userResumeQueue.Dequeue();

                        continue;
                    }

                    var status = message.postBackInfo.ToString().Split('_');

                    if (status[3].ToString() == "0")
                    {
                        if (status[0] == "0" && status[1] == "0" && status[2] == "0")
                        {
                            using (var db = new MangningXssDBEntities())
                            {
                                var userId = (int)userResume.Item1.UserId;

                                var user = db.ZhaopinUser.FirstOrDefault(f => f.Id == userId);

                                if (user != null)
                                {
                                    user.Status = "PENDING";

                                    user.UpdateTime = DateTime.UtcNow;

                                    db.TransactionSaveChanges();
                                }
                            }

                            LogFactory.Warn($"该帐号已不能继续投递！用户ID：{userResume.Item1.UserId}");

                            if(!userResumeQueue.Any()) return;

                            userResume = userResumeQueue.Dequeue();

                            continue;
                        }

                        LogFactory.Warn($"七天内已投递过该职位！用户ID：{userResume.Item1.UserId}，职位编号：{position.Number}，职位名称：{position.Name}");

                        if (userDeilverNumber < userResumeQueue.Count)
                        {
                            userResumeQueue.Enqueue(userResume);

                            userDeilverNumber++;

                            userResume = userResumeQueue.Dequeue();

                            continue;
                        }

                        if (!positionQueue.Any()) return;

                        position = positionQueue.Dequeue();

                        userDeilverNumber = 0;

                        continue;
                    }

                    var deilverCount = userResume.Item2;

                    if (++deilverCount < singleUserDeilverCount)
                    {
                        userResumeQueue.Enqueue(new Tuple<dynamic, int>(userResume.Item1, deilverCount));
                    }

                    break;
                }
                while (true);

                #endregion

                #region 变更任务状态及添加投递记录

                using (var db = new MangningXssDBEntities())
                {
                    var taskId = (int)position.TaskId;

                    var deilverTask = db.ZhaopinDeilverTask.FirstOrDefault(f => f.Id == taskId);

                    if (deilverTask != null)
                    {
                        deilverTask.ActualDeilverCount += 1;

                        if (deilverTask.ActualDeilverCount == deilverTask.ExpectedDeilverCount)
                        {
                            deilverTask.Status = 1;

                            deilverTask.CompleteTime = DateTime.UtcNow;
                        }
                    }
                    else
                    {
                        LogFactory.Warn("数据库未找到对应的任务！任务ID：" + taskId);
                    }

                    db.ZhaopinDeilveryRecord.Add(new ZhaopinDeilveryRecord
                    {
                        CompanyId = position.CompanyId,
                        PositionId = position.PositionId,
                        UserId = userResume.Item1.UserId,
                        CreateTime = DateTime.UtcNow
                    });

                    db.TransactionSaveChanges();
                }

                #endregion

                LogFactory.Info($"投递成功！用户：{userResume.Item1.Username} 今日投递数：{userResume.Item2 + 1} 职位：{position.Name} 还剩 {positionQueue.Count} 个职位待投递");

                if (userResumeQueue.Count != 0)
                {
                    Thread.Sleep(1000 * 5 / userResumeQueue.Count);
                }

                
            }
        }

        /// <summary>
        /// 投递任务调度
        /// </summary>
        private static void DeilverTaskScheduling()
        {
            var date = DateTime.UtcNow.AddDays(-7);

            using (var db = new MangningXssDBEntities())
            {
                var queryable = from a in db.ZhaoPinCompany
                    join b in db.ZhaopinDeilverTask on a.Id equals b.CompanyId into tempb
                    from ab in tempb.DefaultIfEmpty()
                    join c in (from d in db.ZhaopinPosition where d.IsEnable select d) on a.Id equals c.CompanyId into tempc
                    from ac in tempc.DefaultIfEmpty()
                    where a.Source == "SEARCH" && ac.Id != 0 && (ab.Id == 0 || ab.Id != 0 && ab.Status != 0 && ab.CompleteTime < date && ac.CreateTime > date)
                    orderby a.UpdateTime descending
                    select a.Id;

                db.ZhaopinDeilverTask.AddRange(queryable.Distinct().ToList().Select(s => new ZhaopinDeilverTask
                {
                    ActualDeilverCount = 0,
                    CreateTime = DateTime.UtcNow,
                    CompanyId = s,
                    CompleteTime = null,
                    ExpectedDeilverCount = expectedDeilverCount,
                    Priority = 0,
                    Status = 0
                }));

                db.TransactionSaveChanges();
            }
        }

        /// <summary>
        /// 检查用户账户是否被封禁
        /// </summary>
        /// <param name="userResume"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private static bool CheckUserIsUseable(dynamic userResume, out int code)
        {
            code = 0;

            var date = DateTime.Now.Date.AddHours(-8);

            if (checkRecordDictionary.ContainsKey(userResume.UserId) && checkRecordDictionary[userResume.UserId] > date) return true;

            while (true)
            {
                if (!localCompanyInfoQueue.Any())
                {
                    using (var db = new MangningXssDBEntities())
                    {
                        var companyPositionList = (from a in db.ZhaoPinCompany
                            join b in db.ZhaopinPosition on a.Id equals b.CompanyId
                            join c in db.ZhaopinStaff on a.Id equals c.CompanyId
                            where a.Source.Contains("MANUAL") && !string.IsNullOrEmpty(c.Cookie) && b.IsEnable
                            select new
                            {
                                a.Name,
                                CompanyId = a.Id,
                                PositionNumber = b.Number,
                                c.Cookie
                            }).ToList();

                        if (companyPositionList.Any())
                        {
                            companyPositionList.ForEach(f =>
                            {
                                localCompanyInfoQueue.Enqueue(f);
                            });
                        }
                        else
                        {
                            //todo:调用通知API

                            LogFactory.Warn("无有效本地公司 Cookie 校验帐号是否可用！");

                            Thread.Sleep(TimeSpan.FromSeconds(30));

                            continue;
                        }
                    }
                }

                while (true)
                {
                    if (!localCompanyInfoQueue.Any()) break;

                    var localCompanyInfo = localCompanyInfoQueue.Dequeue();

                    var cookieStr = (string)localCompanyInfo.Cookie;

                    var isLoginResult = RequestFactory.QueryRequest("https://ihr.zhaopin.com/home/assetcount.do", cookieContainer: cookieStr.Serialize("zhaopin.com"));

                    if (!isLoginResult.IsSuccess)
                    {
                        LogFactory.Warn($"校验公司 Cookie 是否有效请求异常！公司：{localCompanyInfo.Name}，异常信息：{isLoginResult.ErrorMsg}");

                        continue;
                    }

                    var obj = JsonConvert.DeserializeObject(isLoginResult.Data) as JObject;

                    if ((string)obj?["code"] != "200")
                    {
                        var id = (int)localCompanyInfo.CompanyId;

                        using (var db = new MangningXssDBEntities())
                        {
                            var companyStaff = db.ZhaopinStaff.FirstOrDefault(f => f.CompanyId == id);

                            if (companyStaff != null) companyStaff.Cookie = "";

                            db.TransactionSaveChanges();
                        }

                        var count = localCompanyInfoQueue.Count;

                        for (var i = 0; i < count; i++)
                        {
                            var temp = localCompanyInfoQueue.Dequeue();

                            if (temp.CompanyId != localCompanyInfo.CompanyId)
                            {
                                localCompanyInfoQueue.Enqueue(temp);
                            }
                        }

                        LogFactory.Warn($"公司 Cookie 失效！公司：{localCompanyInfo.Name}，异常信息：{obj?["message"]}");

                        continue;
                    }

                    var userCookieStr = (string)userResume.Cookie;

                    var deilverResult = RequestFactory.QueryRequest($"https://my.zhaopin.com/v5/FastApply/resumeinfo.aspx?t=3&j={localCompanyInfo.PositionNumber}&j2=&so=&su=&ff=ssb&rv={userResume.ResumeNumber}_1&rl=1&cl=&sd=0&fd=&c=jsonp7u3t5v&_=1493174565322", cookieContainer: userCookieStr.Serialize("zhaopin.com"));

                    if (!deilverResult.IsSuccess)
                    {
                        LogFactory.Warn($"投递简历请求异常！简历编号：{userResume.ResumeNumber}，职位编号：{localCompanyInfo.PositionNumber}，异常信息：{deilverResult.ErrorMsg}");

                        continue;
                    }

                    var message = JsonConvert.DeserializeObject<dynamic>(Regex.Match(deilverResult.Data, @"(\{.+?\})").Result("$1"));

                    var status = message.postBackInfo.ToString().Split('_');

                    if (message.loginstatus.ToString() != "6") return true;

                    if (message.loginstatus.ToString() == "7_Position has Down")
                    {
                        LogFactory.Warn($"该职位已下架！职位编号：{localCompanyInfo.PositionNumber}");

                        using (var db = new MangningXssDBEntities())
                        {
                            var positionNumber = (string)localCompanyInfo.PositionNumber;

                            var positionDefault = db.ZhaopinPosition.FirstOrDefault(f => f.Number == positionNumber);

                            if (positionDefault != null)
                            {
                                positionDefault.IsEnable = false;
                            }
                            else
                            {
                                LogFactory.Warn("数据库未找到对应的职位！职位 Number：" + positionNumber);
                            }

                            db.TransactionSaveChanges();
                        }

                        continue;
                    }

                    if (status[3].ToString() == "0")
                    {
                        Console.WriteLine($"投递本地公司失败！，职位 7 天内投递过！公司：{localCompanyInfo.Name} 职位编号：{localCompanyInfo.PositionNumber} 用户：{userResume.Username}");

                        if (!localCompanyInfoQueue.Any())
                        {
                            LogFactory.Warn($"本地可用公司的所有职位均在7天内投递过！用户：{userResume.Username}");
                        }

                        continue;
                    }

                    Console.WriteLine($"投递本地公司成功！公司：{localCompanyInfo.Name}，用户：{userResume.Username}");

                    Thread.Sleep(TimeSpan.FromSeconds(5));

                    var token = cookieStr.Substring(cookieStr.IndexOf("Token=", StringComparison.Ordinal) + 6, 32);

                    var dataResult = RequestFactory.QueryRequest($"https://ihr.zhaopin.com/resumemanage/resumelistbykey.do?access_token={token}&v=0.47046618467879875", "startNum=0&rowsCount=30&ageStart=&ageEnd=&workYears=&sex=&edu=&liveCity=&hopeWorkCity=&upDate=&companyName=&exclude=&keywords=&onlyLastWork=false&orderFlag=deal&countFlag=1&jobNo=&pageType=all&source=1%3B2%3B5&sort=time", RequestEnum.POST, cookieStr.Serialize("zhaopin.com"));

                    if (!dataResult.IsSuccess)
                    {
                        LogFactory.Warn($"搜索简历列表请求异常！公司ID：{localCompanyInfo.CompanyId}，异常信息：{deilverResult.ErrorMsg}");

                        continue;
                    }

                    var jsonObj = JsonConvert.DeserializeObject(dataResult.Data) as JObject;

                    if ((string)jsonObj?["code"] != "1")
                    {
                        var id = (int)localCompanyInfo.CompanyId;

                        using (var db = new MangningXssDBEntities())
                        {
                            var companyStaff = db.ZhaopinStaff.FirstOrDefault(f => f.CompanyId == id);

                            if (companyStaff != null) companyStaff.Cookie = "";

                            db.TransactionSaveChanges();
                        }

                        var count = localCompanyInfoQueue.Count;

                        for (var i = 0; i < count; i++)
                        {
                            var temp = localCompanyInfoQueue.Dequeue();

                            if (temp.CompanyId != localCompanyInfo.CompanyId)
                            {
                                localCompanyInfoQueue.Enqueue(temp);
                            }
                        }

                        LogFactory.Warn($"企业帐号简历列表获取异常！公司ID：{localCompanyInfo.CompanyId}，异常信息：{jsonObj?["message"]}");

                        continue;
                    }

                    var resumes = jsonObj?["data"]["deal"]["results"] as JArray;

                    if (resumes == null)
                    {
                        LogFactory.Warn($"搜索简历列表 Josn 解析异常！公司ID：{localCompanyInfo.CompanyId}，异常 Josn：{dataResult.Data}");

                        continue;
                    }

                    localCompanyInfoQueue.Enqueue(localCompanyInfo);

                    var resume = resumes.FirstOrDefault(a => a["userId"] == userResume.UserId && Convert.ToDateTime(a["createTime"]) > DateTime.UtcNow.AddMinutes(-5));

                    if (resume != null)
                    {
                        if (!resume.ToString().Contains("<script"))
                        {
                            LogFactory.Warn($"简历未处理，用户：{userResume.Username}");

                            code = -1;

                            return false;
                        }

                        checkRecordDictionary[userResume.UserId] = DateTime.UtcNow;

                        Console.WriteLine($"用户可用！公司：{localCompanyInfo.Name}，用户：{userResume.Username}");

                        return true;
                    }

                    Console.WriteLine($"用户不可用！公司：{localCompanyInfo.Name}，用户：{userResume.Username}");

                    return false;
                }
            }
        }

        /// <summary>
        /// 初始化启动
        /// </summary>
        public static void Init()
        {
            Task.WaitAll(new List<Task>
            {
                new Action(DeilverTaskScheduling).LoopStartTask(TimeSpan.FromHours(1)), // 自动添加投递任务
                new Action(DeilverTask).LoopStartTask(TimeSpan.FromSeconds(10)) // 自动投递
            }.ToArray());
        }
    }
}