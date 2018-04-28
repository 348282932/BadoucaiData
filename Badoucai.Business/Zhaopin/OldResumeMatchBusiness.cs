using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Aliyun.OSS;
using Badoucai.Business.Model;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;
using Newtonsoft.Json;
using System.Data.Entity.Migrations;
using Badoucai.EntityFramework.PostgreSql.Crawler_DB;
using Badoucai.EntityFramework.PostgreSql.ResumeMatch_DB;

namespace Badoucai.Business.Zhaopin
{
    public class OldResumeMatchBusiness
    {
        private static OssClient newOss;

        private static readonly string newBucket = ConfigurationManager.AppSettings["Oss.Mangning.Bucket"];

        private static int count;

        private static int index;

        public OldResumeMatchBusiness()
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
        }

        public void Buquan()
        {
            var filePaths = Directory.EnumerateFileSystemEntries(@"Z:\2017-11-24").ToArray();

            count = filePaths.Length;

            foreach (var filePath in filePaths)
            {
                var resumeJson = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(filePath));

                var detail = JsonConvert.DeserializeObject(resumeJson.detialJSonStr.ToString());

                var resumeId = resumeJson.resumeId != null ? (int)resumeJson.resumeId : detail.ResumeId != null ? (int)detail.ResumeId : 0;

                uploadOssActionBlockTemp.Post(new KeyValuePair<string, int>(filePath, resumeId));
            }
        }

        public void MatchOldResumeLibrary()
        {
            //var count = 0;

            var queue = new ConcurrentQueue<IEnumerable<dynamic>>();

            for (var i = 0; i < 8; i++)
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        IEnumerable<dynamic> list;

                        if (!queue.TryDequeue(out list)) continue;

                        var matchedResult = SearchOldLibrary(list);

                        if (matchedResult.Any())
                        {
                            Interlocked.Add(ref count, matchedResult.Count);

                            if (PostResumes(matchedResult))
                            {
                                Console.WriteLine($"已匹配成功 {count} 份简历！待匹配队列剩余：{queue.Count}，待上传OSS队列剩余：{uploadOssActionBlock.InputCount}");
                            }
                        }
                    }
                });
            }

            while (true)
            {
                using (var db = new BadoucaiDBEntities())
                {
                    if (queue.Count < 32)
                    {
                        var list = db.SpiderResumeDownload.Where(w => w.Status != 99 && w.Cellphone == null).Take(1000).ToList();

                        if (!list.Any()) break;

                        foreach (var item in list)
                        {
                            item.Status = 99;
                        }

                        db.TransactionSaveChanges();

                        queue.Enqueue(list.Select(s => new { s.ResumeNumber, s.SavePath }));

                        Console.WriteLine($"已匹配成功 {count} 份简历！待匹配队列剩余：{queue.Count}，待上传OSS队列剩余：{uploadOssActionBlock.InputCount}");
                    }
                }
            }
        }

        /// <summary>
        /// 搜索旧库
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<ResumeMatchResult> SearchOldLibrary(IEnumerable<dynamic> list)
        {
            var resumeList = new List<ResumeMatchResult>();

            using (var db = new ResumeMatchDBEntities())
            {
                foreach (var item in list)
                {
                    var resumeNumber = ((string)item.ResumeNumber).Substring(0, 10);

                    var resume = db.OldResumeSummary.FirstOrDefault(w => w.ResumeId == resumeNumber);

                    if (resume == null) continue;
                    
                    resume.MatchTime = DateTime.Now;

                    resume.IsMatched = true;

                    resumeList.Add(new ResumeMatchResult
                    {
                        Cellphone = resume.Cellphone,
                        Email = resume.Email,
                        ResumeNumber = item.ResumeNumber,
                        Status = 2
                    });

                    using (var xdb = new MangningXssDBEntities())
                    {
                        var filePath = "X:" + ((string)item.SavePath).Replace("/", "\\");

                        if (!File.Exists(filePath))
                        {
                            LogFactory.Warn($"找不到文件路径：{filePath}, ResumeNumber：{resumeNumber}");

                            continue;
                        }

                        var resumeJson = JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(filePath));

                        var detail = JsonConvert.DeserializeObject(resumeJson.detialJSonStr.ToString());

                        var resumeId = resumeJson.resumeId != null ? (int)resumeJson.resumeId : detail.ResumeId != null ? (int)detail.ResumeId : 0;

                        if (resumeId == 0)
                        {
                            LogFactory.Warn($"解析异常！ResumeId 为空, ResumeNumber：{resumeNumber}");

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
                            xdb.ZhaopinUser.AddOrUpdate(a => a.Id, new ZhaopinUser
                            {
                                Id = userId,
                                Source = "XSS",
                                ModifyTime = BaseFanctory.GetTime((string)detail.DateModified).ToUniversalTime(),
                                CreateTime = BaseFanctory.GetTime((string)detail.DateCreated).ToUniversalTime(),
                                Cellphone = resume.Cellphone,
                                Email = resume.Email,
                                Name = resumeJson.userDetials.userName.ToString(),
                                UpdateTime = DateTime.UtcNow,
                                Username = resumeJson.userDetials.email.ToString()
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
                                DeliveryNumber = null,
                                Source = "XSS"
                            });
                        }
                        else
                        {
                            resumeEntity.Id = resumeId;
                            resumeEntity.RandomNumber = resumeNumber;
                            resumeEntity.UserId = userId;
                            resumeEntity.RefreshTime = BaseFanctory.GetTime((string)detail.DateModified).ToUniversalTime();
                            resumeEntity.UpdateTime = DateTime.UtcNow;
                            resumeEntity.UserExtId = detail.UserMasterExtId.ToString();
                            resumeEntity.DeliveryNumber = resumeEntity.DeliveryNumber;
                            resumeEntity.Source = resumeEntity.Source;
                        }
                        
                        xdb.SaveChanges();

                        var path = $"{ConfigurationManager.AppSettings["Resume.SavePath"]}{DateTime.Now:yyyyMMdd}";

                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        var resumePath = $@"{path}\{resumeId}.json";

                        File.WriteAllText(resumePath, JsonConvert.SerializeObject(resumeJson));

                        uploadOssActionBlock.Post(resumePath);
                    }
                }

                db.TransactionSaveChanges();
            }

            return resumeList;
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

                    Console.WriteLine($"上传成功！ path:{path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"上传异常！{ex.Message}, path:{path}");

                uploadOssActionBlock.Post(path);
            }

        }

        private static void UploadResumeToOss(string path, int resumeId)
        {
            try
            {
                using (var stream = new MemoryStream(GZip.Compress(File.ReadAllBytes(path))))
                {
                    newOss.PutObject(newBucket, $"Zhaopin/Resume/{resumeId}", stream);

                    Console.WriteLine($"上传成功！{Interlocked.Increment(ref index)}/{count} path：{path} resumeId：{resumeId}  OSS 队列剩余：{uploadOssActionBlockTemp.InputCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"上传异常！{ex.Message}, path:{path}");

                uploadOssActionBlock.Post(path);
            }

        }

        /// <summary>
        /// 返回匹配结果
        /// </summary>
        /// <param name="list"></param>
        public static bool PostResumes(IReadOnlyCollection<ResumeMatchResult> list)
        {
            var dataResult = new DataResult<string>();

            var deepCopyList = list.Clone<ResumeMatchResult>();

            foreach (var resume in deepCopyList)
            {
                resume.ResumeNumber = resume.ResumeNumber.Substring(0, 10);
            }

            for (var i = 0; i < 3; i++)
            {
                dataResult = RequestFactory.QueryRequest("http://spider.bdc.com:8085/splider/Resume/ModifyContact", JsonConvert.SerializeObject(deepCopyList), RequestEnum.POST, contentType: ContentTypeEnum.Json.Description());

                if (dataResult.IsSuccess && dataResult.Data.Contains("成功"))
                {
                    //if(list.First().Status == 2) LogFactory.Info($"简历匹配结果已成功回传！JSON：{JsonConvert.SerializeObject(deepCopyList)}", MessageSubjectEnum.API);

                    return true;
                }
            }

            LogFactory.Warn($"简历回传 API 异常！响应信息：{dataResult.Data}, ResumeNumbers => {string.Join(",", deepCopyList.Select(s => s.ResumeNumber))}");

            return false;
        }

        /// <summary>
        /// 并发工作流（上传）
        /// </summary>
        public static readonly ActionBlock<string> uploadOssActionBlock = new ActionBlock<string>(path =>
        {
            UploadResumeToOss(path);

        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 });

        public static readonly ActionBlock<KeyValuePair<string,int>> uploadOssActionBlockTemp = new ActionBlock<KeyValuePair<string, int>>(path =>
        {
            UploadResumeToOss(path.Key, path.Value);

        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 });
    }
}