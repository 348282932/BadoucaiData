using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Newtonsoft.Json;
using System.Threading.Tasks.Dataflow;
using Aliyun.OSS;

namespace Badoucai.Business.Zhaopin
{
    public class SpiderResumeBusiness
    {
        private static int downloadCount;

        private static OssClient newOss;

        private static readonly Dictionary<int, DateTime> dictionary = new Dictionary<int, DateTime>();

        private static readonly string newBucket = ConfigurationManager.AppSettings["Oss.Mangning.Bucket"];

        private static readonly ConcurrentDictionary<int,int> resumeDic = new ConcurrentDictionary<int, int>();

        public static readonly ConcurrentQueue<Tuple<CookieContainer, string, string, DateTime, string, long, int, Tuple<string>>> downQueue = new ConcurrentQueue<Tuple<CookieContainer, string, string, DateTime, string, long, int, Tuple<string>>>();

        public SpiderResumeBusiness()
        {
            newOss = new OssClient(
                ConfigurationManager.AppSettings["Oss.Mangning.Url"],
                ConfigurationManager.AppSettings["Oss.Mangning.KeyId"],
                ConfigurationManager.AppSettings["Oss.Mangning.KeySecret"]);

            #region 测试 OSS

            //using (var stream = new MemoryStream())
            //{
            //    var bytes = new byte[1024];

            //    int len;

            //    var streamContent = newOss.GetObject(newBucket, $"Zhaopin/{209306360}").Content;

            //    while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
            //    {
            //        stream.Write(bytes, 0, len);
            //    }

            //    File.WriteAllBytes(@"D:\209306360.json", GZip.Decompress(stream.ToArray()));
            //}

            #endregion

            for (var i = 0; i < 4; i++)
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        Tuple<CookieContainer, string, string, DateTime, string, long, int, Tuple<string>> param;

                        if (!downQueue.TryDequeue(out param))
                        {
                            Thread.Sleep(100);

                            continue;
                        }

                        Down(param.Item1, param.Item2, param.Item3, param.Item4, param.Item5, param.Item6, param.Item7, param.Rest.Item1);
                    }
                });
            }
        }

        /// <summary>
        /// 获取 Cookie 相关信息
        /// </summary>
        /// <returns></returns>
        public Dictionary<int,string> GetCookies()
        {
            dictionary.Clear();

            var cookieList = new Dictionary<int, string>();

            var cookieStaffList = new List<ZhaopinStaff>();

            using (var db = new MangningXssDBEntities())
            {
                while (true)
                {
                    try
                    {
                        var pageIndex = 0;

                        while (true)
                        {
                            //var list = db.ZhaopinStaff.Where(w => !string.IsNullOrEmpty(w.Cookie)).OrderByDescending(o => o.Id).Skip(pageIndex * 500).Take(500).ToList();

                            var companyArr = db.ZhaoPinCompany.Where(w => w.Source.Contains("MANUAL")).Select(s => s.Id).ToArray();

                            var list = db.ZhaopinStaff.Where(w => companyArr.Any(a => a == w.CompanyId) && !string.IsNullOrEmpty(w.Cookie)).OrderByDescending(o => o.Id).Skip(pageIndex * 500).Take(500).ToList();

                            if (!list.Any()) break;

                            cookieStaffList.AddRange(list);

                            pageIndex++;
                        }

                        cookieStaffList = cookieStaffList.Distinct().ToList();

                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"获取 Cookie 异常！{ex.Message} 准备重试...");
                    }
                }
            }

            foreach (var cookie in cookieStaffList)
            {
                if (dictionary.ContainsKey(cookie.Id))
                {
                    if (cookie.UpdateTime?.Subtract(dictionary[cookie.Id]).TotalMinutes < 30) continue;

                    dictionary[cookie.Id] = cookie.UpdateTime ?? DateTime.Now;
                }
                else
                {
                    dictionary.Add(cookie.Id, cookie.UpdateTime ?? DateTime.Now);
                }

                cookieList.Add(cookie.CompanyId, cookie.Cookie);
            }

            Console.WriteLine($"获取 Cookie 完成，共 {cookieStaffList.Count} 个，{cookieList.Count} 个待爬取！");

            return cookieList;
        }

        /// <summary>
        /// 校验简历是否下载过
        /// </summary>
        /// <param name="deliveryId"></param>
        /// <param name="orderFlag"></param>
        /// <returns></returns>
        public bool IsDownloadByDeliveryId(long deliveryId, string orderFlag)
        {
            using (var db = new MangningXssDBEntities())
            {
                var isDownload = db.ZhaopinDeliveryLog.Any(a => a.DeliveryId == deliveryId);

                if(isDownload) Console.WriteLine($"DeliveryId：{deliveryId} OrderFlag：{orderFlag} 已下载过！");

                return isDownload;
            }
        }

        /// <summary>
        /// 获取简历列表
        /// </summary>
        /// <param name="cookieContainer"></param>
        /// <param name="startNum"></param>
        /// <param name="rowsCount"></param>
        /// <param name="orderFlag"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public DataResult<string> GetResumes(CookieContainer cookieContainer, int startNum, int rowsCount, string orderFlag, string token)
        {
            Thread.Sleep(1000);

            var dic = new Dictionary<string, string>
            {
                { "startNum", $"{startNum}" },
                { "rowsCount", $"{rowsCount}" },
                { "orderFlag", orderFlag },
                { "countFlag", "1" },
                { "pageType", "all" },
                { "source", "1;2;5" },
                { "sort", "time" },
                { "onlyLastWork", "false" }
            };

            return RequestFactory.QueryRequest($"http://ihr.zhaopin.com/resumemanage/resumelistbykey.do?access_token={token}", dic.SerializeRequestDic(), RequestEnum.POST, cookieContainer);
        }

        /// <summary>
        /// 获取回收站简历列表
        /// </summary>
        /// <param name="cookieContainer"></param>
        /// <param name="startNum"></param>
        /// <param name="rowsCount"></param>
        /// <param name="orderFlag"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public DataResult<string> GetRecycleResumes(CookieContainer cookieContainer, int startNum, int rowsCount, string orderFlag, string token)
        {
            Thread.Sleep(1000);

            return RequestFactory.QueryRequest($"http://ihr.zhaopin.com/resumemanage/resumelist.do?access_token={token}&startNum={startNum}&rowsCount={rowsCount}&startDate=&endDate=", cookieContainer: cookieContainer);
        }

        /// <summary>
        /// 下载简历
        /// </summary>
        /// <param name="cookieContainer"></param>
        /// <param name="pageIndex"></param>
        /// <param name="orderFlag"></param>
        /// <param name="companyId"></param>
        /// <param name="function"></param>
        /// <param name="token"></param>
        public void DownloadResumes(CookieContainer cookieContainer, int pageIndex, string orderFlag, int companyId, Func<CookieContainer, int, int, string, string, DataResult<string>> function, string token)
        {
            var cacheDictionary = new Dictionary<long, Tuple<string, string, DateTime>>();

            while (true)
            {
                if (pageIndex < 0) break;

                var num = 1;

                if (pageIndex == 0) num = 0;

                var requestResult = function(cookieContainer, pageIndex * 90 - num, 90 + num, orderFlag, token);

                if (!requestResult.IsSuccess) break;

                var content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                if ((int)content.code != 1)
                {
                    LogFactory.Warn($"简历信息下载异常！message：{content.message} companyId：{companyId}");

                    return;
                }

                var count = orderFlag == "recycle" ? (int)content.data.numFound : (int)content.data[orderFlag].numFound;

                if (count == 0) break;

                var pageNumber = (int)Math.Ceiling(count / 90.0);

                var summaries = orderFlag == "recycle" ? content.data.results : content.data[orderFlag].results;

                var size = summaries.Count;

                if (size == 0 || IsDownloadByDeliveryId((long)summaries[0].id, orderFlag))
                {
                    pageIndex -= 1;

                    continue;
                }

                if (!IsDownloadByDeliveryId((long)summaries[size - 1].id, orderFlag) && pageNumber - 1 > pageIndex && size > (pageIndex + 1) * 90)
                {
                    pageIndex += 1;

                    continue;
                }

                cacheDictionary.Add((long)summaries[0].id, new Tuple<string, string, DateTime>((string)summaries[0].jobNumber, (string)summaries[0].number, BaseFanctory.GetTime((string)summaries[0].refreshTime)));

                downQueue.Enqueue(new Tuple<CookieContainer, string, string, DateTime, string, long, int, Tuple<string>>(cookieContainer, (string)summaries[0].jobNumber, (string)summaries[0].number, BaseFanctory.GetTime((string)summaries[0].refreshTime).ToUniversalTime(), orderFlag, (long)summaries[0].id, companyId, new Tuple<string>(token)));

                for (var i = 1; i < size; i++)
                {
                    if (IsDownloadByDeliveryId((long)summaries[i].id, orderFlag)) break;

                    cacheDictionary.Add((long)summaries[i].id, new Tuple<string, string, DateTime>((string)summaries[i].jobNumber, (string)summaries[i].number, BaseFanctory.GetTime((string)summaries[i].refreshTime)));

                    downQueue.Enqueue(new Tuple<CookieContainer, string, string, DateTime, string, long, int, Tuple<string>>(cookieContainer, (string)summaries[i].jobNumber, (string)summaries[i].number, BaseFanctory.GetTime((string)summaries[i].refreshTime).ToUniversalTime(), orderFlag, (long)summaries[i].id, companyId, new Tuple<string>(token)));  
                }

                cacheDictionary = cacheDictionary.Reverse().ToDictionary(k => k.Key, v => v.Value);

                pageIndex -= 1;

                break;
            }

            while (true)
            {
                if(pageIndex < 0) break;

                var num = 1;

                if (pageIndex == 0) num = 0;

                var requestResult = GetResumes(cookieContainer, pageIndex * 90 - num, 90 + num , orderFlag, token);

                if (!requestResult.IsSuccess) break;

                var content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                if ((int)content.code != 1)
                {
                    LogFactory.Warn($"简历信息下载异常！message：{content.message} companyId：{companyId}");

                    return;
                }

                var count = orderFlag == "recycle" ? (int)content.data.numFound : (int)content.data[orderFlag].numFound;

                if (count == 0) break;

                var summaries = orderFlag == "recycle" ? content.data.results : content.data[orderFlag].results;

                var size = summaries.Count;

                if (size == 0 || cacheDictionary.ContainsKey((long)summaries[0].id))
                {
                    pageIndex -= 1;

                    continue;
                }

                if (!cacheDictionary.ContainsKey((long)summaries[size - 1].id) && size > (pageIndex + 1) * 90)
                {
                    pageIndex += 1;

                    continue;
                }

                for (var i = size - 1; i > -1; i--)
                {
                    if (cacheDictionary.ContainsKey((long)summaries[i].id)) continue;

                    if (IsDownloadByDeliveryId((long)summaries[i].id, orderFlag))
                    {
                        cacheDictionary.Add((long)summaries[i].id, new Tuple<string, string, DateTime>((string)summaries[i].jobNumber, (string)summaries[i].number, BaseFanctory.GetTime((string)summaries[i].refreshTime)));

                        continue;
                    }

                    cacheDictionary.Add((long)summaries[i].id, new Tuple<string, string, DateTime>((string)summaries[i].jobNumber, (string)summaries[i].number, BaseFanctory.GetTime((string)summaries[i].refreshTime)));

                    downQueue.Enqueue(new Tuple<CookieContainer, string, string, DateTime, string, long, int, Tuple<string>>(cookieContainer, (string)summaries[i].jobNumber, (string)summaries[i].number, BaseFanctory.GetTime((string)summaries[i].refreshTime).ToUniversalTime(), orderFlag, (long)summaries[i].id, companyId, new Tuple<string>(token)));
                }

                --pageIndex;
            }
        }

        /// <summary>
        /// 并发工作流（下载）
        /// </summary>

        /// <summary>
        /// 并发工作流（上传）
        /// </summary>
        public static readonly ActionBlock<string> uploadOssActionBlock = new ActionBlock<string>(path =>
        {
            UploadResumeToOss(path);

        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 });

        /// <summary>
        /// 下载
        /// </summary>
        /// <param name="cookieContainer"></param>
        /// <param name="jobNumber"></param>
        /// <param name="number"></param>
        /// <param name="refreshTime"></param>
        /// <param name="orderFlag"></param>
        /// <param name="deliveryId"></param>
        /// <param name="companyId"></param>
        /// <param name="token"></param>
        private static void Down(CookieContainer cookieContainer, string jobNumber, string number, DateTime refreshTime, string orderFlag, long deliveryId, int companyId, string token)
        {
            var resumeId = 0;

            var url = orderFlag == "recycle" ? $"http://ihr.zhaopin.com/resumesearch/getresumedetial.do?access_token={token}&resumeJobId={deliveryId}" : $"http://ihr.zhaopin.com/resumesearch/getresumedetial.do?resumeNo={deliveryId}_{jobNumber}_{number}_1_1&resumeSource=3";

            try
            {
                var requestResult = RequestFactory.QueryRequest(url, cookieContainer: cookieContainer);

                if (!requestResult.IsSuccess)
                {
                    LogFactory.Warn($"简历信息下载异常！异常信息：{requestResult.ErrorMsg}，CompanyId：{companyId}，jobNumber：{jobNumber}, ResumeNumber:{number} ");

                    return;
                }

                var resume = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                if ((int)resume.code != 1)
                {
                    LogFactory.Warn($"简历信息下载异常！message：{resume.message}，CompanyId：{companyId}，jobNumber：{jobNumber}, ResumeNumber:{number} ");

                    if ((int)resume.code == 6001)
                    {
                        while (true)
                        {
                            Tuple<CookieContainer, string, string, DateTime, string, long, int, Tuple<string>> param;

                            if (!downQueue.TryDequeue(out param)) break;

                            if(param.Item7 != companyId) downQueue.Enqueue(param);
                        }

                        return; // 登录过期
                    }

                    return;
                }

                Thread.Sleep(1000);

                if (resume.data == null) return;

                var detail = JsonConvert.DeserializeObject(resume.data.detialJSonStr.ToString());

                resume.data.detialJSonStr = detail;

                var path = $"{ConfigurationManager.AppSettings["Resume.SavePath"]}";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                resumeId = (int)resume.data.resumeId;

                using (var db = new MangningXssDBEntities())
                {
                    if (db.ZhaopinDelivery.Any(a => a.Id == deliveryId))
                    {
                        Console.WriteLine($"DeliveryId：{deliveryId} 已下载过！");

                        return;
                    }

                    db.ZhaopinDelivery.Add(new ZhaopinDelivery
                    {
                        CompanyId = companyId,
                        Id = deliveryId,
                        ResumeId = resumeId,
                        JobNumber = jobNumber,
                        ResumeNumber = number
                    });

                    if (!db.ZhaopinDeliveryLog.Any(a => a.DeliveryId == deliveryId))
                    {
                        db.ZhaopinDeliveryLog.Add(new ZhaopinDeliveryLog
                        {
                            DeliveryId = deliveryId,
                            CompanyId = companyId
                        });
                    }

                    db.TransactionSaveChanges();

                    if (db.ZhaopinResume.Any(a => a.Id == resumeId && a.RefreshTime >= refreshTime && a.Flag != 2))
                    {
                        var resumeNumber = number.Substring(0, 10);

                        Console.WriteLine($"简历已下载过！ResumeNumber：{resumeNumber}");

                        return;
                    }
                }

                if(!resumeDic.TryAdd(resumeId, resumeId)) return;

                var resumePath = $@"{path}\{resumeId}.json";

                File.WriteAllText(resumePath, JsonConvert.SerializeObject(resume.data));

                uploadOssActionBlock.Post(resumePath);

                var userId = (int)resume.data.userDetials.userMasterId;

                using (var db = new MangningXssDBEntities())
                {
                    var user = db.ZhaopinUser.FirstOrDefault(f => f.Id == userId);

                    if (user != null)
                    {
                        if (!user.Source.Contains("MANUAL"))
                        {
                            user.Id = userId;
                            user.Source = "XSS";
                            user.ModifyTime = BaseFanctory.GetTime((string)detail.DateModified).ToUniversalTime();
                            user.CreateTime = BaseFanctory.GetTime((string)detail.DateCreated).ToUniversalTime();
                            user.Cellphone = resume.data.userDetials.mobilePhone.ToString();
                            user.Email = resume.data.userDetials.email.ToString();
                            user.Name = resume.data.userDetials.userName.ToString();
                            user.UpdateTime = DateTime.UtcNow;
                            user.Username = resume.data.userDetials.email.ToString();
                        }
                    }
                    else
                    {
                        db.ZhaopinUser.AddOrUpdate(a => a.Id ,new ZhaopinUser
                        {
                            Id = userId,
                            Source = "XSS",
                            ModifyTime = BaseFanctory.GetTime((string)detail.DateModified).ToUniversalTime(),
                            CreateTime = BaseFanctory.GetTime((string)detail.DateCreated).ToUniversalTime(),
                            Cellphone = resume.data.userDetials.mobilePhone.ToString(),
                            Email = resume.data.userDetials.email.ToString(),
                            Name = resume.data.userDetials.userName.ToString(),
                            UpdateTime = DateTime.UtcNow,
                            Username = resume.data.userDetials.email.ToString()
                        });
                    }

                    var resumeEntity = db.ZhaopinResume.FirstOrDefault(f => f.Id == resumeId);

                    var userExdId = Regex.IsMatch(detail.UserMasterExtId.ToString(), @"^J[MRSL]\d{9}$") ? detail.UserMasterExtId.ToString() : string.Empty;

                    if (resumeEntity == null)
                    {
                        db.ZhaopinResume.Add(new ZhaopinResume
                        {
                            Id = resumeId,
                            RandomNumber = number.Substring(0, 10),
                            UserId = userId,
                            RefreshTime = BaseFanctory.GetTime((string)detail.DateModified).ToUniversalTime(),
                            UpdateTime = DateTime.UtcNow,
                            UserExtId = userExdId,
                            DeliveryNumber = null,
                            Source = "XSS,Deliver",
                            Flag = 0xE
                        });
                    }
                    else
                    {
                        resumeEntity.RandomNumber = number.Substring(0, 10);
                        resumeEntity.UserId = userId;
                        resumeEntity.RefreshTime = BaseFanctory.GetTime((string)detail.DateModified).ToUniversalTime();
                        resumeEntity.UpdateTime = DateTime.UtcNow;
                        if(string.IsNullOrEmpty(resumeEntity.UserExtId)) resumeEntity.UserExtId = userExdId;
                        resumeEntity.DeliveryNumber = resumeEntity.DeliveryNumber;
                        resumeEntity.Source = !resumeEntity.Source.Contains("Deliver") ? resumeEntity.Source += ",Deliver" : resumeEntity.Source;
                        resumeEntity.Flag = 0xE;
                        if (resumeEntity.IncludeTime == null) resumeEntity.IncludeTime = DateTime.UtcNow;
                    }

                    db.SaveChanges();
                }

                Interlocked.Increment(ref downloadCount);

                resumeDic.TryRemove(resumeId, out resumeId);

                Console.WriteLine($"下载成功！{downloadCount}/{downQueue.Count} CId:{companyId} RId:{resumeId} DId:{deliveryId} orderFlag:{orderFlag} oss:{uploadOssActionBlock.InputCount}");
            }
            catch (Exception ex)
            {
                while (true)
                {
                    if (ex.InnerException == null) break;

                    ex = ex.InnerException;
                }

                LogFactory.Warn($"下载简历异常！异常信息：{ex.Message} 堆栈：{ex.StackTrace} CompanyId：{companyId}，ResumeId:{resumeId}, orderFlag：{orderFlag}");
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"上传异常！{ex.Message}, path:{path}");

                uploadOssActionBlock.Post(path);
            }

        }
    }
}