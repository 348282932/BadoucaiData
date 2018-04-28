using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aliyun.OSS;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using Badoucai.EntityFramework.MySql;
using Newtonsoft.Json;

namespace Badoucai.Service
{
    public class FlagOssResumeThread : BaseThread
    {
        private static readonly ConcurrentQueue<string> fileQueue = new ConcurrentQueue<string>();

        public override Thread Create()
        {
            return new Thread(() =>
            {
                try
                {
                    var endpoint = ConfigurationManager.AppSettings["Oss.Mangning.Url"];

                    var keyId = ConfigurationManager.AppSettings["Oss.Mangning.KeyId"];

                    var keySecret = ConfigurationManager.AppSettings["Oss.Mangning.KeySecret"];

                    var bucket = ConfigurationManager.AppSettings["Oss.Mangning.Bucket"];

                    var client = new OssClient(endpoint, keyId, keySecret);

                    var total = 0;

                    var count = 0;

                    Task.Run(() => ListObject(client, bucket));

                    for (var i = 0; i < 16; i++)
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

                                try
                                {
                                    stopwatch.Restart();

                                    int flag;

                                    int resumeId;

                                    using (var stream = new MemoryStream())
                                    {
                                        var bytes = new byte[1024];

                                        int len;

                                        var streamContent = client.GetObject(bucket, path).Content;

                                        while ((len = streamContent.Read(bytes, 0, bytes.Length)) > 0)
                                        {
                                            stream.Write(bytes, 0, len);
                                        }

                                        int.TryParse(Path.GetFileNameWithoutExtension(path), out resumeId);

                                        flag = FlagResume(Encoding.UTF8.GetString(GZip.Decompress(stream.ToArray())), resumeId, client, bucket);
                                    }

                                    stopwatch.Stop();

                                    var elapsed = stopwatch.ElapsedMilliseconds;

                                    Interlocked.Increment(ref total);

                                    if(flag == 0xF) Interlocked.Increment(ref count);

                                    Console.WriteLine($"{DateTime.Now} > ResumeID = {resumeId}, Flag = {Convert.ToString(flag, 2).PadLeft(4, '0')}, Elapsed = {elapsed} ms, Count/Total = {count}/{total}.");
                                }
                                catch (Exception ex)
                                {
                                    Trace.TraceError(ex.ToString());
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
        }

        /// <summary>
        /// 标记简历
        /// </summary>
        /// <param name="jsonContent"></param>
        /// <param name="resumeId"></param>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        private static short FlagResume(string jsonContent, int resumeId, IOss client, string bucketName)
        {
            using (var db = new MangningXssDBEntities())
            {
                var resume = db.ZhaopinResume.FirstOrDefault(f => f.Id == resumeId);
                
                if(resume == null) return 0x0;

                if (jsonContent.Contains("detialJSonStr"))
                {
                    var jsonObj = JsonConvert.DeserializeObject<dynamic>(jsonContent);

                    if (!string.IsNullOrWhiteSpace((string)jsonObj.userDetials.mobilePhone))
                    {
                        resume.Flag = 0xF;
                    }
                    else
                    {
                        var user = db.ZhaopinUser.FirstOrDefault(f => f.Id == resume.UserId && !string.IsNullOrEmpty(f.Cellphone));

                        if (user == null)
                        {
                            resume.Flag = 0xD;
                        }
                        else
                        {
                            jsonObj.userDetials.mobilePhone = user.Cellphone;

                            jsonObj.userDetials.email = user.Email;

                            resume.Flag = 0xF;

                            var jsonResume = JsonConvert.SerializeObject(jsonObj);

                            try
                            {
                                using (var stream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(jsonResume))))
                                {
                                    client.PutObject(bucketName, $"Zhaopin/Resume/{resumeId}", stream);
                                }

                                //if (resume.Flag != 0xD) File.WriteAllText($@"F:\ZhaopinOss\Resume\{resumeId}", jsonResume);
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError(ex.ToString());
                            }
                        }
                    }
                }
                else
                {
                    resume.Flag = 0x9;
                }

                db.SaveChanges();

                return resume.Flag;
            }
        }
    }
}
