using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Web;
using Aliyun.OSS;
using Badoucai.Business.Model;
using Badoucai.EntityFramework.MySql;
using Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB;
using Badoucai.EntityFramework.PostgreSql.ResumeMatch_DB;
using Badoucai.Library;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace Badoucai.Business.Zhaopin
{
    public class MatchResumeLocationBusiness
    {
        private static OssClient newOss;

        private static readonly string newBucket = ConfigurationManager.AppSettings["Oss.Mangning.Bucket"];

        private static int totalUpload;

        private const string savePath = @"D:\Badoucai\WatchResume";

        private static readonly ConcurrentQueue<SearchResumeModel> searchQueue = new ConcurrentQueue<SearchResumeModel>();

        private static int totalMatched;

        public MatchResumeLocationBusiness()
        {
            newOss = new OssClient(
                ConfigurationManager.AppSettings["Oss.Mangning.Url"],
                ConfigurationManager.AppSettings["Oss.Mangning.KeyId"],
                ConfigurationManager.AppSettings["Oss.Mangning.KeySecret"]);
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

        public static void SaveResumeInfo(ResumeMatchResult model)
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
                    resumeEntity.UpdateTime = DateTime.UtcNow;
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

        public void Match()
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            var cookieQueue = new ConcurrentQueue<KeyValuePair<KeyValuePair<int, string>, CookieContainer>>();

            var companyDic = new ConcurrentDictionary<string, int>();

            //using (var db = new MangningXssDBEntities())
            //{
            //    var companyArr = db.ZhaoPinCompany.Where(w => w.Source.Contains("MANUAL")).Select(s => s.Id).ToArray();

            //    var paramArr = db.ZhaopinStaff.Where(w => companyArr.Any(a => a == w.CompanyId) && !string.IsNullOrEmpty(w.Cookie)).Select(s => new { s.CompanyId, s.Username, s.Cookie }).ToArray();

            //    foreach (var item in paramArr)
            //    {
            //        companyDic[item.Username] = 0;

            //        cookieQueue.Enqueue(new KeyValuePair<KeyValuePair<int, string>, CookieContainer>(new KeyValuePair<int, string>(item.CompanyId, item.Username), item.Cookie.Serialize(".zhaopin.com")));
            //    }
            //}

            using (var db = new MangningXssDBEntities())
            {
                var list = db.ZhaopinCleaningProcedure.Where(w => w.IsEnable/* && !w.IsOnline*/ && !string.IsNullOrEmpty(w.Cookie));

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

            Task.Run(() =>
            {
                while (true)
                {
                    if (searchQueue.Count > 500)
                    {
                        Thread.Sleep(100);

                        continue;
                    }

                    var list = this.GetOldResumes();

                    foreach (var item in list)
                    {
                        searchQueue.Enqueue(item);
                    }
                }
            });

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

                                Console.WriteLine($"Company => {temp.Key.Value} 找不到可用Cookie！");

                                continue;
                            }

                            while (true)
                            {
                                SearchResumeModel search;

                                if (!searchQueue.TryDequeue(out search))
                                {
                                    Console.WriteLine($"Company => {temp.Key.Value} 找不到可搜索的条件！");

                                    Thread.Sleep(1000);

                                    continue;
                                }

                                var pageIndex = 1;

                                var pageTotal = 1;

                                var total = 0;

                                var isbreak = false;

                                var gender = search.Gender == "男" ? 1 : 2;

                                var isMatched = false;

                                foreach (var company in search.Companys)
                                {
                                    while (pageIndex < pageTotal + 1)
                                    {
                                        var url = $"https://ihrsearch.zhaopin.com/Home/ResultForCustom?SF_1_1_9={gender}&SF_1_1_25=COMPANY_NAME_ALL:{HttpUtility.UrlEncode(company)}&orderBy=BIRTH_YEAR%2C0&SF_1_1_27=0&exclude=1&pageIndex={pageIndex}&pageSize=60";

                                        var requestResult = RequestFactory.QueryRequest(url, cookieContainer: temp.Value, referer: url);

                                        if (!requestResult.IsSuccess)
                                        {
                                            LogFactory.Warn($"Company => {temp.Key.Value} 条件搜索异常！异常原因=>{requestResult.ErrorMsg} Condition=>{JsonConvert.SerializeObject(search)}");

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

                                        if(pageTotal > 1) break;

                                        pageIndex++;

                                        var matchs = Regex.Matches(requestResult.Data, "(?s)RedirectToRd/([^\r\n]+?)','([^\r\n]+?)','([^\r\n]+?)',this\\);this.+?(\\d{2}-\\d{2}-\\d{2})");

                                        var index = 0;

                                        foreach (Match item in matchs)
                                        {
                                            var number = item.Result("$1").Substring(0, 10);

                                            var numberParam = item.Result("$1").Substring(0, item.Result("$1").IndexOf("?", StringComparison.Ordinal));

                                            var cache = GetResumeOfCache(number);

                                            if (cache == null)
                                            {
                                                DateTime updateDateTime;

                                                if (DateTime.TryParse(item.Result("$4"), out updateDateTime)) QueryResumeIsExists(number, updateDateTime);

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

                                                    var userId = (string)resumeData.userDetials.userMasterId;

                                                    if (search.SearchResumeId.Substring(2, 8) == (userId.Length == 8 ? userId : userId.Substring(1)))
                                                    {
                                                        Console.WriteLine($"匹配简历成功！ResumeNumber=>{number} 今日匹配成功数=> {++totalMatched}");

                                                        resumeData.userDetials.mobilePhone = search.Cellphone;

                                                        resumeData.userDetials.email = search.Email;

                                                        isMatched = true;
                                                    }

                                                    SaveCacheByOss(new KeyValuePair<KeyValuePair<int, string>, dynamic>(temp.Key, resumeData));

                                                    Console.WriteLine($"Company => {temp.Key.Value} 查看简历成功！查看简历份数：{++companyDic[temp.Key.Value]} ResumeNumber => {number} {++index}/{matchs.Count}");

                                                    if(isMatched) break;
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
                                            else
                                            {
                                                var userId = cache.UserId.ToString();

                                                if (search.SearchResumeId.Substring(2, 8) == (userId.Length == 8 ? userId : userId.Substring(1)))
                                                {
                                                    Console.WriteLine($"匹配简历成功！ResumeNumber=>{number} 今日匹配成功数=> {++totalMatched}");

                                                    using (var xdb = new MangningXssDBEntities())
                                                    {
                                                        var model = xdb.ZhaopinMatchedCache.FirstOrDefault(f => f.ResumeId == cache.ResumeId);

                                                        if (model != null)
                                                        {
                                                            model.Cellphone = search.Cellphone;

                                                            model.Email = search.Email;

                                                            xdb.SaveChanges();
                                                        }
                                                    }

                                                    isMatched = true;

                                                    break;
                                                }
                                            }
                                        }

                                        if (isbreak || isMatched) break;
                                    }

                                    if (isbreak || isMatched) break;
                                }

                                ChangeResumeStatus(search.SearchResumeId, isMatched);

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

        private void SaveCacheByOss(KeyValuePair<KeyValuePair<int, string>, dynamic> tempData)
        {
            var resumeData = tempData.Value;

            var resumeDetail = JsonConvert.DeserializeObject(resumeData.detialJSonStr.ToString());

            var resumeId = resumeData.resumeId != null ? (int)resumeData.resumeId : resumeDetail.ResumeId != null ? (int)resumeDetail.ResumeId : 0;

            var resumeNumber = ((string)resumeData.resumeNo).Substring(0, 10);

            var model = new ResumeMatchResult
            {
                Cellphone = (string)resumeData.userDetials.mobilePhone,
                Email = (string)resumeData.userDetials.email,
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

            uploadOssActionBlock.Post(new KeyValuePair<ResumeMatchResult, dynamic>(model, tempData.Value));
        }

        /// <summary>
        /// 并发工作流（上传）
        /// </summary>
        public static readonly ActionBlock<KeyValuePair<ResumeMatchResult, dynamic>> uploadOssActionBlock = new ActionBlock<KeyValuePair<ResumeMatchResult, dynamic>>(tempData =>
        {
            try
            {
                File.WriteAllText($@"{savePath}\{tempData.Key.ResumeId}.json", JsonConvert.SerializeObject(tempData.Value));

                UploadResumeToOss($@"{savePath}\{tempData.Key.ResumeId}.json");

                SaveResumeInfo(tempData.Key);
            }
            catch (Exception ex)
            {
                while (true)
                {
                    if (ex.InnerException == null) break;

                    ex = ex.InnerException;
                }

                LogFactory.Error($"上传简历异常！异常信息：{ex.Message}");

                uploadOssActionBlock.Post(tempData);

            }
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 });

        #region 非协议版

        private static void UploadResumeToOss(string path)
        {
            while (true)
            {
                try
                {
                    if(!File.Exists(path)) break;

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

        public List<SearchResumeModel> GetOldResumes()
        {
            var searchResumeList = new List<SearchResumeModel>();

            using (var db = new ResumeMatchDBEntities())
            {
                var resumeList = db.OldResumeSummary.Where(w => w.ResumeId.Length == 11 && !w.IsMatched && (w.Status == 0 || w.Status == 99)).Take(500).ToList();

                foreach (var resume in resumeList)
                {
                    resume.Status = 2;
                }

                db.SaveChanges();

                foreach (var resume in resumeList)
                {
                    var searchResume = new SearchResumeModel();

                    var filePath = $@"\\DOLPHIN-PC\Data\智联招聘\{resume.Template}\{resume.ResumeId}.{Path.GetFileNameWithoutExtension(resume.Template)}";

                    if (!File.Exists(filePath))
                    {
                        LogFactory.Warn($"指定路径不存在！ResumeNumber=>{resume.ResumeId} Path=>{filePath}");

                        resume.Status = 1;

                        resume.MatchTime = DateTime.Now;

                        db.SaveChanges();

                        continue;
                    }

                    var sourceCode = File.ReadAllText(filePath);

                    var genderMatch = Regex.Match(sourceCode, "(男|女)");

                    if (genderMatch.Success) searchResume.Gender = genderMatch.Value;

                    var replaceMatch = Regex.Match(sourceCode, "应聘机构：.+?>(.+?)</strong>");

                    if (replaceMatch.Success) sourceCode = sourceCode.Replace(replaceMatch.Result("$1"), "");

                    var matchs = Regex.Matches(sourceCode, "([\u4e00-\u9fa5]{4,11}有限公司[\u4e00-\u9fa5]{0,6})");

                    if (matchs.Count == 0)
                    {
                        Console.WriteLine($"该简历未匹配到公司！ResumeNumber=> {resume.ResumeId}");

                        resume.Status = 1;

                        resume.MatchTime = DateTime.Now;

                        db.SaveChanges();

                        continue;
                    }

                    var companys = new List<string>();

                    for (var i = 0; i < matchs.Count; i++)
                    {
                        if (i == 2) break;

                        companys.Add(matchs[i].Result("$1"));
                    }

                    searchResume.Companys = companys;

                    searchResume.Cellphone = resume.Cellphone;

                    searchResume.Email = resume.Email;

                    searchResume.SearchResumeId = resume.ResumeId;

                    searchResumeList.Add(searchResume);
                }
            }

            return searchResumeList;
        }

        public void ChangeResumeStatus(string resumeId, bool isMatchd)
        {
            using (var db = new ResumeMatchDBEntities())
            {
                var resume = db.OldResumeSummary.FirstOrDefault(f => f.ResumeId == resumeId);

                if (resume == null) return;

                resume.Status = 1;

                resume.MatchTime = DateTime.Now;

                resume.IsMatched = isMatchd;

                db.SaveChanges();
            }
        }

        public void SaveMatchedCache(ResumeMatchResult model)
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

                if (resumeEntity == null)
                {
                    xdb.ZhaopinResume.Add(new ZhaopinResume
                    {
                        Id = model.ResumeId,
                        RandomNumber = model.ResumeNumber,
                        UserId = model.UserId,
                        RefreshTime = model.ModifyTime,
                        UpdateTime = DateTime.UtcNow,
                        UserExtId = model.UserExtId,
                        Source = "Watch"
                    });
                }
                else
                {
                    resumeEntity.Id = model.ResumeId;
                    resumeEntity.RandomNumber = model.ResumeNumber;
                    resumeEntity.UserId = model.UserId;
                    resumeEntity.RefreshTime = model.ModifyTime;
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

        public ZhaopinMatchedCache GetResumeOfCache(string resumeNumber)
        {
            using (var db = new MangningXssDBEntities())
            {
                return db.ZhaopinMatchedCache.FirstOrDefault(f => f.ResumeNumber == resumeNumber);
            }
        }

        public void MatchResume()
        {
            const string path = @"D:\待清理数据\2017-11-24  智联招聘简历导出";

            var filesPath = Directory.EnumerateFileSystemEntries(path);

            var dictionary = new ConcurrentDictionary<string,string>();

            var index = 0;

            var queue = new ConcurrentQueue<string>();

            foreach (var filePath in filesPath)
            {
                queue.Enqueue(filePath);
            }

            for (var i = 0; i < 8; i++)
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        string filePath;

                        if (!queue.TryDequeue(out filePath)) continue;

                        Interlocked.Increment(ref index);

                        var htmlSource = File.ReadAllText(filePath);

                        var numberMatchResult = Regex.Match(htmlSource, "tips-id\">ID:(\\S{10})");

                        if (!numberMatchResult.Success)
                        {
                            LogFactory.Warn("简历编号匹配失败！Path：" + filePath);

                            continue;
                        }

                        var mobileMatchResult = Regex.Match(htmlSource, "main-title-fr\">(Mobile|手机) ：.*?(\\d{11})");

                        if (!mobileMatchResult.Success)
                        {
                            LogFactory.Warn("手机号码匹配失败！Path：" + filePath);

                            continue;
                        }

                        var resumeNumber = numberMatchResult.Result("$1").Trim().Substring(0, 10);

                        var mobile = mobileMatchResult.Result("$2");

                        using (var db = new MangningXssDBEntities())
                        {
                            if (db.ZhaopinResume.Any(a => a.RandomNumber.StartsWith(resumeNumber)))
                            {
                                dictionary.TryAdd(resumeNumber, mobile);

                                continue;
                            }

                            if (db.ZhaopinUser.Any(a => a.Cellphone == mobile))
                            {
                                dictionary.TryAdd(resumeNumber, mobile);

                                continue;
                            }
                        }

                        using (var db = new BadoucaiAliyunDBEntities())
                        {
                            if (db.CoreResumeReferenceMapping.Any(a => a.Value.StartsWith(resumeNumber)))
                            {
                                dictionary.TryAdd(resumeNumber, mobile);

                                continue;
                            }

                            if (db.CoreResumeSummary.Any(a => a.Cellphone.ToString() == mobile))
                            {
                                dictionary.TryAdd(resumeNumber, mobile);
                            }
                        }
                    }
                });
            }

            SpinWait.SpinUntil(() => false);
        }

        #endregion
    }
}