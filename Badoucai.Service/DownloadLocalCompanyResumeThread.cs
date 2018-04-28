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
using Badoucai.Library;
using Badoucai.EntityFramework.MySql;
using Newtonsoft.Json;
using System.Web;

namespace Badoucai.Service
{
    public class DownloadLocalCompanyResumeThread : BaseThread
    {
        private static readonly string uploadFilePath = ConfigurationManager.AppSettings["File.Path"];

        private static readonly ConcurrentQueue<ZhaopinStaff> staffQueue = new ConcurrentQueue<ZhaopinStaff>();

        private static readonly ConcurrentDictionary<int,int> dictionary = new ConcurrentDictionary<int, int>();

        private static int count;

        private static int companyId;

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

                        //var staffs = db.ZhaopinStaff.AsNoTracking().Where(w => (companyArr.Contains(w.CompanyId) || w.Source.Contains("5.5")) && !string.IsNullOrEmpty(w.Cookie)).ToList();

                        var staffIdArray = new[] { 705826281, 683974003, 705834336, 705675698, 700680503, 700537915, 705680163, 698198504 };

                        var staffs = db.ZhaopinStaff.AsNoTracking().Where(w => (staffIdArray.Contains(w.Id) || w.Source.Contains("5.5")) && !string.IsNullOrEmpty(w.Cookie)).ToList();

                        foreach (var staff in staffs)
                        {
                            if (!dictionary.Keys.Contains(staff.Id)) staffQueue.Enqueue(staff);
                        }

                        if (staffs.Count != 0)
                        {
                            Console.WriteLine($"{DateTime.Now} > Get Cookies Success ! Count = {staffs.Count}.");
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

                try
                {
                    if (!dictionary.TryAdd(staff.Id, staff.CompanyId)) continue;

                    var cookieContainer = staff.Cookie.Serialize(".zhaopin.com");

                    var isWhile = true;

                    if (staff.Source.Contains("5.5"))
                    {
                        #region 网聘 5.5

                        while (isWhile)
                        {
                            var param = new { S_ResumeState = "1", S_CreateDate = $"{DateTime.Now.AddDays(-90):yyMMdd},{DateTime.Now:yyMMdd}", S_feedback = "", page = 1, pageSize = 100 };

                            var requestResult = RequestFactory.QueryRequest("https://rdapi.zhaopin.com/rd/resume/list", JsonConvert.SerializeObject(param), RequestEnum.POST, cookieContainer, contentType: ContentTypeEnum.Json.Description());

                            if (!requestResult.IsSuccess)
                            {
                                Trace.TraceWarning(requestResult.ErrorMsg);

                                continue;
                            }

                            var content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                            if ((int)content.code == 4) // 登录过期
                            {
                                using (var db = new MangningXssDBEntities())
                                {
                                    var zhaopinStaff = db.ZhaopinStaff.FirstOrDefault(f => f.Id == staff.Id);

                                    if (zhaopinStaff != null)
                                    {
                                        zhaopinStaff.Cookie = null;

                                        db.SaveChanges();

                                        dictionary.TryRemove(staff.Id, out companyId);
                                    }
                                }

                                Trace.WriteLine($"{DateTime.Now} > Loging Timeout ! Username = {staff.Username}, Message = {content.message}.");

                                break;
                            }

                            if ((int)content.code != 0)
                            {
                                Trace.WriteLine($"{DateTime.Now} > Get Resume List Error ! Username = {staff.Username}, Message = {content.message}.");

                                continue;
                            }

                            var resumes = content.data.dataList;

                            var unhandled = (int)content.data.total;

                            if (resumes.Count == 0) break;

                            HandleResumes(resumes, unhandled, staff, ref isWhile, cookieContainer);
                        }

                        #endregion
                    }
                    else
                    {
                        #region 新系統

                        foreach (var orderFlag in new[] { "deal", "commu", "interview" })

                        {
                            while (isWhile)
                            {
                                var requestResult = GetResumes(cookieContainer, 0, 60, orderFlag);

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

                                            dictionary.TryRemove(staff.Id, out companyId);
                                        }
                                    }

                                    Trace.WriteLine($"{DateTime.Now} > Loging Timeout ! Username = {staff.Username}, Message = {content.message}.");

                                    isWhile = false;

                                    break;
                                }

                                if ((int)content.code != 1)
                                {
                                    Trace.WriteLine($"{DateTime.Now} > Get Resume List Error ! Username = {staff.Username}, Message = {content.message}.");

                                    continue;
                                }

                                var resumes = content.data[orderFlag].results;

                                var unhandled = (int)content.data[orderFlag].numFound;

                                if (resumes.Count == 0) break;

                                HandleResumes(resumes, unhandled, staff, ref isWhile, cookieContainer);
                            }
                        }

                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
                finally
                {
                    dictionary.TryRemove(staff.Id, out companyId);
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
        private static void HandleResumes(dynamic resumes, int unhandled, ZhaopinStaff staff, ref bool isWhile, CookieContainer cookieContainer)
        {
            var stopwatch = new Stopwatch();

            foreach (var item in resumes)
            {
                #region Handle resume

                try
                {
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

                                dictionary.TryRemove(staff.Id, out companyId);
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

                            db.SaveChanges();
                        }
                        else
                        {
                            status = "NoHandle";
                        }
                    }

                    #endregion

                    Thread.Sleep(3000);

                    #region SignResume

                    var data = HttpUtility.UrlEncode(JsonConvert.SerializeObject(new
                    {
                        signTag = "noSuit",
                        resumeList = new List<dynamic>
                                    {
                                        new
                                        {
                                            resumeNo = $"{item.jobNumber}_{item.number}_{item.version}_1",
                                            resumenumber = item.number,
                                            item.version,
                                            lanType = 1,
                                            jobname = ((string)resumeData?.jobName)?.Replace("\"", "\\\"").Replace("\\", "\\\\"),
                                            jobno = item.jobNumber,
                                            resumesource = item.resumeSource,
                                            resumeJobId = item.id,
                                            resumerName = ((string)item?.userName).Replace("\r", "").Replace("\n", "").Replace("\"", "\\\""),
                                            resumejlName = ((string)item?.name).Replace("\r", "").Replace("\n", "").Replace("\"", "\\'").Replace("\\", "\\\\"),
                                            resumerId = item.userId
                                        }
                                    },
                        mark = ""
                    }));

                    requestResult = RequestFactory.QueryRequest("https://ihr.zhaopin.com/resumemanage/resumesignstate.do", $"data={data}", RequestEnum.POST, cookieContainer);

                    if (!requestResult.IsSuccess)
                    {
                        Trace.TraceWarning(requestResult.ErrorMsg);

                        continue;
                    }

                    content = JsonConvert.DeserializeObject<dynamic>(requestResult.Data);

                    if ((int)content.code == 6001) // 登录过期
                    {
                        using (var db = new MangningXssDBEntities())
                        {
                            var zhaopinStaff = db.ZhaopinStaff.FirstOrDefault(f => f.Id == staff.Id);

                            if (zhaopinStaff != null)
                            {
                                zhaopinStaff.Cookie = null;

                                db.SaveChanges();

                                dictionary.TryRemove(staff.Id, out companyId);
                            }
                        }

                        Trace.WriteLine($"{DateTime.Now} > Loging Timeout ! Username = {staff.Username}, Message = {content.message}.");

                        isWhile = false;

                        break;
                    }

                    if ((int)content.code != 1)
                    {
                        Trace.WriteLine($"{DateTime.Now} > Sign Resume Error ! Username = {staff.Username}, Message = {content.message}, ResumeId = {resumeId}.");

                        continue;
                    }

                    #endregion

                    stopwatch.Stop();

                    var elapsed = stopwatch.ElapsedMilliseconds;

                    Interlocked.Increment(ref count);

                    Console.WriteLine($"{DateTime.Now} > {count} {status} {staff.Username}：Unhandled = {--unhandled}, RId = {resumeId}, Elapsed = {elapsed} ms.");
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                #endregion
            }
        }
    }
}