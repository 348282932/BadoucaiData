using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aliyun.OSS;
using Badoucai.EntityFramework.MySql;
using Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB;
using Newtonsoft.Json;
using Exception = System.Exception;

namespace Badoucai.Service
{
    public class HandleBDCOssResumeThread : BaseThread
    {
        private static readonly ConcurrentQueue<string> fileQueue = new ConcurrentQueue<string>();

        private static readonly string domFilePath = ConfigurationManager.AppSettings["File.HandleDomPath"];

        private static readonly string jsonFilePath = ConfigurationManager.AppSettings["File.ImportPath"];

        public override Thread Create()
        {
            return new Thread(() =>
            {
                try
                {
                    var total = 0;

                    Task.Run(() => ListObject(badoucaiOssClient, badoucaiBucketName));

                    for (var i = 0; i < 1; i++)
                    {
                        Task.Run(() =>
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

                                        var streamContent = badoucaiOssClient.GetObject(badoucaiBucketName, path).Content;

                                        while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
                                        {
                                            stream.Write(bytes, 0, len);
                                        }

                                        int.TryParse(Path.GetFileNameWithoutExtension(path), out resumeId);

                                        FlagResume(Encoding.UTF8.GetString(GZip.Decompress(stream.ToArray())), resumeId);
                                    }

                                    stopwatch.Stop();

                                    var elapsed = stopwatch.ElapsedMilliseconds;

                                    Interlocked.Increment(ref total);

                                    Console.WriteLine($"{DateTime.Now} > ResumeID = {resumeId}, Elapsed = {elapsed} ms, Total = {total}.");
                                }
                                catch (InvalidDataException ex)
                                {
                                    badoucaiOssClient.DeleteObject(badoucaiBucketName, $"Zhaopin/{resumeId}");

                                    Trace.TraceError($"流异常 ResumeId = {resumeId}, 异常 = {ex.Message}.");
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError($"异常 ResumeId = {resumeId}, 异常 = {ex}.");
                                }
                            }
                        });
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
        /// 获取Oss简历
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        private static void ListObject(IOss client, string bucketName)
        {
            while (true)
            {
                try
                {
                    ObjectListing result;

                    var nextMarker = "";//File.ReadAllText("NextMarker.txt");

                    do
                    {
                        var listObjectsRequest = new ListObjectsRequest(bucketName)
                        {
                            Prefix = "Zhaopin/",
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

                        //File.WriteAllText("NextMarker.txt", nextMarker);

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
        /// 标记简历
        /// </summary>
        /// <param name="jsonContent"></param>
        /// <param name="resumeId"></param>
        private static void FlagResume(string jsonContent, int resumeId)
        {
            using (var db = new MangningXssDBEntities())
            {
                var resume = db.ZhaopinResume.FirstOrDefault(f => f.Id == resumeId);

                if (resume?.Flag == 0xF)
                {
                    badoucaiOssClient.DeleteObject(badoucaiBucketName, $"Zhaopin/{resumeId}");

                    resume.IncludeTime = DateTime.UtcNow;

                    db.SaveChanges();

                    return;
                }

                var resumeIdStr = resumeId.ToString();

                if (jsonContent.Contains("detialJSonStr"))
                {
                    var jsonObj = JsonConvert.DeserializeObject<dynamic>(jsonContent);

                    //var userId = (int)jsonObj.userDetials.userMasterId;

                    if (string.IsNullOrWhiteSpace((string)jsonObj.userDetials.mobilePhone))
                    {
                        var user = resume != null ? db.ZhaopinUser.FirstOrDefault(f => f.Id == resume.UserId && !string.IsNullOrEmpty(f.Cellphone)) : null;

                        if (user == null)
                        {
                            using (var adb = new BadoucaiAliyunDBEntities())
                            {
                                var reference = adb.CoreResumeReference.AsNoTracking().FirstOrDefault(f => f.Id == resumeIdStr);

                                CoreResumeSummary summary = null;

                                if (reference != null) summary = adb.CoreResumeSummary.AsNoTracking().FirstOrDefault(f => f.Id == reference.ResumeId);

                                if (summary != null)
                                {
                                    jsonObj.userDetials.mobilePhone = summary.Cellphone;

                                    jsonObj.userDetials.email = summary.Email;
                                }
                            }
                        }
                        else
                        {
                            jsonObj.userDetials.mobilePhone = user.Cellphone;

                            jsonObj.userDetials.email = user.Email;
                        }
                    }

                    File.WriteAllText($"{jsonFilePath}{resumeId}", jsonContent);

                    #region 被注释的代码

                    //var flag = string.IsNullOrEmpty((string)jsonObj.userDetials.mobilePhone) ? (short)0x0 : (short)0xF;

                    //dynamic detialJSonStr;

                    //try
                    //{
                    //    detialJSonStr = jsonObj.detialJSonStr;

                    //    if (!string.IsNullOrEmpty((string)jsonObj.detialJSonStr.DateModified))
                    //    {
                    //        jsonObj.detialJSonStr = JsonConvert.SerializeObject(jsonObj.detialJSonStr);
                    //    }
                    //}
                    //catch (Exception)
                    //{
                    //    detialJSonStr = JsonConvert.DeserializeObject<dynamic>((string)jsonObj.detialJSonStr);
                    //}

                    //if (resume == null)
                    //{
                    //    db.ZhaopinResume.Add(new ZhaopinResume
                    //    {
                    //        Id = resumeId,
                    //        RandomNumber = ((string)jsonObj.resumeNo).Substring(0, 10),
                    //        UserId = userId,
                    //        RefreshTime = BaseFanctory.GetTime((string)detialJSonStr.DateModified).ToUniversalTime(),
                    //        UpdateTime = DateTime.UtcNow,
                    //        UserExtId = (string)detialJSonStr.UserMasterExtId,
                    //        Source = "XSS",
                    //        Flag = flag
                    //    });

                    //    handlerFlag = "Insert";
                    //}
                    //else
                    //{
                    //    resume.RandomNumber = ((string)jsonObj.resumeNo).Substring(0, 10);
                    //    resume.UserId = userId;
                    //    resume.RefreshTime = BaseFanctory.GetTime((string)detialJSonStr.DateModified).ToUniversalTime();
                    //    resume.UpdateTime = DateTime.UtcNow;
                    //    resume.Flag = flag;

                    //    handlerFlag = "Update";
                    //}

                    //db.ZhaopinUser.AddOrUpdate(new ZhaopinUser
                    //{
                    //    Id = userId,
                    //    Cellphone = (string)jsonObj.userDetials.mobilePhone,
                    //    CreateTime = BaseFanctory.GetTime((string)detialJSonStr.DateCreated).ToUniversalTime(),
                    //    Email = (string)jsonObj.userDetials.email,
                    //    ModifyTime = BaseFanctory.GetTime((string)detialJSonStr.DateModified).ToUniversalTime(),
                    //    Name = (string)jsonObj.userDetials.userName,
                    //    Source = "XSS",
                    //    UpdateTime = DateTime.UtcNow
                    //});

                    //var jsonResume = JsonConvert.SerializeObject(jsonObj);

                    //if (flag == 0x0)
                    //{
                    //    var path = $@"F:\ZhaopinOss\Resume\NoInformation\{resumeIdStr.Substring(0, 2)}\{resumeIdStr.Substring(2, 2)}";

                    //    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    //    File.WriteAllText($@"{path}\{resumeIdStr}",jsonResume);
                    //}
                    //else
                    //{
                    //    var path = $@"F:\ZhaopinOss\Resume\HaveInformation\{resumeIdStr.Substring(0, 2)}\{resumeIdStr.Substring(2, 2)}";

                    //    if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                    //    File.WriteAllText($@"{path}\{resumeIdStr}", jsonResume);

                    //    using (var stream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(jsonResume))))
                    //    {
                    //        mangningClient.PutObject(mangningBucketName, $"Zhaopin/Resume/{resumeIdStr}", stream);
                    //    }
                    //}

                    #endregion

                    badoucaiOssClient.DeleteObject(badoucaiBucketName, $"Zhaopin/{resumeId}");
                }
                else
                {
                    if (jsonContent.StartsWith("\"<!DOCTYPE HTML>", StringComparison.OrdinalIgnoreCase) || jsonContent.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
                    {
                        if (jsonContent.StartsWith("\"<!DOCTYPE HTML>", StringComparison.OrdinalIgnoreCase))
                        {
                            jsonContent = jsonContent.Substring(1);

                            jsonContent = jsonContent.Substring(0, jsonContent.Length - 1);

                            jsonContent = Regex.Unescape(jsonContent);
                        }

                        var updateTime = Regex.Match(jsonContent, "(?s)resumeUpdateTime\">(.+?)<.+?userName.+?alt=\"(.+?)\"").ResultOrDefault("$1","");

                        var name = Regex.Match(jsonContent, "(?s)resumeUpdateTime\">(.+?)<.+?userName.+?alt=\"(.+?)\"").ResultOrDefault("$2","").Replace("\\","").Replace("/","");

                        string fileName;

                        if (string.IsNullOrEmpty(updateTime) || string.IsNullOrEmpty(name))
                        {
                            fileName = $"{resumeId}";
                        }
                        else
                        {
                            fileName = $"{name}_{updateTime.Replace("年", "-").Replace("月", "-").Replace("日", "")}.txt";
                        }

                        File.WriteAllText($"{domFilePath}{fileName}", jsonContent);
                    }
                    else
                    {
                        Trace.WriteLine($"{DateTime.Now} > 简历格式异常！异常简历ID = {resumeId}, Content = {jsonContent}");
                    }

                    badoucaiOssClient.DeleteObject(badoucaiBucketName, $"Zhaopin/{resumeId}");
                }
            }
        }
    }
}