using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aliyun.OSS;
using Badoucai.EntityFramework.MySql;
using Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB;
using Badoucai.Library;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Badoucai.Business.Model;

namespace Badoucai.Business.Zhaopin
{
    public class WatchOldResumeBusiness
    {
        private static OssClient newOss;

        private static readonly string newBucket = ConfigurationManager.AppSettings["Oss.Mangning.Bucket"];

        private static readonly string updateRange = ConfigurationManager.AppSettings["UpdateRange"];

        private static int totalUpload;

        private const string savePath = @"D:\Badoucai\WatchResume";

        public WatchOldResumeBusiness()
        {
            newOss = new OssClient(
                ConfigurationManager.AppSettings["Oss.Mangning.Url"],
                ConfigurationManager.AppSettings["Oss.Mangning.KeyId"],
                ConfigurationManager.AppSettings["Oss.Mangning.KeySecret"]);
        }

        public ZhaopinCleaningProcedure GetSingleProcedure()
        {
            using (var db = new MangningXssDBEntities())
            {
                return db.ZhaopinCleaningProcedure.FirstOrDefault(f => f.IsOnline == false && f.IsEnable);
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

        public bool QueryResumeIsExists(string resumeNumber, DateTime? refreshTime = null)
        {
            using (var db = new MangningXssDBEntities())
            {
                var exists = db.ZhaopinWatchedResume.AsNoTracking().Any(f => f.ResumeNumber == resumeNumber);

                if (exists) return true;

                var resume = db.ZhaopinResume.FirstOrDefault(f => f.RandomNumber == resumeNumber);

                if (resume != null)
                {
                    resume.RefreshTime = refreshTime;

                    db.SaveChanges();

                    return true;
                }
            }

            using (var db = new BadoucaiAliyunDBEntities())
            {
                return db.CoreResumeReferenceMapping.AsNoTracking().Any(a => a.Key == "ResumeNumber" && a.Source == "ZHAOPIN" && a.Value.StartsWith(resumeNumber));
            }
        }

        public void SetSearchStatus(int id, short status, int quantity, int lastWatchPage = -1)
        {
            using (var db = new MangningXssDBEntities())
            {
                var condition = db.ZhaopinSearchCondition.FirstOrDefault(f => f.Id == id);

                if (condition == null) return;

                condition.Status = status;

                condition.Quantity = quantity;

                condition.LastSearchDate = DateTime.UtcNow;

                if(lastWatchPage > 0) condition.LastWatchPage = lastWatchPage;

                db.SaveChanges();
            }
        }

        public void RemoveCookie(string account, int todayWatch)
        {
            using (var db = new MangningXssDBEntities())
            {
                var pro = db.ZhaopinCleaningProcedure.FirstOrDefault(f => f.Account == account);

                if (pro != null)
                {
                    pro.Cookie = "";

                    pro.TodayWatch = todayWatch;

                    pro.IsOnline = false;

                    db.SaveChanges();
                }
            }
        }

        public void SaveResumeInfo(ResumeMatchResult model)
        {
            using (var xdb = new MangningXssDBEntities())
            {
                var user = xdb.ZhaopinUser.FirstOrDefault(f => f.Id == model.UserId);

                if (user == null)
                {
                    xdb.ZhaopinUser.Add(new ZhaopinUser
                    {
                        Id = model.UserId,
                        Source = "Watch",
                        ModifyTime = model.ModifyTime,
                        Cellphone = model.Cellphone,
                        Email = model.Email,
                        Name = model.Name,
                        UpdateTime = DateTime.UtcNow,
                        CreateTime = model.CreateTime
                    });
                }
                else
                {
                    user.ModifyTime = model.ModifyTime;
                    user.UpdateTime = DateTime.UtcNow;
                    user.Name = model.Name;
                }

                var resumeEntity = xdb.ZhaopinResume.FirstOrDefault(f => f.Id == model.ResumeId);

                var userExtId = Regex.IsMatch(model.UserExtId, @"^J[MRSL]\d{9}$") ? model.UserExtId : string.Empty;

                if (resumeEntity == null)
                {
                    xdb.ZhaopinResume.Add(new ZhaopinResume
                    {
                        Id = model.ResumeId,
                        RandomNumber = model.ResumeNumber,
                        UserId = model.UserId,
                        RefreshTime = model.ModifyTime,
                        UpdateTime = DateTime.UtcNow,
                        UserExtId = userExtId,
                        Source = "Watch"
                    });
                }
                else
                {
                    resumeEntity.RandomNumber = model.ResumeNumber;
                    resumeEntity.UserId = model.UserId;
                    resumeEntity.RefreshTime = model.ModifyTime;
                    if (string.IsNullOrEmpty(resumeEntity.UserExtId)) resumeEntity.UserExtId = userExtId;
                }

                var cache = xdb.ZhaopinMatchedCache.FirstOrDefault(f => f.ResumeId == model.ResumeId);

                if (cache == null)
                {
                    xdb.ZhaopinMatchedCache.Add(new ZhaopinMatchedCache
                    {
                        ResumeId = model.ResumeId,
                        ModifyTime = model.ModifyTime,
                        Cellphone = model.Cellphone,
                        Email = model.Email,
                        Name = model.Name,
                        Path = model.Path,
                        ResumeNumber = model.ResumeNumber,
                        UserExtId = model.UserExtId,
                        UserId = model.UserId
                    });
                }
                else
                {
                    cache.Cellphone = model.Cellphone;
                    cache.Email = model.Email;
                    cache.UserExtId = model.UserExtId;
                }

                var watched = xdb.ZhaopinWatchedResume.FirstOrDefault(f => f.Id == model.ResumeId);

                if (watched == null)
                {
                    xdb.ZhaopinWatchedResume.Add(new ZhaopinWatchedResume
                    {
                        Id = model.ResumeId,
                        ResumeNumber = model.ResumeNumber,
                        CompanyId = model.CompanyId,
                        WatchTime = DateTime.UtcNow
                    });
                }

                xdb.SaveChanges();
            }
        }

        public void Watch()
        {

            //using (var db = new MangningXssDBEntities())
            //{
            //    var maxId = db.ZhaopinCleaningProcedure.Max(m => m.Id);

            //    for (var i = 1; i <= 11; i++)
            //    {
            //        db.ZhaopinCleaningProcedure.Add(new ZhaopinCleaningProcedure
            //        {
            //            Id = (short)(i + maxId),
            //            Account = $"qhzl_{i:000}",
            //            Cookie = null,
            //            IsEnable = true,
            //            IsOnline = false,
            //            Password = "PVLy5rT5",
            //            StartTime = DateTime.Now.AddDays(-1)
            //        });
            //    }

            //    db.SaveChanges();
            //}


            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            var cookieQueue = new ConcurrentQueue<KeyValuePair<KeyValuePair<int, string>, CookieContainer>>();

            var companyDic = new ConcurrentDictionary<string, int>();

            using (var db = new MangningXssDBEntities())
            {
                var companyArr = db.ZhaoPinCompany.Where(w => w.Source.Contains("MANUAL")).Select(s => s.Id).ToArray();

                var paramArr = db.ZhaopinStaff.Where(w => companyArr.Any(a => a == w.CompanyId) && !string.IsNullOrEmpty(w.Cookie)).Select(s => new { s.CompanyId, s.Username, s.Cookie }).ToArray();

                foreach (var item in paramArr)
                {
                    companyDic[item.Username] = 0;

                    cookieQueue.Enqueue(new KeyValuePair<KeyValuePair<int, string>, CookieContainer>(new KeyValuePair<int, string>(item.CompanyId, item.Username), item.Cookie.Serialize(".zhaopin.com")));
                }
            }

            using (var db = new MangningXssDBEntities())
            {
                var list = db.ZhaopinCleaningProcedure.Where(w => w.IsEnable && !w.IsOnline && !string.IsNullOrEmpty(w.Cookie));

                foreach (var item in list)
                {
                    if (item.StartTime < DateTime.Today) item.TodayWatch = 0;

                    companyDic[item.Account] = item.TodayWatch;
                    
                    cookieQueue.Enqueue(new KeyValuePair<KeyValuePair<int, string>, CookieContainer>(new KeyValuePair<int, string>(0, item.Account), item.Cookie.Serialize(".zhaopin.com")));

                    item.IsOnline = true;

                    item.StartTime = DateTime.Now;
                }

                db.SaveChanges();
            }

            Console.WriteLine($"已获取 Cookie 数 => {cookieQueue.Count}");

            for (var j = 0; j < 16; j++)
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
                                var condition = GetSingleCondition(temp.Key.Value);

                                if (condition == null)
                                {
                                    Console.WriteLine($"Company => {temp.Key.Value} 找不到可搜索的条件！");

                                    Thread.Sleep(1000);

                                    continue;
                                }

                                var pageIndex = 1;

                                var pageTotal = 1;

                                var total = 0;

                                var isbreak = false;

                                var workYearsEnd = condition.WorkYears == 30 ? 30 : condition.WorkYears + 1;

                                while (pageIndex < pageTotal + 1)
                                {
                                    var url = $"https://ihrsearch.zhaopin.com/Home/ResultForCustom?SF_1_1_8={condition.Age}%2C{condition.Age}&SF_1_1_9={condition.Gender}&SF_1_1_4={condition.WorkYears}%2C{workYearsEnd}&SF_1_1_5={condition.Degrees}%2C{condition.Degrees}&orderBy=BIRTH_YEAR%2C0&SF_1_1_27=0&SF_1_1_7={updateRange}%2C9&exclude=1&pageIndex={pageIndex}&pageSize=60";

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

                                                RemoveCookie(temp.Key.Value, companyDic[temp.Key.Value]);
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

                                    //var matchs = Regex.Matches(requestResult.Data, "RedirectToRd/(.+?)','(.+?)','(.+?)',this\\);this");
                                    var matchs = Regex.Matches(requestResult.Data, "(?s)RedirectToRd/([^\r\n]+?)','([^\r\n]+?)','([^\r\n]+?)',this\\);this.+?(\\d{2}-\\d{2}-\\d{2})");

                                    var index = 0;

                                    foreach (Match item in matchs)
                                    {
                                        var number = item.Result("$1").Substring(0, 10);

                                        var numberParam = item.Result("$1").Substring(0, item.Result("$1").IndexOf("?", StringComparison.Ordinal));

                                        DateTime updateDateTime;

                                        if (DateTime.TryParse(item.Result("$4"), out updateDateTime))
                                        {
                                            if (QueryResumeIsExists(number, updateDateTime)) continue;
                                        }
                                        else
                                        {
                                            if (QueryResumeIsExists(number)) continue;
                                        }

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
                                                LogFactory.Warn($"Company => {temp.Key.Value} ResumeNumber => {number} 查看详情异常 信息：{(string)jsonObj["message"]} 查看简历份数：{companyDic[temp.Key.Value]}");

                                                if (((string)jsonObj["message"]).Contains("当日查看简历已达上限"))
                                                {
                                                    RemoveCookie(temp.Key.Value, companyDic[temp.Key.Value]);

                                                    isbreak = true;

                                                    break;
                                                }

                                                continue;
                                            }

                                            var resumeData = jsonObj.data;

                                            SaveCacheByOss(new KeyValuePair<KeyValuePair<int, string>, dynamic>(temp.Key, resumeData));

                                            Console.WriteLine($"Company => {temp.Key.Value} 查看简历成功！查看简历份数：{++companyDic[temp.Key.Value]} ResumeNumber => {number} {++index}/{matchs.Count}");
                                        }
                                        catch (Exception ex)
                                        {
                                            while (true)
                                            {
                                                if(ex.InnerException == null) break;

                                                ex = ex.InnerException;
                                            }

                                            LogFactory.Error($"保存简历信息异常！异常信息：{ex.Message} 响应信息：{requestResult.Data}");
                                        }

                                        //if (companyDic[temp.Key.Value] > 1500)
                                        //{
                                        //    RemoveCookie(temp.Key.Value, companyDic[temp.Key.Value]);

                                        //    isbreak = true;

                                        //    break;
                                        //}
                                    }

                                    if (isbreak) break;
                                }

                                if (isbreak) break;

                                SetSearchStatus(condition.Id, 1, total);
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

        private void SaveCacheByOss(KeyValuePair<KeyValuePair<int, string>, dynamic> tempData)
        {
            var resumeData = tempData.Value;

            var resumeDetail = JsonConvert.DeserializeObject(resumeData.detialJSonStr.ToString());

            var resumeId = resumeData.resumeId != null ? (int)resumeData.resumeId : resumeDetail.ResumeId != null ? (int)resumeDetail.ResumeId : 0;

            var resumeNumber = ((string)resumeData.resumeNo).Substring(0, 10);

            var model = new ResumeMatchResult
            {
                Cellphone = null,
                Email = null,
                ModifyTime = BaseFanctory.GetTime((string)resumeDetail.DateModified).ToUniversalTime(),
                Name = (string)resumeData.userDetials.userName,
                ResumeId = resumeId,
                ResumeNumber = resumeNumber,
                UserExtId = resumeDetail.UserMasterExtId.ToString(),
                UserId = (int)resumeData.userDetials.userMasterId,
                Path = $@"{savePath}\{resumeId}.json",
                CompanyId = tempData.Key.Key,
                CreateTime = BaseFanctory.GetTime((string)resumeDetail.DateCreated).ToUniversalTime()
            };

            uploadOssActionBlock.Post(new KeyValuePair<string, dynamic>($@"{savePath}\{resumeId}.json", tempData.Value));

            this.SaveResumeInfo(model);
        }

        /// <summary>
        /// 并发工作流（上传）
        /// </summary>
        public static readonly ActionBlock<KeyValuePair<string, dynamic>> uploadOssActionBlock = new ActionBlock<KeyValuePair<string, dynamic>>(tempData =>
        {
            try
            {
                File.WriteAllText(tempData.Key, JsonConvert.SerializeObject(tempData.Value));

                UploadResumeToOss(tempData.Key);
            }
            catch (Exception ex)
            {
                while (true)
                {
                    if (ex.InnerException == null) break;

                    ex = ex.InnerException;
                }

                LogFactory.Error($"上传简历异常！异常信息：{ex.Message}");

            }
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 });


        private static void UploadResumeToOss(string path)
        {
            while (true)
            {
                try
                {
                    using (var stream = new MemoryStream(GZip.Compress(File.ReadAllBytes(path))))
                    {
                        var businessId = Path.GetFileNameWithoutExtension(path);

                        newOss.PutObject(newBucket, $"WatchResume/{businessId}", stream);

                        Interlocked.Increment(ref totalUpload);

                        Console.WriteLine($"上传成功！ path：{path} 待上传数：{uploadOssActionBlock.InputCount} 已上传：{totalUpload}");

                        File.Delete(path);
                    }

                    break;
                }
                catch (Exception ex)
                {
                    LogFactory.Error($"缓存简历上传OSS异常！异常信息：{ex.Message}");
                }
            }
        }

        private static int buquanTotal;

        public void Buquan()
        {
            var queue = new ConcurrentQueue<KeyValuePair<int,KeyValuePair<string,DateTime>>>();

            var index = 0;

            Task.Run(() =>
            {
                while (true)
                {
                    KeyValuePair<int, KeyValuePair<string, DateTime>> temp;

                    if (!queue.TryDequeue(out temp))
                    {
                        Thread.Sleep(10);

                        continue;
                    }

                    using (var db = new MangningXssDBEntities())
                    {
                        var resume = db.ZhaopinResume.FirstOrDefault(f => f.Id == temp.Key);

                        if (resume == null)
                        {
                            LogFactory.Warn($"找不到简历 ResumeId =>{temp.Key}");

                            continue;
                        }

                        var user = db.ZhaopinUser.FirstOrDefault(f => f.Id == resume.UserId);

                        if (user == null)
                        {
                            LogFactory.Warn($"找不到用户  ResumeId =>{temp.Key} UserId =>{resume.UserId}");

                            continue;
                        }

                        if (string.IsNullOrEmpty(temp.Value.Key) && string.IsNullOrEmpty(user.Cellphone))
                        {
                            db.ZhaopinResume.Remove(resume);

                            db.ZhaopinUser.Remove(user);

                            Console.WriteLine($"删除成功！ResumeId=>{temp.Key} {++index}/{buquanTotal}");
                        }
                        else
                        {
                            resume.UserExtId = temp.Value.Key;

                            user.CreateTime = temp.Value.Value;

                            Console.WriteLine($"补全成功！{++index}/{buquanTotal}");
                        }

                        db.SaveChanges();
                    }

                    
                }
            });

            using (var db = new MangningXssDBEntities())
            {
                var idArray = db.ZhaopinResume.Where(w => w.Source == "Watch").Select(s => s.Id).ToArray();

                buquanTotal = idArray.Length;

                foreach (var businessId in idArray)
                {
                    try
                    {
                        var streamContent = newOss.GetObject(newBucket, $"WatchResume/{businessId}").Content;

                        using (var stream = new MemoryStream())
                        {
                            var bytes = new byte[1024];

                            int len;

                            while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
                            {
                                stream.Write(bytes, 0, len);
                            }

                            var jsonContent = Encoding.UTF8.GetString(GZip.Decompress(stream.ToArray()));

                            var resumeData = JsonConvert.DeserializeObject<dynamic>(jsonContent);

                            var resumeDetail = JsonConvert.DeserializeObject<dynamic>((string)resumeData.detialJSonStr);

                            var createDate = BaseFanctory.GetTime((string)resumeDetail.DateCreated).ToUniversalTime();

                            var extId = resumeDetail.UserMasterExtId.ToString();

                            queue.Enqueue(new KeyValuePair<int, KeyValuePair<string, DateTime>>(businessId, new KeyValuePair<string, DateTime>(extId, createDate)));
                        }
                    }
                    catch (Exception ex)
                    {
                        LogFactory.Warn($"Oss 获取简历异常！ ResumeId => {businessId} 异常原因 => {ex.Message}");

                        queue.Enqueue(new KeyValuePair<int, KeyValuePair<string, DateTime>>(businessId, new KeyValuePair<string, DateTime>("", DateTime.Now)));
                    }
                }
            }
        }
    }
}