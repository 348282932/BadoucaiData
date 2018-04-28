using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Aliyun.OSS;
using Badoucai.EntityFramework.MySql;
using Badoucai.EntityFramework.PostgreSql.ResumeMatch_DB;
using Badoucai.Library;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Badoucai.Business.Zhaopin
{
    public class MatchZhaopinBusiness
    {
        private static OssClient newOss;

        private static readonly string newBucket = ConfigurationManager.AppSettings["Oss.Mangning.Bucket"];

        private static readonly object lockObj = new object();

        public MatchZhaopinBusiness()
        {
            newOss = new OssClient(
                ConfigurationManager.AppSettings["Oss.Mangning.Url"],
                ConfigurationManager.AppSettings["Oss.Mangning.KeyId"],
                ConfigurationManager.AppSettings["Oss.Mangning.KeySecret"]);
        }

        public void MatchZhaopin()
        {
            var resumeQueue = new ConcurrentQueue<OldResumeSummary>();

            var cookieQueue = new ConcurrentQueue<QueueParam>();

            var todayString = DateTime.Today.ToString("yyyy-MM-dd");

            using (var db = new MangningXssDBEntities())
            {
                var companyArr = db.ZhaoPinCompany.Where(w => w.Source.Contains("MANUAL")).Select(s => s.Id).ToArray();

                var paramArr = db.ZhaopinStaff.Where(w => companyArr.Any(a => a == w.CompanyId) && !string.IsNullOrEmpty(w.Cookie)).Select(s => new { s.CompanyId, s.Cookie }).ToArray();

                foreach (var param in paramArr)
                {
                    var task = db.ZhaopinResumeMatchLimit.FirstOrDefault(f => f.CompanyId == param.CompanyId);

                    if (task == null) continue;

                    var todayTask = db.ZhaopinResumeMatchStatistic.FirstOrDefault(f => f.CompanyId == param.CompanyId && f.Date == todayString);

                    if (todayTask == null)
                    {
                        todayTask = new ZhaopinResumeMatchStatistic
                        {
                            Date = todayString,
                            CompanyId = param.CompanyId,
                            MatchedCount = 0,
                            SearchCount = 0,
                            WatchCount = 0
                        };

                        db.ZhaopinResumeMatchStatistic.Add(todayTask);

                        db.SaveChanges();
                    }

                    if(todayTask.SearchCount == task.DailySearchCount || todayTask.WatchCount == task.DailyWatchCount) continue;

                    cookieQueue.Enqueue(new QueueParam
                    {
                        CompanyId = param.CompanyId,
                        Cookie = param.Cookie,
                        DailySeachCount = task.DailySearchCount,
                        DailyWatchCount = task.DailyWatchCount,
                        MatchCount = todayTask.MatchedCount,
                        SeachCount = todayTask.SearchCount,
                        WatchCount = todayTask.WatchCount + todayTask.MatchedCount
                    });
                }
            }


            Task.Run(() =>
            {
                while (true)
                {
                    if (resumeQueue.Count < 10)
                    {
                        using (var db = new ResumeMatchDBEntities())
                        {
                            var resumeList = db.OldResumeSummary.Where(w => w.ResumeId.Length == 10 && !w.IsMatched && w.Status == 0).Take(1000).ToList();

                            resumeList.ForEach(f =>
                            {
                                resumeQueue.Enqueue(f);
                            });
                        }
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            });

            for (var j = 0; j < 16; j++)
            {
                Task.Run(() =>
                {
                    using (var db = new ResumeMatchDBEntities())
                    {
                        while (true)
                        {
                            #region 匹配

                            OldResumeSummary resume;

                            if (!resumeQueue.TryDequeue(out resume)) continue;

                            var startNum = 0;

                            var resumeTemp = resume;

                            resume = db.OldResumeSummary.FirstOrDefault(f => f.Id == resumeTemp.Id);

                            QueueParam cookie;

                            if (!cookieQueue.TryDequeue(out cookie)) return;

                            try
                            {
                                using (var xdb = new MangningXssDBEntities())
                                {
                                    var filePath = $@"E:\Data\智联招聘\{resume.Template}\{resume.ResumeId}.{Path.GetFileNameWithoutExtension(resume.Template)}";

                                    if (!File.Exists(filePath))
                                    {
                                        LogFactory.Warn($"指定路径不存在！ResumeNumber=>{resume.ResumeId} Path=>{filePath}");

                                        resume.Status = 1;

                                        resume.MatchTime = DateTime.Now;

                                        db.SaveChanges();

                                        continue;
                                    }

                                    var sourceCode = File.ReadAllText(filePath);

                                    //var sourceCode = string.Empty;

                                    var genderMatch = Regex.Match(sourceCode, "(男|女)");

                                    var gender = string.Empty;

                                    if (genderMatch.Success) gender = genderMatch.Value == "男" ? "1" : "2";

                                    var matchs = Regex.Matches(sourceCode, "[\u4e00-\u9fa5]{4,11}有限公司[\u4e00-\u9fa5]{0,6}");

                                    var companys = new List<string>();

                                    if (matchs.Count == 0)
                                    {
                                        if (cookie.SeachCount - cookie.WatchCount < 50)
                                        {
                                            Console.WriteLine($"该简历未匹配到公司！ResumeNumber=> {resume.ResumeId}");

                                            resume.Status = 1;

                                            resume.MatchTime = DateTime.Now;

                                            db.SaveChanges();

                                            cookieQueue.Enqueue(cookie);

                                            continue;
                                        }

                                        companys.Add(string.Empty);
                                    }

                                    for (var i = 0; i < matchs.Count; i++)
                                    {
                                        if (i == 2) break;

                                        companys.Add(matchs[i].Value);
                                    }

                                    var isMatched = true;

                                    var age = string.Empty;

                                    for (var i = 0; i < companys.Count; i++)
                                    {
                                        var companyName = companys[i];

                                        var statistic = xdb.ZhaopinResumeMatchStatistic.FirstOrDefault(f => f.Date == todayString && f.CompanyId == cookie.CompanyId);

                                        if (statistic == null) continue;

                                        var cookieContainer = cookie.Cookie.Serialize(".zhaopin.com");

                                        var start = string.IsNullOrEmpty(companyName) ? startNum * 100 : 0;

                                        var paramDictionary = new Dictionary<string, string>
                                        {
                                            { "keywords", "的" },
                                            { "startNum", $"{start}" },
                                            { "rowsCount", "100" },
                                            { "sortColumnName", "sortUpDate" },
                                            { "sortColumn", "sortUpDate desc" },
                                            { "onlyHasImg", "false" },
                                            { "anyKeyWord", "false" },
                                            { "sex", gender },
                                            { "companyName", companyName },
                                            { "onlyLastWork", "false" },
                                            { "ageStart",age  },
                                            { "ageEnd",age }
                                            
                                        };

                                        var requestResult = HttpClientFactory.RequestForString("https://ihr.zhaopin.com/resumesearch/search.do", HttpMethod.Post, paramDictionary, cookieContainer);

                                        if (!requestResult.IsSuccess)
                                        {
                                            isMatched = false;

                                            break;
                                        }

                                        var jObject = JsonConvert.DeserializeObject(requestResult.Data) as JObject;

                                        if (jObject["code"] != null)
                                        {
                                            LogFactory.Warn($"CompanyId => {cookie.CompanyId} 搜索异常 异常信息：{(string)jObject["message"]}");

                                            isMatched = false;

                                            return;
                                        }

                                        statistic.SearchCount += 1;

                                        ++cookie.SeachCount;

                                        Console.WriteLine($"CompanyId => {cookie.CompanyId} 搜索公司=> {companyName} 性别=> {genderMatch.Value} 今日请求次数：{cookie.SeachCount} ");

                                        xdb.SaveChanges();

                                        var matchResumes = (JArray)jObject?["results"];

                                        if (matchResumes == null) continue;

                                        if (matchResumes.Count == 0) continue;

                                        var matchResume = matchResumes.FirstOrDefault(a => ((string)a["number"]).Substring(0, 10) == resume.ResumeId);

                                        var resumes = matchResumes.Where(w => DateTime.Parse((string)w["modifyDate"]) > DateTime.Today.AddDays(-2));

                                        foreach (var item in resumes)
                                        {
                                            var wcount = matchResume == null ? cookie.WatchCount : cookie.WatchCount + 1;

                                            if (cookie.SeachCount <= wcount) break;

                                            var number = ((string)item["number"]).Substring(0, 10);

                                            if (xdb.ZhaopinResume.Any(a => a.RandomNumber == number)) continue;

                                            lock (lockObj)
                                            {
                                                if (xdb.ZhaopinWatchedResume.Any(a => a.ResumeNumber == number)) continue;

                                                requestResult = HttpClientFactory.RequestForString($"http://ihr.zhaopin.com/resumesearch/getresumedetial.do?resumeNo={(string)item["id"]}_1&resumeSource=1&key=&{(string)item["valResumeTimeStr"]}", HttpMethod.Get, null, cookieContainer);

                                                if (requestResult.IsSuccess)
                                                {
                                                    var resumeData = JsonConvert.DeserializeObject<dynamic>(requestResult.Data).data;

                                                    var resumeDetail = JsonConvert.DeserializeObject(resumeData.detialJSonStr.ToString());

                                                    var resumeid = resumeData.resumeId != null ? (int)resumeData.resumeId : resumeDetail.ResumeId != null ? (int)resumeDetail.ResumeId : 0;

                                                    statistic.WatchCount += 1;

                                                    Console.WriteLine($"CompanyId => {cookie.CompanyId} 查看简历成功！查看简历份数：{++cookie.WatchCount} ResumeNumber => {number}");

                                                    xdb.ZhaopinWatchedResume.Add(new ZhaopinWatchedResume
                                                    {
                                                        Id = resumeid,
                                                        ResumeNumber = number,
                                                        CompanyId = cookie.CompanyId,
                                                        WatchTime = DateTime.UtcNow
                                                    });

                                                    xdb.SaveChanges();
                                                }
                                            }
                                        }

                                        if (cookie.SeachCount - cookie.WatchCount > 50 && string.IsNullOrEmpty(companyName) && matchResume == null)
                                        {
                                            i--;

                                            if (startNum == 40)
                                            {
                                                if (string.IsNullOrEmpty(age))
                                                {
                                                    age = "18";
                                                }
                                                else
                                                {
                                                    age = (Convert.ToInt32(age) + 1).ToString();
                                                }

                                                startNum = 0;
                                            }
                                            else
                                            {
                                                startNum++;
                                            }

                                            continue;
                                        }

                                        if (matchResume == null) continue;

                                        var resumeNo = (string)matchResume["id"] + "_1";

                                        var resumeNumber = ((string)matchResume["number"]).Substring(0, 10);

                                        if (xdb.ZhaopinResume.Any(a => a.RandomNumber == resumeNumber)) continue;

                                        var valResumeTimeStr = (string)matchResume["valResumeTimeStr"];

                                        requestResult = HttpClientFactory.RequestForString($"http://ihr.zhaopin.com/resumesearch/getresumedetial.do?resumeNo={resumeNo}&resumeSource=1&key=&{valResumeTimeStr}", HttpMethod.Get, null, cookieContainer);

                                        if (!requestResult.IsSuccess) continue;

                                        ++cookie.WatchCount;

                                        var resumeJson = JsonConvert.DeserializeObject<dynamic>(requestResult.Data).data;

                                        var detail = JsonConvert.DeserializeObject(resumeJson.detialJSonStr.ToString());

                                        var resumeId = resumeJson.resumeId != null ? (int)resumeJson.resumeId : detail.ResumeId != null ? (int)detail.ResumeId : 0;

                                        if (xdb.ZhaopinMatchedResume.Any(a => a.Id == resumeId))
                                        {
                                            statistic.MatchedCount += 1;

                                            LogFactory.Warn($"匹配到重复简历！ResumeId=>{resumeId} ResumeNumber=>{resumeNumber}");

                                            continue;
                                        }

                                        if (resumeId == 0)
                                        {
                                            LogFactory.Warn($"CompanyId => {cookie.CompanyId} 解析异常！ResumeId 为空, ResumeNumber：{resumeNumber}");

                                            continue;
                                        }

                                        var userId = (int)resumeJson.userDetials.userMasterId;

                                        var user = xdb.ZhaopinUser.FirstOrDefault(f => f.Id == userId);

                                        if (user != null)
                                        {
                                            if (!user.Source.Contains("MANUAL"))
                                            {
                                                user.Id = userId;
                                                user.Source = "XSS";
                                                user.ModifyTime = BaseFanctory.GetTime((string)detail.DateModified).ToUniversalTime();
                                                user.CreateTime = BaseFanctory.GetTime((string)detail.DateCreated).ToUniversalTime();
                                                user.Cellphone = resume.Cellphone;
                                                user.Email = resume.Email;
                                                user.Name = resumeJson.userDetials.userName.ToString();
                                                user.UpdateTime = DateTime.UtcNow;
                                                user.Username = resumeJson.userDetials.email.ToString();
                                            }
                                        }
                                        else
                                        {
                                            xdb.ZhaopinUser.Add(new ZhaopinUser
                                            {
                                                Id = userId,
                                                Source = "XSS",
                                                ModifyTime = BaseFanctory.GetTime((string)detail.DateModified).ToUniversalTime(),
                                                CreateTime = BaseFanctory.GetTime((string)detail.DateCreated).ToUniversalTime(),
                                                Cellphone = resume.Cellphone,
                                                Email = resume.Email,
                                                Name = resumeJson.userDetials.userName.ToString(),
                                                UpdateTime = DateTime.UtcNow
                                            });
                                        }

                                        var resumeEntity = xdb.ZhaopinResume.FirstOrDefault(f => f.Id == resumeId);

                                        if (resumeEntity == null)
                                        {
                                            xdb.ZhaopinResume.Add(new ZhaopinResume
                                            {
                                                Id = resumeId,
                                                RandomNumber = resumeNumber,
                                                UserId = userId,
                                                RefreshTime = BaseFanctory.GetTime((string)detail.DateModified).ToUniversalTime(),
                                                UpdateTime = DateTime.UtcNow,
                                                UserExtId = detail.UserMasterExtId.ToString(),
                                                DeliveryNumber = resumeEntity?.DeliveryNumber,
                                                Source = resumeEntity == null ? "XSS" : resumeEntity.Source
                                            });
                                        }
                                        else
                                        {
                                            resumeEntity.RandomNumber = resumeNumber;
                                            resumeEntity.UserId = userId;
                                            resumeEntity.RefreshTime = BaseFanctory.GetTime((string)detail.DateModified).ToUniversalTime();
                                            resumeEntity.UpdateTime = DateTime.UtcNow;
                                            resumeEntity.UserExtId = detail.UserMasterExtId.ToString();
                                            resumeEntity.DeliveryNumber = resumeEntity?.DeliveryNumber;
                                            resumeEntity.Source = resumeEntity == null ? "XSS" : resumeEntity.Source;
                                        }

                                        var path = $"{ConfigurationManager.AppSettings["Resume.SavePath"]}{DateTime.Now:yyyyMMdd}";

                                        if (!Directory.Exists(path))
                                        {
                                            Directory.CreateDirectory(path);
                                        }

                                        var resumePath = $@"{path}\{resumeId}.json";

                                        resumeJson.userDetials.email = resume.Email;

                                        resumeJson.userDetials.mobilePhone = resume.Cellphone;

                                        File.WriteAllText(resumePath, JsonConvert.SerializeObject(resumeJson));

                                        uploadOssActionBlock.Post(resumePath);

                                        statistic.MatchedCount += 1;

                                        Console.WriteLine($"CompanyId => {cookie.CompanyId} 搜索简历成功！匹配成功 {++cookie.MatchCount} 份， ResumeNumner=>{resumeNumber}，ResumeId=>{resumeId}");

                                        xdb.ZhaopinMatchedResume.Add(new ZhaopinMatchedResume
                                        {
                                            Id = resumeId,
                                            CompanyId = cookie.CompanyId,
                                            MatchTime = DateTime.UtcNow
                                        });

                                        xdb.SaveChanges();

                                        resume.IsMatched = true;

                                        break;
                                    }

                                    if (cookie.SeachCount < cookie.DailySeachCount) cookieQueue.Enqueue(cookie);

                                    if (isMatched)
                                    {
                                        resume.Status = 99;

                                        resume.MatchTime = DateTime.Now;
                                    }

                                    db.SaveChanges();
                                }
                            }
                            catch (Exception ex)
                            {
                                //while (true)
                                //{
                                //    if(ex.InnerException == null) break;

                                //    ex = ex.InnerException;
                                //}

                                if (cookie.SeachCount < cookie.DailySeachCount) cookieQueue.Enqueue(cookie);

                                LogFactory.Warn($"程序异常, 异常信息：{ex.Message} 堆栈：{ex.StackTrace}");
                            }

                            #endregion
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 上传OSS
        /// </summary>
        /// <param name="path"></param>
        private static void UploadResumeToOss(string path)
        {
            try
            {
                using (var stream = new MemoryStream(GZip.Compress(File.ReadAllBytes(path))))
                {
                    var businessId = Path.GetFileNameWithoutExtension(path);

                    newOss.PutObject(newBucket, $"Zhaopin/Resume/{businessId}", stream);

                    File.Delete(path);

                    Console.WriteLine($"上传成功！ path:{path}，OSS 队列剩余：{uploadOssActionBlock.InputCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"上传异常！{ex.Message}, path:{path}");

                uploadOssActionBlock.Post(path);
            }

        }

        /// <summary>
        /// 并发工作流（上传）
        /// </summary>
        public static readonly ActionBlock<string> uploadOssActionBlock = new ActionBlock<string>(path =>
        {
            UploadResumeToOss(path);

        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 });
    }

    public class QueueParam
    {
        public int CompanyId { get; set; }

        public string Cookie { get; set; }

        public int MatchCount { get; set; }

        public int WatchCount { get; set; }

        public int SeachCount { get; set; }

        public int DailySeachCount { get; set; }

        public int DailyWatchCount { get; set; }
    }
}