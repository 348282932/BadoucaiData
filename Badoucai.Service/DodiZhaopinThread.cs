using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
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
    public class DodiZhaopinThread : BaseThread
    {
        private static readonly CookieContainer cookieContainer = "PHPSESSID=j0lklef94l9akqabg41n3nqd93; Example_auth=f9d7XYivszgUGkEXygbytRrg8EzZWngyS25FZaKx1OSub%2FhBVliH; Hm_lvt_1360b6fe7fa346ff51189adc58afb874=1507336367,1507510768,1507596684,1507682510; Hm_lpvt_1360b6fe7fa346ff51189adc58afb874=1507705480".Serialize("crm.dodi.cn");

        private static readonly ConcurrentQueue<int> businessIdQueue = new ConcurrentQueue<int>();

        private static readonly ConcurrentQueue<ZhaopinCleaningProcedure> cookieQueue = new ConcurrentQueue<ZhaopinCleaningProcedure>();

        private static readonly string path = ConfigurationManager.AppSettings["File.ImportPath"];

        public override Thread Create()
        {
            return new Thread(() =>
            {
                try
                {
                    Task.Run(() => LoadCookies());

                    Task.Run(() => LoadDodiResume());

                    for (var i = 0; i < 1; i++)
                    {
                        Task.Run(() => HandleDodiResume());
                    }
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
        /// 加载搜索帐号
        /// </summary>
        private static void LoadCookies()
        {
            while (true)
            {
                if (cookieQueue.Count != 0)
                {
                    Thread.Sleep(1000);

                    continue;
                }

                try
                {
                    using (var db = new MangningXssDBEntities())
                    {
                        var list = db.ZhaopinCleaningProcedure.Where(w => w.IsEnable && !w.IsOnline && !string.IsNullOrEmpty(w.Cookie));

                        foreach (var item in list)
                        {
                            cookieQueue.Enqueue(item);
                        }

                        Console.WriteLine($"{DateTime.Now} > Loading account complete ! Account count = {cookieQueue.Count}.");
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 加载多迪商机
        /// </summary>
        private static void LoadDodiResume()
        {
            using (var db = new MangningXssDBEntities())
            {
                var dodiBusinessList = db.DodiBusiness.Where(w => w.Status == 1).ToList();

                foreach (var item in dodiBusinessList)
                {
                    item.Status = 0;
                }

                db.SaveChanges();
            }

            while (true)
            {
                if (businessIdQueue.Count > 128)
                {
                    Thread.Sleep(1000);

                    continue;
                }

                try
                {
                    using (var db = new MangningXssDBEntities())
                    {
                        var dodiBusinessList = db.DodiBusiness.Where(w => w.Status == 0 && w.Sources.Contains("智联")).Take(1024).ToList();

                        foreach (var item in dodiBusinessList)
                        {
                            item.Status = 1; 
                        }

                        db.SaveChanges();

                        foreach (var item in dodiBusinessList)
                        {
                            businessIdQueue.Enqueue(item.Id);
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
        /// 处理多迪简历
        /// </summary>
        private static void HandleDodiResume()
        {
            while (true)
            {
                ZhaopinCleaningProcedure cleaningProcedure;

                if (!cookieQueue.TryDequeue(out cleaningProcedure))
                {
                    Thread.Sleep(100);

                    continue;
                }

                var localCookieContainer = cleaningProcedure.Cookie.Serialize(".zhaopin.com");

                while (true)
                {
                    int businessId;

                    if (!businessIdQueue.TryDequeue(out businessId))
                    {
                        Thread.Sleep(100);

                        continue;
                    }

                    using (var db = new MangningXssDBEntities())
                    {
                        var dodiBusiness = db.DodiBusiness.FirstOrDefault(f => f.Id == businessId);

                        if (dodiBusiness != null)
                        {
                            dodiBusiness.Status = 2;

                            db.SaveChanges();
                        }
                    }

                    try
                    {
                        var response = RequestFactory.QueryRequest($"http://crm.dodi.cn/index.php/Main/khxxy/business_id/{businessId}/source/false_note", cookieContainer: cookieContainer);

                        if (!response.IsSuccess) continue;

                        if (!response.Data.Contains("商 机 ID："))
                        {
                            Trace.WriteLine($"{DateTime.Now} > Business ID is Empty ! BusinessId = {businessId}");

                            continue;
                        }

                        var match = Regex.Match(response.Data, @"resume_email\('(.*?)','(\d+)','(\d+)','(\d+)',(\d+)\)");

                        if (!match.Success)
                        {
                            Trace.WriteLine($"{DateTime.Now} > Details match failed ! BusinessId = {businessId}");

                            continue;
                        }

                        var email = HttpUtility.UrlEncode(match.Result("$1"));

                        var phone = HttpUtility.UrlEncode(match.Result("$2"));

                        var email_id = HttpUtility.UrlEncode(match.Result("$3"));

                        var now_month = HttpUtility.UrlEncode(match.Result("$4"));

                        var school_id = HttpUtility.UrlEncode(match.Result("$5"));

                        response = HttpClientFactory.RequestForString($"http://crm.dodi.cn/index.php/Main/email_body?email={email}&phone={phone}&email_id={email_id}&now_month={now_month}&school_id={school_id}", HttpMethod.Get, null, cookieContainer);

                        if (!response.IsSuccess)
                        {
                            Trace.WriteLine($"{DateTime.Now} > Details request failed ! BusinessId = {businessId}");

                            continue;
                        }

                        var resumeDetail = Regex.Unescape(response.Data);

                        var company = Regex.Match(resumeDetail, "20[01]\\d[.][\\d\\s-.]+(.*?)\\s*（").ResultOrDefault("$1", "");

                        if (string.IsNullOrEmpty(company))
                        {
                            Console.WriteLine($"{DateTime.Now} > Company is Empty ! BusinessId = {businessId}");

                            continue;
                        }

                        var matchs = Regex.Matches(resumeDetail, "20[01]\\d\\.\\d{2}\\s-\\s20[01]\\d\\.\\d{2}\\s+([^&]+?)&");

                        var project = string.Empty;

                        foreach (Match item in matchs)
                        {
                            var matchString = item.Result("$1");

                            if (matchString.Contains("（")) continue;

                            if (matchString.Contains("</br>")) continue;

                            if (matchString.Contains(" ")) continue;

                            project = matchString;

                            break;
                        }

                        if (string.IsNullOrEmpty(project))
                        {
                            Console.WriteLine($"{DateTime.Now} > Project is Empty ! BusinessId = {businessId}");

                            continue;
                        }

                        var url = $"https://ihrsearch.zhaopin.com/Home/ResultForCustom?SF_1_1_1={HttpUtility.UrlEncode(project)}&SF_1_1_25=COMPANY_NAME_ALL:{HttpUtility.UrlEncode(company)}&SF_1_1_27=0&orderBy=DATE_MODIFIED,1&pageSize=60&exclude=1";

                        var requestResult = RequestFactory.QueryRequest(url, cookieContainer: localCookieContainer, referer: url);

                        if (!requestResult.IsSuccess)
                        {
                            Trace.TraceError($"{DateTime.Now} > Search request failed ! BusinessId = {businessId}.");

                            continue;
                        }

                        match = Regex.Match(requestResult.Data, "(?s)<span>(\\d+)</span>份简历.+?rd-resumelist-pageNum\">1/(\\d+)</span>");

                        if (!match.Success)
                        {
                            if (requestResult.Data.Contains("text/javascript\" r='m'"))
                            {
                                Console.WriteLine($"{DateTime.Now} > Cookie Expired ! Account = {cleaningProcedure.Account}.");
                            }
                            else
                            {
                                Trace.TraceWarning($"{DateTime.Now} > Condition search error ! Page content = {requestResult.Data}, Account = {cleaningProcedure.Account}.");
                            }

                            using (var db = new MangningXssDBEntities())
                            {
                                var procedure = db.ZhaopinCleaningProcedure.FirstOrDefault(f => f.Id == cleaningProcedure.Id);

                                if (procedure != null)
                                {
                                    procedure.Cookie = null;

                                    db.SaveChanges();
                                }
                            }

                            businessIdQueue.Enqueue(businessId);

                            break;
                        }

                        var total = Convert.ToInt32(match.Result("$1"));

                        if (total != 1)
                        {
                            Console.WriteLine($"{DateTime.Now} > Search match wrong ! ResultCount = {total}, BusinessId = {businessId}, Company = {company}, Project = {project}.");

                            continue;
                        }

                        match = Regex.Match(requestResult.Data, "(?s)RedirectToRd/([^\r\n]+?)','([^\r\n]+?)','([^\r\n]+?)',this\\);this.+?(\\d{2}-\\d{2}-\\d{2})");

                        HandleResume(match, HttpUtility.HtmlDecode(phone), HttpUtility.HtmlDecode(email), cleaningProcedure, businessId);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError(ex.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// 处理简历
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cellphone"></param>
        /// <param name="email"></param>
        /// <param name="cleaningProcedure"></param>
        /// <param name="businessId"></param>
        private static void HandleResume(Match item, string cellphone, string email, ZhaopinCleaningProcedure cleaningProcedure, int businessId)
        {
            var number = item.Result("$1").Substring(0, 10);

            DateTime updateDateTime;

            if (!DateTime.TryParse(item.Result("$4"), out updateDateTime)) return;

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
                            if (!mangningOssClient.DoesObjectExist(mangningBucketName, $"Zhaopin/Resume/{resume.Id}")) return;

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

                        if (user == null) return;

                        if (!WatchResumeDetail(item, cookieContainer, user.Cellphone, user.Email, businessId))
                        {
                            var procedure = db.ZhaopinCleaningProcedure.FirstOrDefault(f => f.Id == cleaningProcedure.Id);

                            if (procedure != null)
                            {
                                procedure.Cookie = null;

                                db.SaveChanges();
                            }

                            businessIdQueue.Enqueue(businessId);

                        }
                    }
                }
                else
                {
                    var incompleteResume = db.ZhaopinIncompleteResume.FirstOrDefault(f => f.ResumeNumber == number);

                    if (incompleteResume != null) incompleteResume.CompletionTime = DateTime.Now;

                    WatchResumeDetail(item, cookieContainer, cellphone, email, businessId);
                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// 查看简历详情
        /// </summary>
        /// <param name="match"></param>
        /// <param name="localCookieContainer"></param>
        /// <param name="cellphone"></param>
        /// <param name="email"></param>
        /// <param name="businessId"></param>
        /// <returns></returns>
        private static bool WatchResumeDetail(Match match, CookieContainer localCookieContainer, string cellphone, string email, int businessId)
        {
            var number = match.Result("$1").Substring(0, 10);

            var numberParam = match.Result("$1").Substring(0, match.Result("$1").IndexOf("?", StringComparison.Ordinal));

            var requestResult = RequestFactory.QueryRequest($"http://ihr.zhaopin.com/resumesearch/getresumedetial.do?resumeNo={numberParam}&searchresume=1&resumeSource=1&keyword=%E8%AF%B7%E8%BE%93%E5%85%A5%E7%AE%80%E5%8E%86%E5%85%B3%E9%94%AE%E8%AF%8D%EF%BC%8C%E5%A4%9A%E5%85%B3%E9%94%AE%E8%AF%8D%E5%8F%AF%E7%94%A8%E7%A9%BA%E6%A0%BC%E5%88%86%E9%9A%94&t={match.Result("$2")}&k={match.Result("$3")}&v=undefined&version=3&openFrom=1", cookieContainer: localCookieContainer);

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
                    Trace.WriteLine($"{DateTime.Now} > Watching failure ! Message = {(string)jsonObj["message"]}, ResumeNumber = {number}");

                    return false;
                }

                var resumeData = jsonObj.data;

                resumeData.userDetials.mobilePhone = cellphone;

                resumeData.userDetials.email = email;

                var resumeDetail = JsonConvert.DeserializeObject(resumeData.detialJSonStr.ToString());

                var resumeId = resumeData.resumeId != null ? (int)resumeData.resumeId : resumeDetail.ResumeId != null ? (int)resumeDetail.ResumeId : 0;

                using (var db = new MangningXssDBEntities())
                {
                    var business = db.DodiBusiness.FirstOrDefault(f => f.Id == businessId);

                    if (business != null)
                    {
                        business.Status = 3;

                        db.SaveChanges();
                    }
                }

                File.WriteAllText($"{path}{resumeId}.json", JsonConvert.SerializeObject(resumeData));

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