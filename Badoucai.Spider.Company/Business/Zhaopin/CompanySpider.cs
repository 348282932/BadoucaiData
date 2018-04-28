using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml;
using Badoucai.EntityFramework.MySql;
using Badoucai.Library;

namespace Badoucai.Spider.Company.Business.Zhaopin
{
    /// <summary>
    /// 公司信息采集
    /// </summary>
    public class CompanySpider 
    {
        private static readonly ConcurrentQueue<string> companyPathQueue = new ConcurrentQueue<string>();

        private static readonly ConcurrentDictionary<string,string> companyDictionary = new ConcurrentDictionary<string, string>();

        private static readonly XmlDocument xml = new XmlDocument();

        private static int insertCount;

        private static int updateCount;

        /// <summary>
        /// 爬取公司信息工作块
        /// </summary>
        private static void SpiderWork()
        {
            while (true)
            {
                string companyPath;

                if(!companyPathQueue.TryDequeue(out companyPath)) continue;

                var dataResult = RequestFactory.QueryRequest(companyPath);

                if (!dataResult.IsSuccess)
                {
                    LogFactory.Warn(dataResult.ErrorMsg);

                    continue;
                }

                var company = new ZhaopinCompany
                {
                    Id = Convert.ToInt32(companyPath.Substring(companyPath.IndexOf("/C", StringComparison.Ordinal) + 3, 8)),
                    Name = Regex.IsMatch(dataResult.Data, "(?s)公司简介.+?h1>(.*?)<") ? Regex.Match(dataResult.Data, "(?s)公司简介.+?h1>(.*?)<").Result("$1").Trim() : string.Empty,
                    Number = companyPath.Substring(companyPath.IndexOf("/C", StringComparison.Ordinal) + 1, 11),
                    Address = Regex.IsMatch(dataResult.Data, "comAddress\">(.*?)</")? Regex.Match(dataResult.Data,"comAddress\">(.*?)</").Result("$1") : string.Empty,
                    UpdateTime = DateTime.UtcNow
                };

                try
                {
                    using (var db = new MangningXssDBEntities())
                    {
                        var zhaoPinCompany = db.ZhaoPinCompany.FirstOrDefault(f => f.Id == company.Id);

                        if (zhaoPinCompany != null)
                        {
                            if (zhaoPinCompany.Number != company.Number)
                            {
                                LogFactory.Warn($"公司已存在！但公司编号不一致！ 公司ID：{company.Id} 公司名称：{company.Name} 公司编号：{company.Number} 数据库公司编号：{zhaoPinCompany.Name} 队列剩余：{companyPathQueue.Count}");

                                continue;
                            }

                            //zhaoPinCompany.UpdateTime = DateTime.UtcNow;

                            //db.TransactionSaveChanges();

                            LogFactory.Info($"公司信息更新成功！ 公司ID：{company.Id} 公司名称：{company.Name} 队列剩余：{companyPathQueue.Count} 新增：{insertCount} 更新：{Interlocked.Increment(ref updateCount)}");
                        }
                        else
                        {
                            db.ZhaoPinCompany.Add(company);

                            db.TransactionSaveChanges();

                            LogFactory.Info($"公司信息新增成功！ 公司ID：{company.Id} 公司名称：{company.Name} 队列剩余：{companyPathQueue.Count} 新增：{Interlocked.Increment(ref insertCount)} 更新：{updateCount}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    companyPathQueue.Enqueue(companyPath);

                    LogFactory.Error($"异常信息：{ex.Message} 堆栈信息：{ex.StackTrace}");
                }
            }
        }

        private static readonly ActionBlock<XmlNode> cityActionBlock = new ActionBlock<XmlNode>(i =>
        {
            SpiderByCity(i);
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 3 });

        private static readonly ActionBlock<XmlNode> keywordActionBlock = new ActionBlock<XmlNode>(i =>
        {
            SpiderByKeyword(i);
        }, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 16 });

        /// <summary>
        /// 获取公司及职位信息
        /// </summary>
        private static void GetCompanyUrlAndPositions()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory.Remove(AppDomain.CurrentDomain.BaseDirectory.IndexOf("bin", StringComparison.Ordinal) - 1) + "\\Sources\\SpiderFilter.xml";

            xml.Load(path);

            companyDictionary.Clear();

            var citys = xml.SelectNodes("//cityFilter/city");

            var keywords = xml.SelectNodes("//keywordFilter/keyword");

            if (citys == null) throw new Exception("资源文件加载异常！");

            foreach (XmlNode city in citys)
            {
                cityActionBlock.Post(city);
            }

            if (keywords != null)
            {
                foreach (XmlNode keywordNode in keywords)
                {
                    keywordActionBlock.Post(keywordNode);
                }
            }
        }

        /// <summary>
        /// 按城市抓取
        /// </summary>
        /// <param name="cityNode"></param>
        private static void SpiderByCity(XmlNode cityNode)
        {
            var pageIndex = 1;

            var pageSize = -1;

            var cityName = cityNode.Attributes?["name"].InnerXml;

            do
            {
                var searchResult = RequestFactory.QueryRequest($"http://sou.zhaopin.com/jobs/searchresult.ashx?bj=&sj=&in=&jl={cityName}&p={pageIndex}&isadv=0");

                if (!searchResult.IsSuccess)
                {
                    LogFactory.Warn("搜索公司列表异常！匹配不到页数");

                    continue;
                }

                if (!Regex.IsMatch(searchResult.Data, "<em>(\\d+)</em>个职位"))
                {
                    LogFactory.Warn("搜索公司列表异常！异常消息：" + searchResult.ErrorMsg);

                    continue;
                }

                if (pageSize < 0)
                {
                    pageSize = (int)Math.Ceiling(Convert.ToDecimal(Regex.Match(searchResult.Data, "<em>(\\d+)</em>个职位").Result("$1")) / 60);

                    if (pageSize > 90) pageSize = 90;
                }

                MatchCompanyUrlAndPositions(searchResult.Data);

                LogFactory.Info($"【按城市】正在抓取公司链接！城市：{cityName} 第 {pageIndex} 页 / 共 {pageSize} 页");
            }
            while (++pageIndex <= pageSize);
        }

        /// <summary>
        /// 匹配公司信息及职位
        /// </summary>
        /// <param name="content"></param>
        private static void MatchCompanyUrlAndPositions(string content)
        {
            var positions = (from Match match in Regex.Matches(content, "(?s)<table ce.+?</table")
                where Regex.IsMatch(match.Value, "(?s)(http://jobs\\.zhaopin\\.com/CZ\\S+)\".+?>(.+?)</.+?gsmc\"><a href=\"http://company\\.zhaopin\\.com/CZ(\\d{8}).+?zwyx\">(.+?)</.+?gzdd\">(.+?)</.+?gxsj\"><span>(.+?)</.+?saveOne\\('(.+?)'")
                select Regex.Match(match.Value, "(?s)(http://jobs\\.zhaopin\\.com/CZ\\S+)\".+?>(.+?)</.+?gsmc\"><a href=\"http://company\\.zhaopin\\.com/CZ(\\d{8}).+?zwyx\">(.+?)</.+?gzdd\">(.+?)</.+?gxsj\"><span>(.+?)</.+?saveOne\\('(.+?)'") into matchResult
                select new ZhaopinPosition
                {
                    CompanyId = Convert.ToInt32(matchResult.Result("$3")),
                    Name = matchResult.Result("$2").Replace("<b>",""),
                    Number = matchResult.Result("$7") + "_1",
                    ReleaseTime = matchResult.Result("$6") == "最新" ? DateTime.Now.ToString("yyyy-MM-dd") : matchResult.Result("$6"),
                    Salary = matchResult.Result("$4"),
                    Url = matchResult.Result("$1"),
                    WorkPlace = matchResult.Result("$5")
                }).ToList();

            if (positions.Any())
            {
                try
                {
                    using (var db = new MangningXssDBEntities())
                    {
                        db.ZhaopinPosition.AddOrUpdate(w => w.Number, positions.ToArray());

                        db.TransactionSaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    LogFactory.Error($"异常信息：{ex.Message} 堆栈信息：{ex.StackTrace}");
                }
            }

            foreach (Match match in Regex.Matches(content, "class=\"gsmc\"><a href=\"(http://company\\.zhaopin\\.com/CZ\\d+\\.htm)"))
            {
                var companyName = match.Result("$1");

                if (companyDictionary.ContainsKey(companyName))
                {
                    continue;
                }

                SpinWait.SpinUntil(() => companyPathQueue.Count < 10, -1);

                companyPathQueue.Enqueue(companyName);

                companyDictionary.TryAdd(companyName, companyName);
            }
        }

        /// <summary>
        /// 按关键字抓取
        /// </summary>
        /// <param name="keywordNode"></param>
        private static void SpiderByKeyword(XmlNode keywordNode)
        {
            var city = keywordNode.Attributes?["city"].InnerXml;

            var keyword = keywordNode.Attributes?["name"].InnerXml;

            var cityList = new List<string>();

            if (string.IsNullOrEmpty(city))
            {
                LogFactory.Error("关键字搜索资源文件格式录入异常！找不到 city 节点！");

                return;
            }

            if (city == "*")
            {
                var citys = xml.SelectNodes("//cityFilter/city");

                if (citys == null) throw new Exception("资源文件加载异常！");

                cityList.AddRange(from XmlNode node in citys select node.Attributes?["name"].InnerXml);
            }
            else
            {
                cityList.AddRange(city.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries));
            }

            foreach (var cityName in cityList)
            {
                var pageIndex = 1;

                var pageSize = -1;

                do
                {
                    var searchResult = RequestFactory.QueryRequest($"http://sou.zhaopin.com/jobs/searchresult.ashx?bj=&sj=&in=&jl={cityName}&kw={keyword}&p={pageIndex}&isadv=0");

                    if (!searchResult.IsSuccess)
                    {
                        LogFactory.Warn("搜索公司列表异常！匹配不到页数");

                        continue;
                    }

                    if (!Regex.IsMatch(searchResult.Data, "<em>(\\d+)</em>个职位"))
                    {
                        LogFactory.Warn("搜索公司列表异常！异常消息：" + searchResult.ErrorMsg);

                        continue;
                    }

                    if (pageSize < 0)
                    {
                        pageSize = (int)Math.Ceiling(Convert.ToDecimal(Regex.Match(searchResult.Data, "<em>(\\d+)</em>个职位").Result("$1")) / 60);

                        if (pageSize > 90) pageSize = 90;
                    }

                    MatchCompanyUrlAndPositions(searchResult.Data);

                    LogFactory.Info($"【按关键词】正在抓取公司链接！城市：{cityName} 关键词：{keyword} 第 {pageIndex} 页 / 共 {pageSize} 页");
                }
                while (++pageIndex <= pageSize);
            }
        }

        /// <summary>
        /// 初始化执行
        /// </summary>
        public static void Init()
        {
            Task.WaitAll(new List<Task>
            {
                new Action(SpiderWork).LoopStartTask(TimeSpan.FromHours(2)),// 抓取公司信息
                new Action(GetCompanyUrlAndPositions).LoopStartTask(TimeSpan.FromHours(2),()=> DateTime.Now.Hour >= 8 && DateTime.Now.Hour <=18 && cityActionBlock.InputCount == 0) // 抓取调度

            }.ToArray());
        }
    }
}