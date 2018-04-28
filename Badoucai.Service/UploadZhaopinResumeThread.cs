using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Badoucai.EntityFramework.MySql;
using Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB;
using Badoucai.Library;
using Newtonsoft.Json;
using Badoucai.Business.Zhaopin;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Badoucai.Service
{
    public class UploadZhaopinResumeThread : BaseThread
    {
        private static readonly ConcurrentQueue<string> uploadQueue = new ConcurrentQueue<string>();

        private static readonly ConcurrentQueue<ZhaopinResume> resumeQueue = new ConcurrentQueue<ZhaopinResume>();

        private static int totalUpload;

        private static int successUpload;

        private static readonly string uploadFilePath = ConfigurationManager.AppSettings["File.Path"];

        private static readonly string importFilePath = ConfigurationManager.AppSettings["File.ImportPath"];

        private static readonly string importFailPath = ConfigurationManager.AppSettings["File.ImportFailPath"];

        private static readonly string handleDomFilePath = ConfigurationManager.AppSettings["File.HandleDomPath"];

        private static readonly string handleDomFailPath = ConfigurationManager.AppSettings["File.HandleDomFailPath"];

        private static readonly string handleDomSuccessPath = ConfigurationManager.AppSettings["File.HandleDomSuccessPath"];

        public override Thread Create()
        {
            return new Thread(() =>
            {
                try
                {
                    ProgramTasksThread.InitDataByXML();

                    for (var i = 0; i < 4; i++)
                    {
                        Task.Run(() => UploadResume());
                    }

                    for (var i = 0; i < 8; i++)
                    {
                        Task.Run(() => PortfolioResume());
                    }

                    Task.Run(() => ImportResume());

                    Task.Run(() => HandleOldDomResume());

                    GetResumes();
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
        /// 组合简历
        /// </summary>
        private static void GetResumes()
        {
            Console.WriteLine($"{DateTime.Now} > Get Resumes Start.");

            while (true)
            {
                if (resumeQueue.Count + uploadQueue.Count > 0)
                {
                    Thread.Sleep(100);

                    continue;
                }

                Thread.Sleep(5000);

                try
                {
                    using (var db = new MangningXssDBEntities())
                    {
                        var resumes = db.ZhaopinResume.AsNoTracking().Where(w => w.Flag == 14 || w.Flag == 12).Take(1000).ToList();

                        foreach (var resume in resumes)
                        {
                            resumeQueue.Enqueue(resume);
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
        /// 上传简历
        /// </summary>
        private static void UploadResume()
        {
            var serializer = new Serialization.Template.Zhaopin.Json.v1.Serializer();

            var stopwatch = new Stopwatch();

            while (true)
            {
                string path;

                if (!uploadQueue.TryDequeue(out path))
                {
                    Thread.Sleep(100);

                    continue;
                }

                Interlocked.Increment(ref totalUpload);

                try
                {
                    stopwatch.Restart();

                    var resumeId = Convert.ToInt32(Path.GetFileNameWithoutExtension(path));

                    var content = File.ReadAllText(path);

                    var jsonObj = JsonConvert.DeserializeObject<dynamic>(content);

                    dynamic formatterResume;

                    try
                    {
                        if (jsonObj.Flag != null && jsonObj.Flag < 0)
                        {
                            formatterResume = Format.Convert_V0(Format.ConvertTo_Dtl_V5(content));
                        }
                        else
                        {
                            var serializationResume = serializer.Deserialize(content);

                            formatterResume = Formatter.Template.Zhaopin.Json.v1.Formatter.Format(serializationResume);
                        }
                    }
                    catch (Exception ex)
                    {
                        var filePath = $"{ConfigurationManager.AppSettings["File.FailPath"]}{Path.GetFileName(path)}";

                        if (File.Exists(filePath)) File.Delete(filePath);

                        File.Move(path, filePath);

                        Trace.TraceError(ex.ToString());

                        using (var db = new MangningXssDBEntities())
                        {
                            var resume = db.ZhaopinResume.FirstOrDefault(f => f.Id == resumeId);

                            if (resume != null) resume.Flag = 0xA;

                            db.SaveChanges();
                        }

                        continue;
                    }

                    string tag;

                    while (true)
                    {
                        var data = JsonConvert.SerializeObject(new
                        {
                            formatterResume.Reference.Id,
                            formatterResume.Reference.Source
                        });

                        var response = PrepareUploadResume(data);

                        if (response.Code.ToString() != "0")
                        {
                            FinishUploadResume(data);

                            Trace.WriteLine($"{DateTime.Now} > PrepareUploadResume failed ! Data = {data}, Response = {JsonConvert.SerializeObject(response)}");

                            continue;
                        }

                        var badoucaiResumeJson = JsonConvert.SerializeObject(formatterResume);

                        response = UploadResume(badoucaiResumeJson);

                        var returnCode = (string)response.Code;

                        using (var db = new MangningXssDBEntities())
                        {
                            tag = response.Reference?.Tag.ToString();

                            db.ZhaopinResumeUploadLog.Add(new ZhaopinResumeUploadLog
                            {
                                ResumeId = resumeId,
                                ReturnCode = returnCode,
                                Tag = tag,
                                UploadTime = DateTime.Now
                            });

                            FinishUploadResume(data);

                            var resume = db.ZhaopinResume.FirstOrDefault(f => f.Id == resumeId);

                            if (resume != null) resume.Flag = 0xF;

                            if (jsonObj.Flag != null && jsonObj.Flag < 0) resume.Flag = 0xB;

                            if (returnCode != "0")
                            {
                                Trace.WriteLine($"{DateTime.Now} > UploadResume failed !ResumeId = {resumeId}, Response = {JsonConvert.SerializeObject(response)}.");

                                tag = "NULL";

                                db.SaveChanges();

                                var filePath = $"{ConfigurationManager.AppSettings["File.UploadFailPath"]}{Path.GetFileName(path)}";

                                if (File.Exists(filePath)) File.Delete(filePath);

                                File.Move(path, filePath);

                                break;
                            }

                            if (tag == "C" || tag == "U" || tag == "N")
                            {
                                using (var stream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(badoucaiResumeJson))))
                                {
                                    var id = response.Reference.ResumeId.ToString();

                                    badoucaiOssClient.PutObject(badoucaiBucketName, $"Badoucai/{id}", stream); 
                                }

                                ProgramTasksThread.HandleResume(content, resumeId); // 更新 Resume 表的信息
                            }

                            File.Delete(path);

                            db.SaveChanges();

                            Interlocked.Increment(ref successUpload);
                        }

                        break;
                    }

                    stopwatch.Stop();

                    Console.WriteLine($"{DateTime.Now} > ResumeId = {resumeId}, Tag = {tag}, {successUpload}/{totalUpload} = {Math.Round(successUpload / (double)totalUpload, 3) * 100}%, Elapsed = {stopwatch.ElapsedMilliseconds} ms.");
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }
        
        /// <summary>
        /// 组合简历
        /// </summary>
        private static void PortfolioResume()
        {
            while (true)
            {
                try
                {
                    ZhaopinResume resume;

                    var stopwatch = new Stopwatch();

                    stopwatch.Restart();

                    if (!resumeQueue.TryDequeue(out resume))
                    {
                        Thread.Sleep(100);

                        continue;
                    }

                    var filePath = $"{uploadFilePath}{resume.Id}.json";

                    if (File.Exists(filePath))
                    {
                        uploadQueue.Enqueue(filePath);

                        continue;
                    }

                    string userId;

                    string cellphone;

                    string email;

                    using (var db = new MangningXssDBEntities())
                    {
                        var user = db.ZhaopinUser.AsNoTracking().FirstOrDefault(f => f.Id == resume.UserId);

                        if (user == null) continue;

                        userId = user.Id.ToString();

                        cellphone = user.Cellphone;

                        email = user.Email;

                        using (var stream = new MemoryStream())
                        {
                            if (mangningOssClient.DoesObjectExist(mangningBucketName, $"Zhaopin/Resume/{resume.Id}"))
                            {
                                var bytes = new byte[1024];

                                int len;

                                var streamContent = mangningOssClient.GetObject(mangningBucketName, $"Zhaopin/Resume/{resume.Id}").Content;

                                while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
                                {
                                    stream.Write(bytes, 0, len);
                                }

                                var resumeContent = Encoding.UTF8.GetString(GZip.Decompress(stream.ToArray()));

                                var resumeObj = JsonConvert.DeserializeObject<dynamic>(resumeContent);

                                var resumeDetail = JsonConvert.DeserializeObject(resumeObj.detialJSonStr.ToString());

                                resumeDetail.DateModified = user.ModifyTime.ToLocalTime();

                                resumeDetail.DateCreated = user.CreateTime?.ToLocalTime() ?? resumeDetail.DateCreated;

                                resumeDetail.DateLastReleased = resume.RefreshTime.Value.ToLocalTime();

                                resumeDetail.DateLastViewed = resume.RefreshTime.Value.ToLocalTime();

                                resumeObj.detialJSonStr = resumeDetail;

                                resumeContent = JsonConvert.SerializeObject(resumeObj);

                                File.WriteAllText(filePath, resumeContent);

                                uploadQueue.Enqueue(filePath);

                                continue;
                            }

                            if (mangningOssClient.DoesObjectExist(mangningBucketName, $"WatchResume/{resume.Id}"))
                            {
                                var bytes = new byte[1024];

                                int len;

                                var streamContent = mangningOssClient.GetObject(mangningBucketName, $"WatchResume/{resume.Id}").Content;

                                while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
                                {
                                    stream.Write(bytes, 0, len);
                                }

                                var resumeContent = Encoding.UTF8.GetString(GZip.Decompress(stream.ToArray()));

                                var resumeObj = JsonConvert.DeserializeObject<dynamic>(resumeContent);

                                resumeObj.userDetials.mobilePhone = user.Cellphone;

                                resumeObj.userDetials.email = user.Email;

                                var resumeDetail = JsonConvert.DeserializeObject(resumeObj.detialJSonStr.ToString());

                                resumeDetail.DateModified = user.ModifyTime.ToLocalTime();

                                resumeDetail.DateCreated = user.CreateTime.Value.ToLocalTime();

                                resumeDetail.DateLastReleased = resume.RefreshTime.Value.ToLocalTime();

                                resumeDetail.DateLastViewed = resume.RefreshTime.Value.ToLocalTime();

                                resumeObj.detialJSonStr = resumeDetail;

                                resumeContent = JsonConvert.SerializeObject(resumeObj);

                                using (var jsonStream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(resumeContent))))
                                {
                                    mangningOssClient.PutObject(mangningBucketName, $"Zhaopin/Resume/{resume.Id}", jsonStream);
                                }

                                File.WriteAllText(filePath, resumeContent);

                                uploadQueue.Enqueue(filePath);

                                continue;
                            }

                            var zhaopinResume = db.ZhaopinResume.FirstOrDefault(f => f.Id == resume.Id);

                            if (zhaopinResume != null) zhaopinResume.Flag = 0x2;
                        }

                        db.SaveChanges();

                        stopwatch.Stop();
                    }

                    using (var bdcDb = new BadoucaiAliyunDBEntities())
                    {
                        bdcDb.CoreResumeZhaopin.Add(new CoreResumeZhaopin
                        {
                            Cellphone = cellphone,
                            Email = email,
                            ResumeKey = userId,
                            Type = "ResumeUserId",
                            IsMatched = false
                        });

                        bdcDb.SaveChanges();
                    }

                    Console.WriteLine($"{DateTime.Now} > 简历未找到 Josn 源！ResumeId = {resume.Id}, UserId = {userId}, Elapsed = {stopwatch.ElapsedMilliseconds} ms.");
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        /// <summary>
        /// 导入外部Josn
        /// </summary>
        private static void ImportResume()
        {
            var importCount = 0;

            while (true)
            {
                try
                {
                    var filePaths = Directory.GetFiles(importFilePath);

                    if (filePaths.Length == 0)
                    {
                        Thread.Sleep(10 * 1000);

                        continue;
                    }

                    Console.WriteLine($"{DateTime.Now} > {filePaths.Length} Need Import ! ");

                    foreach (var filePath in filePaths)
                    {
                        try
                        {
                            var jsonObj = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(filePath));

                            var resumeData = jsonObj.data == null ? jsonObj : jsonObj.data;

                            var resumeDetail = JsonConvert.DeserializeObject(resumeData.detialJSonStr.ToString());

                            var resumeId = resumeData.resumeId != null ? (int)resumeData.resumeId : resumeDetail.ResumeId != null ? (int)resumeDetail.ResumeId : 0;

                            var cellphone = resumeData.userDetials.mobilePhone.ToString();

                            if (string.IsNullOrEmpty(cellphone))
                            {
                                var path = importFailPath + Path.GetFileName(filePath);

                                if (File.Exists(path))
                                {
                                    File.Delete(path);
                                }

                                File.Move(filePath, path);

                                continue;
                            }

                            var resumeNumber = ((string)resumeData.resumeNo).Substring(0, 10);

                            var userId = (int)resumeData.userDetials.userMasterId;

                            var refreshTime = BaseFanctory.GetTime((string)resumeDetail.DateLastReleased).ToUniversalTime();

                            using (var db = new MangningXssDBEntities())
                            {
                                var resume = db.ZhaopinResume.FirstOrDefault(f => f.Id == resumeId);

                                var isNeedUpload = true;

                                var sourceFlag = -1;

                                var isUpload = false;

                                if (resume != null) sourceFlag = resume.Flag;

                                if (!(resume?.RefreshTime != null && resume.RefreshTime.Value.Date >= refreshTime.Date) || resume?.Flag < 8)
                                {
                                    if (resume != null)
                                    {
                                        resume.RandomNumber = resumeNumber;
                                        resume.RefreshTime = refreshTime;
                                        resume.UpdateTime = DateTime.UtcNow;
                                        if (string.IsNullOrEmpty(resume.UserExtId)) resume.UserExtId = resumeDetail.UserMasterExtId?.ToString();
                                        if (resumeData.Flag != null && (int)resumeData.Flag < 0)
                                        {
                                            if (resume.Flag >= 8 || mangningOssClient.DoesObjectExist(mangningBucketName, $"Zhaopin/Resume/{resume.Id}") || mangningOssClient.DoesObjectExist(mangningBucketName, $"WatchResume/{resume.Id}"))
                                            {
                                                isNeedUpload = false;
                                            }
                                        }

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
                                            Source = "Import",
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
                                            user.CreateTime = resumeData.Flag != null && (int)resumeData.Flag < 0 ? user.CreateTime : BaseFanctory.GetTime((string)resumeDetail.DateCreated).ToUniversalTime();
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
                                            Source = "Import",
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

                                    if (isNeedUpload)
                                    {
                                        isUpload = true;

                                        var resumeContent = JsonConvert.SerializeObject(resumeData);

                                        using (var jsonStream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(resumeContent))))
                                        {
                                            mangningOssClient.PutObject(mangningBucketName, $"Zhaopin/Resume/{resume.Id}", jsonStream);
                                        }
                                    }

                                    db.SaveChanges();
                                }

                                File.Delete(filePath);

                                Console.WriteLine($"{DateTime.Now} > Improt success ! ResumeId = {resumeId}, SourceFlag = {sourceFlag}, IsUpload = {isUpload}, Count = {++importCount}.");
                            }
                        }
                        catch (Exception ex)
                        {
                            var path = importFailPath + Path.GetFileName(filePath);

                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }

                            File.Move(filePath, path);

                            Trace.WriteLine($"{DateTime.Now} > Import Error Message = {ex.Message}, Path = {path}.");
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
        /// 处理旧库投递的Dom简历
        /// </summary>
        public static void HandleOldDomResume()
        {
            var importCount = 0;

            while (true)
            {
                try
                {
                    var filePaths = Directory.GetFiles(handleDomFilePath);

                    if (filePaths.Length == 0)
                    {
                        Thread.Sleep(10 * 1000);

                        continue;
                    }

                    Console.WriteLine($"{DateTime.Now} > {filePaths.Length} Old Resume Dom Need Handle ! ");

                    foreach (var filePath in filePaths)
                    {
                        try
                        {
                            var htmlDocument = new HtmlDocument();

                            htmlDocument.LoadHtml(File.ReadAllText(filePath));

                            var resumeObj = Format.ConvertToZhaopin(ZhaopinHelper.ConvertTo_Dtl_V0(htmlDocument));

                            var fileName = Path.GetFileNameWithoutExtension(filePath);

                            resumeObj = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(resumeObj));

                            var releasedDateTime = DateTime.Parse(fileName.Substring(fileName.LastIndexOf("_", StringComparison.Ordinal) + 1).Replace("：", ":"));

                            resumeObj.detialJSonStr.DateLastReleased = releasedDateTime;

                            resumeObj.detialJSonStr.DateLastViewed = releasedDateTime;

                            resumeObj.detialJSonStr.DateModified = releasedDateTime;

                            File.WriteAllText($"{importFilePath}{resumeObj.resumeId}.json", JsonConvert.SerializeObject(resumeObj));

                            var path = handleDomSuccessPath + Path.GetFileName(filePath);

                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }

                            File.Move(filePath, path);

                            Console.WriteLine($"{DateTime.Now} > Handel Dom success ! ResumeId = {resumeObj.resumeId}, Import count = {++importCount}.");
                        }
                        catch (Exception ex)
                        {
                            var path = handleDomFailPath + Path.GetFileName(filePath);

                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }

                            File.Move(filePath, path);

                            Trace.WriteLine($"{DateTime.Now} > Handle Dom Error Message = {ex.Message}, Path = {path}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"{DateTime.Now} > Handle Dom Error Message = {ex.Message}.");
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static dynamic PrepareUploadResume(string content)
        {
            return JsonConvert.DeserializeObject<dynamic>(new HttpClient().SendAsync(new HttpRequestMessage
            {
                Headers =
                {
                    { "Accept", "*/*" },
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36" },
                    { "Authorization", $"Advanced {ConfigurationManager.AppSettings["Http.Resume.Token"]}" }
                },
                Method = HttpMethod.Post,
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
                RequestUri = new Uri(ConfigurationManager.AppSettings["Http.Resume.PrepareUploadUrl"])
            }).Result.Content.ReadAsStringAsync().Result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static dynamic UploadResume(string content)
        {
            content = content.Replace("'", "’");

            return JsonConvert.DeserializeObject<dynamic>(new HttpClient().SendAsync(new HttpRequestMessage
            {
                Headers =
                {
                    { "Accept", "*/*" },
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36" },
                    { "Authorization", $"Advanced {ConfigurationManager.AppSettings["Http.Resume.Token"]}" }
                },
                Method = HttpMethod.Post,
                Content = new StringContent(content, Encoding.UTF8, "application/json"),
                RequestUri = new Uri(ConfigurationManager.AppSettings["Http.Resume.UploadUrl"])
            }).Result.Content.ReadAsStringAsync().Result);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static void FinishUploadResume(string content)
        {
            var result = string.Empty;

            try
            {
                result = new HttpClient().SendAsync(new HttpRequestMessage
                {
                    Headers =
                    {
                        { "Accept", "*/*" },
                        { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36" },
                        { "Authorization", $"Advanced {ConfigurationManager.AppSettings["Http.Resume.Token"]}" }
                    },
                    Method = HttpMethod.Post,
                    Content = new StringContent(content, Encoding.UTF8, "application/json"),
                    RequestUri = new Uri(ConfigurationManager.AppSettings["Http.Resume.FinishUploadUrl"])
                }).Result.Content.ReadAsStringAsync().Result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}=>{result}");
            }
        }
    }
}