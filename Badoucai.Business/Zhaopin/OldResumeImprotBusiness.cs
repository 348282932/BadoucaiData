using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Badoucai.EntityFramework.MySql;
using Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB;
using Badoucai.EntityFramework.PostgreSql.ResumeMatch_DB;

namespace Badoucai.Business.Zhaopin
{
    public class OldResumeImprotBusiness
    {
        private static readonly ConcurrentQueue<List<CoreResumeSummary>> resumeQueue = new ConcurrentQueue<List<CoreResumeSummary>>();

        public int count;

        private static void GetOldResumes()
        {
            using (var db = new BadoucaiAliyunDBEntities())
            {
                var pageIndex = 0;

                const int pageSize = 1000;

                while (true)
                {
                    if (resumeQueue.Count > 8)
                    {
                        Thread.Sleep(1000);

                        continue;
                    }

                    try
                    {
                        var resumeList = db.CoreResumeSummary
                            .AsNoTracking()
                            .OrderBy(o => o.Id)
                            .Skip(pageIndex * pageSize)
                            .Take(pageSize)
                            .ToList();

                        if (!resumeList.Any()) break;

                        resumeQueue.Enqueue(resumeList);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    pageIndex++;
                }
            }
        }

        public void Improt()
        {
            Task.Run(() => GetOldResumes());

            var sb = new StringBuilder();

            var tasks = new List<Task>();

            for (var i = 0; i < 8; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    while (true)
                    {
                        List<CoreResumeSummary> resumeList;

                        if (!resumeQueue.TryDequeue(out resumeList))
                        {
                            Thread.Sleep(100);

                            continue;
                        }

                        try
                        {
                            using (var db = new MangningXssDBEntities())
                            {
                                foreach (var resume in resumeList)
                                {
                                    var cellphone = resume.Cellphone.ToString();

                                    var user = db.ZhaopinUser.AsNoTracking().FirstOrDefault(f => f.Cellphone == cellphone);

                                    if (user == null)
                                    {
                                        using (var bdb = new BadoucaiAliyunDBEntities())
                                        {
                                            var reference = bdb.CoreResumeReference.FirstOrDefault(f => f.ResumeId == resume.Id && f.Source == "ZHAOPIN");

                                            if (reference != null) sb.AppendLine(resume.Id);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }

                        Interlocked.Add(ref count, resumeList.Count);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            File.WriteAllText(@"F:\ResumeIdList.txt",sb.ToString());
        }
    }
}