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
using System.Threading;
using System.Threading.Tasks;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Badoucai.Service
{
    public class DownloadAllCompanyReusmeThread :BaseThread
    {
        private static readonly string uploadFilePath = ConfigurationManager.AppSettings["File.Path"];

        private static readonly int interval = Convert.ToInt32(ConfigurationManager.AppSettings["Interval"]);

        private static readonly ConcurrentQueue<ZhaopinStaff> staffQueue = new ConcurrentQueue<ZhaopinStaff>();

        private static readonly ConcurrentDictionary<int, bool> dictionary = new ConcurrentDictionary<int, bool>();

        private static int count;

        private static bool isComplete;

        public override Thread Create()
        {
            return new Thread(() =>
            {
                try
                {
                    for (var i = 0; i < 4; i++)
                    {
                        Task.Run(() => DownloadResume());
                    }

                    LoadCookies();
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
        /// 加载账户信息到队列
        /// </summary>
        private static void LoadCookies()
        {
            while (true)
            {
                Thread.Sleep(1000);

                if (!staffQueue.IsEmpty) continue;

                try
                {
                    using (var db = new MangningXssDBEntities())
                    {
                        //var companyArr = db.ZhaoPinCompany.AsNoTracking().Where(w => w.Source.Contains("MANUAL")).Select(s => s.Id).ToArray();

                        //var staffs = db.ZhaopinStaff.AsNoTracking().Where(w => !companyArr.Contains(w.CompanyId) && (!w.Source.Contains("5.5") || string.IsNullOrEmpty(w.Source)) && !string.IsNullOrEmpty(w.Cookie)).ToList();

                        var staffIdArray = new[] { 705826281, 683974003, 705834336, 705675698, 700680503, 700537915, 705680163, 698198504, 707603919, 707195303, 707297691, 706849229 };

                        var staffs = db.ZhaopinStaff.AsNoTracking().Where(w => !staffIdArray.Contains(w.Id) && !string.IsNullOrEmpty(w.Cookie)).ToList();

                        var cookieCount = 0;

                        foreach (var staff in staffs)
                        {
                            if (!dictionary.Keys.Contains(staff.Id))
                            {
                                staffQueue.Enqueue(staff);

                                cookieCount++;
                            }
                        }

                        if (cookieCount != 0)
                        {
                            Console.WriteLine($"{DateTime.Now} > Get Cookies Success ! Count = {cookieCount}.");
                        }
                        else
                        {
                            Thread.Sleep(5000);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 下载简历
        /// </summary>
        private static void DownloadResume()
        {
            while (true)
            {
                ZhaopinStaff staff;

                if (!staffQueue.TryDequeue(out staff))
                {
                    Thread.Sleep(10 * 1000);

                    continue;
                }

                if (!dictionary.TryAdd(staff.Id, false)) continue;

                var cookieContainer = staff.Cookie.Serialize(".zhaopin.com");

                var isWhile = true;

                var requestResult =  RequestFactory.QueryRequest("https://rd5.zhaopin.com", cookieContainer: cookieContainer);

                if (!requestResult.IsSuccess)
                {
                    Trace.TraceWarning(requestResult.ErrorMsg);

                    staffQueue.Enqueue(staff);

                    dictionary.TryRemove(staff.Id, out isComplete);

                    continue;
                }

                var isNewSystem = !Regex.IsMatch(requestResult.Data, "网聘<span class=\"tag-version\">5\\.5</span>");

                if (!isNewSystem)
                {
                    #region 网聘 5.5

                    foreach (var resumeState in new[] { "1", "2", "3", "4" })
                    {
                        var pageIndex = 0;

                        var unhandled = 0;

                        while (isWhile)
                        {
                            var param = new { S_ResumeState = resumeState, S_CreateDate = $"{DateTime.Now.AddDays(-90):yyMMdd},{DateTime.Now:yyMMdd}", S_feedback = "", page = ++pageIndex, pageSize = 100 };

                            requestResult = RequestFactory.QueryRequest("https://rdapi.zhaopin.com/rd/resume/list", JsonConvert.SerializeObject(param), RequestEnum.POST, cookieContainer, contentType: ContentTypeEnum.Json.Description());

                            if (!requestResult.IsSuccess)
                            {
                                Trace.TraceWarning(requestResult.ErrorMsg);

                                --pageIndex;

                                continue;
                            }

                            var content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                            if ((int)content.code == 6001) // 登录过期
                            {
                                using (var db = new MangningXssDBEntities())
                                {
                                    var zhaopinStaff = db.ZhaopinStaff.FirstOrDefault(f => f.Id == staff.Id);

                                    if (zhaopinStaff != null)
                                    {
                                        zhaopinStaff.Cookie = null;

                                        db.SaveChanges();

                                        dictionary.TryRemove(staff.Id, out isComplete);
                                    }
                                }

                                Trace.WriteLine($"{DateTime.Now} > Loging Timeout ! Username = {staff.Username}, Message = {content.message}.");

                                isWhile = false;

                                break;
                            }

                            if ((int)content.code != 0)
                            {
                                dictionary.TryRemove(staff.Id, out isComplete);

                                Trace.WriteLine($"{DateTime.Now} > Get Resume List Error ! Username = {staff.Username}, Message = {content.message}.");

                                isWhile = false;

                                break;
                            }

                            var resumes = content.data.dataList;

                            if (resumes.Count == 0) break;

                            unhandled = unhandled == 0 ? (int)content.data.total : unhandled;

                            HandleResumes(resumes, ref unhandled, staff, ref isWhile, cookieContainer, false);
                        }
                    }

                    #endregion
                }
                else
                {
                    #region 新系統

                    foreach (var orderFlag in new[] { "deal", "commu", "interview", "noSuit" })
                    {
                        var pageIndex = 0;

                        var unhandled = 0;

                        while (isWhile)
                        {
                            var startNum = 60 * pageIndex++;

                            requestResult = GetResumes(cookieContainer, startNum, 60, orderFlag);

                            if (!requestResult.IsSuccess)
                            {
                                Trace.TraceWarning(requestResult.ErrorMsg);

                                --pageIndex;

                                continue;
                            }

                            var content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                            if ((int)content.code == 6001) // 登录过期
                            {
                                using (var db = new MangningXssDBEntities())
                                {
                                    var zhaopinStaff = db.ZhaopinStaff.FirstOrDefault(f => f.Id == staff.Id);

                                    if (zhaopinStaff != null)
                                    {
                                        zhaopinStaff.Cookie = null;

                                        db.SaveChanges();

                                        dictionary.TryRemove(staff.Id, out isComplete);
                                    }
                                }

                                Trace.WriteLine($"{DateTime.Now} > Loging Timeout ! Username = {staff.Username}, Message = {content.message}.");

                                isWhile = false;

                                break;
                            }

                            if ((int)content.code != 1 )
                            {
                                Trace.WriteLine($"{DateTime.Now} > Get Resume List Error ! Username = {staff.Username}, Message = {content.message}.");

                                isWhile = false;

                                break;
                            }

                            var resumes = content.data[orderFlag].results;

                            if (resumes.Count == 0) break;

                            unhandled = unhandled == 0 ? (int)content.data[orderFlag].numFound : unhandled;

                            HandleResumes(resumes, ref unhandled, staff, ref isWhile, cookieContainer, true);
                        }
                    }

                    #endregion
                }
            }
        }

        /// <summary>
        /// 获取简历列表
        /// </summary>
        /// <param name="cookieContainer"></param>
        /// <param name="startNum"></param>
        /// <param name="rowsCount"></param>
        /// <param name="orderFlag"></param>
        /// <returns></returns>
        private static DataResult<string> GetResumes(CookieContainer cookieContainer, int startNum, int rowsCount, string orderFlag)
        {
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

            return RequestFactory.QueryRequest("http://ihr.zhaopin.com/resumemanage/resumelistbykey.do", dic.SerializeRequestDic(), RequestEnum.POST, cookieContainer);
        }

        /// <summary>
        /// 處理簡歷列表
        /// </summary>
        /// <param name="resumes"></param>
        /// <param name="unhandled"></param>
        /// <param name="staff"></param>
        /// <param name="isWhile"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="isNewSystem"></param>
        private static void HandleResumes(dynamic resumes, ref int unhandled, ZhaopinStaff staff, ref bool isWhile, CookieContainer cookieContainer, bool isNewSystem)
        {
            var stopwatch = new Stopwatch();

            var time = 0;

            foreach (var item in resumes)
            {
                #region Handle resume

                try
                {
                    if (IsDownload((long)item.id, staff.CompanyId))
                    {
                        if (++time > 30) break;

                        continue;
                    }

                    time = 0;

                    stopwatch.Restart();

                    #region Save resume and Upload

                    var requestResult = RequestFactory.QueryRequest($"http://ihr.zhaopin.com/resumesearch/getresumedetial.do?resumeNo={item.id}_{item.jobNumber}_{item.number}_1_1&resumeSource=3", cookieContainer: cookieContainer);

                    if (!requestResult.IsSuccess)
                    {
                        Trace.TraceWarning(requestResult.ErrorMsg);

                        continue;
                    }

                    var content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                    if ((int)content.code == 6001) // 登录过期
                    {
                        using (var db = new MangningXssDBEntities())
                        {
                            var zhaopinStaff = db.ZhaopinStaff.FirstOrDefault(f => f.Id == staff.Id);

                            if (zhaopinStaff != null)
                            {
                                zhaopinStaff.Cookie = null;

                                db.SaveChanges();

                                dictionary.TryUpdate(staff.Id, true, true);
                            }
                        }

                        Trace.WriteLine($"{DateTime.Now} > Loging Timeout ! Username = {staff.Username}, Message = {content.message}.");

                        isWhile = false;

                        break;
                    }

                    if ((int)content.code != 1)
                    {
                        Trace.WriteLine($"{DateTime.Now} > Get Resume Detail Error ! Username = {staff.Username}, Message = {content.message}.");

                        continue;
                    }

                    var resumeData = content.data;

                    var resumeDetail = JsonConvert.DeserializeObject(resumeData.detialJSonStr.ToString());

                    var refreshTime = BaseFanctory.GetTime((string)resumeDetail.DateLastReleased).ToUniversalTime();

                    resumeData.detialJSonStr = resumeDetail;

                    var resumeNumber = ((string)resumeData.resumeNo).Substring(0, 10);

                    var userId = (int)resumeData.userDetials.userMasterId;

                    var resumeId = resumeData.resumeId != null ? (int)resumeData.resumeId : resumeDetail.ResumeId != null ? (int)resumeDetail.ResumeId : 0;

                    var status = "Handle";

                    using (var db = new MangningXssDBEntities())
                    {
                        var resume = db.ZhaopinResume.FirstOrDefault(f => f.Id == resumeId);

                        if (!(resume?.RefreshTime != null && resume.RefreshTime.Value.Date >= refreshTime.Date))
                        {
                            if (resume != null)
                            {
                                resume.RandomNumber = resumeNumber;
                                resume.RefreshTime = refreshTime;
                                resume.UpdateTime = DateTime.UtcNow;
                                if (string.IsNullOrEmpty(resume.UserExtId)) resume.UserExtId = resumeDetail.UserMasterExtId.ToString();
                                resume.Source = !resume.Source.Contains("Deliver") ? resume.Source += ",Deliver" : resume.Source;
                                resume.Flag = 0xE;
                            }
                            else
                            {
                                resume = new ZhaopinResume
                                {
                                    Id = resumeId,
                                    RandomNumber = resumeNumber,
                                    UserId = userId,
                                    RefreshTime = refreshTime,
                                    UpdateTime = DateTime.UtcNow,
                                    UserExtId = resumeDetail.UserMasterExtId.ToString(),
                                    DeliveryNumber = null,
                                    Source = "Deliver",
                                    Flag = 0xE
                                };

                                db.ZhaopinResume.Add(resume);
                            }

                            var user = db.ZhaopinUser.FirstOrDefault(f => f.Id == userId);

                            if (user != null)
                            {
                                if (!user.Source.Contains("MANUAL"))
                                {
                                    user.ModifyTime = BaseFanctory.GetTime((string)resumeDetail.DateModified).ToUniversalTime();
                                    user.CreateTime = BaseFanctory.GetTime((string)resumeDetail.DateCreated).ToUniversalTime();
                                    user.Cellphone = resumeData.userDetials.mobilePhone.ToString();
                                    user.Email = resumeData.userDetials.email.ToString();
                                    user.Name = resumeData.userDetials.userName.ToString();
                                    user.UpdateTime = DateTime.UtcNow;
                                    user.Username = resumeData.userDetials.email.ToString();
                                }
                            }
                            else
                            {
                                user = new ZhaopinUser
                                {
                                    Id = userId,
                                    Source = "Deliver",
                                    ModifyTime = BaseFanctory.GetTime((string)resumeDetail.DateModified).ToUniversalTime(),
                                    CreateTime = BaseFanctory.GetTime((string)resumeDetail.DateCreated).ToUniversalTime(),
                                    Cellphone = resumeData.userDetials.mobilePhone.ToString(),
                                    Email = resumeData.userDetials.email.ToString(),
                                    Name = resumeData.userDetials.userName.ToString(),
                                    UpdateTime = DateTime.UtcNow,
                                    Username = resumeData.userDetials.email.ToString()
                                };

                                db.ZhaopinUser.Add(user);
                            }

                            var resumeContent = JsonConvert.SerializeObject(resumeData);

                            using (var jsonStream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(resumeContent))))
                            {
                                mangningOssClient.PutObject(mangningBucketName, $"Zhaopin/Resume/{resumeId}", jsonStream);
                            }

                            var resumePath = $"{uploadFilePath}{resumeId}.json";

                            File.WriteAllText(resumePath, JsonConvert.SerializeObject(resumeData));
                        }
                        else
                        {
                            status = "NoHandle";
                        }

                        db.ZhaopinDeliveryLog.Add(new ZhaopinDeliveryLog
                        {
                            CompanyId = staff.CompanyId,
                            DeliveryId = (long)item.id
                        });

                        db.ZhaopinDelivery.Add(new ZhaopinDelivery
                        {
                            CompanyId = staff.CompanyId,
                            Id = (long)item.id,
                            CreateTime = DateTime.UtcNow,
                            ResumeId = resumeId,
                            ResumeNumber = resumeData.resumeNo.ToString(),
                            JobNumber = resumeData.jobNo.ToString(),
                            JobName = resumeData.jobName.ToString()
                        });

                        db.SaveChanges();
                    }

                    #endregion

                    stopwatch.Stop();

                    var elapsed = stopwatch.ElapsedMilliseconds;

                    Interlocked.Increment(ref count);

                    var version = isNewSystem ? "NewSystem" : "5.5";

                    Console.WriteLine($"{DateTime.Now} > {version} {count} {status} {staff.Username}：Unhandled = {--unhandled}, RId = {resumeId}, Elapsed = {elapsed} ms.");

                    Thread.Sleep(interval);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                #endregion
            }
        }

        /// <summary>
        /// 是否下载过
        /// </summary>
        /// <param name="deliveryId"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        private static bool IsDownload(long deliveryId, int companyId)
        {
            using (var db = new MangningXssDBEntities())
            {
                return db.ZhaopinDeliveryLog.Any(a => a.DeliveryId == deliveryId && a.CompanyId == companyId);
            }
        }
    }
}