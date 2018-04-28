using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Badoucai.EntityFramework.PostgreSql.ResumeMatch_DB;
using Badoucai.Library;

namespace Badoucai.Business.Zhaopin
{
    public class SearchOldResumeBusiness
    {
        public void Search()
        {
            var resumeQueue = new ConcurrentQueue<OldResumeSummary>();

            Task.Run(() =>
            {
                var pageIndex = 0;

                while (true)
                {
                    if (resumeQueue.Count < 100)
                    {
                        try
                        {
                            using (var db = new ResumeMatchDBEntities())
                            {
                                var resumeList = db.OldResumeSummary.OrderBy(o => o.Id).Skip(pageIndex * 5000).Take(5000).ToList();

                                resumeList.ForEach(f =>
                                {
                                    resumeQueue.Enqueue(f);
                                });
                            }

                            pageIndex++;
                        }
                        catch (Exception ex)
                        {
                            while (true)
                            {
                                if(ex.InnerException == null) break;

                                ex = ex.InnerException;
                            }

                            LogFactory.Error(ex.Message);
                        }
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            });

            var queue = new ConcurrentQueue<string>();

            var sjhArr = "130,131,132,155,156,185,186,145,171,1707,1708,1709,166,146,1349,173,133,153,177,180,181,189,149,1700,1701,1702,199".Split(",");

            for (var j = 0; j < 32; j++)
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        OldResumeSummary resume;

                        if (!resumeQueue.TryDequeue(out resume)) continue;

                        var cellphoneStart = resume.Cellphone.Substring(0, 3);

                        if (!sjhArr.Contains(cellphoneStart)) continue;

                        var filePath = $@"E:\智联招聘\{resume.Template}\{resume.ResumeId}.{Path.GetFileNameWithoutExtension(resume.Template)}";

                        if (!File.Exists(filePath))
                        {
                            LogFactory.Warn($"指定路径不存在！ResumeNumber=>{resume.ResumeId} Path=>{filePath}");

                            continue;
                        }

                        var sourceCode = File.ReadAllText(filePath);

                        var genderMatch = Regex.Match(sourceCode, "(男|女)");

                        if(!genderMatch.Success || genderMatch.Value == "男") continue;

                        var addressMatch = Regex.Match(sourceCode, "(广州|北京|上海)");

                        if (addressMatch.Success) continue;

                        queue.Enqueue(resume.Cellphone);
                    }
                });
            }

            var ltsb = new StringBuilder();

            var dxsb = new StringBuilder();

            Task.Run(() =>
            {
                var ltcount = 0;

                var dxcount = 0;

                const string liantong = @"D:\360安全浏览器下载\联通NEW.csv";

                const string dianxin = @"D:\360安全浏览器下载\电信NEW.csv";

                var ltArr = File.ReadAllLines(liantong).ToList();

                var dxArr = File.ReadAllLines(dianxin).ToList();

                var arr = new List<string>();

                arr.AddRange(ltArr.Select(s=>s.Substring(0,11)));

                arr.AddRange(dxArr.Select(s=>s.Substring(0,11)));

                while (true)
                {
                    string cellphone;

                    if (!queue.TryDequeue(out cellphone)) continue;

                    var yysString = string.Empty;

                    var cellphoneStart = cellphone.Substring(0, 3);

                    if ("130,131,132,155,156,185,186,145,176".Split(",").Contains(cellphoneStart))
                    {
                        yysString = "联通";
                    }
                    else if ("133,153,177,180,181,189".Split(",").Contains(cellphoneStart))
                    {
                        yysString = "电信";
                    }

                    if (string.IsNullOrEmpty(yysString) || arr.Contains(cellphone)) continue;

                    if (yysString == "电信")
                    {
                        dxsb.AppendLine(cellphone);
                        
                        if (++dxcount % 10000 == 0)
                        {
                            File.WriteAllText($@"D:\360安全浏览器下载\手机号数据\旧库{yysString}_{dxcount / 10000}数据.txt", dxsb.ToString());

                            dxsb.Clear();
                        }
                    }
                    else
                    {
                        ltsb.AppendLine(cellphone);

                        if (++ltcount % 10000 == 0)
                        {
                            File.WriteAllText($@"D:\360安全浏览器下载\手机号数据\旧库{yysString}_{ltcount / 10000}数据.txt", ltsb.ToString());

                            ltsb.Clear();
                        }
                    }
                }
            });

            SpinWait.SpinUntil(() => false);
        }
    }
}