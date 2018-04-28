using Badoucai.EntityFramework.MySql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Badoucai.Library;

namespace Badoucai.Service
{
    public class SpiderTycCompanyThread : BaseThread
    {
        private static readonly ConcurrentQueue<ZhaopinCompany> companyQueue = new ConcurrentQueue<ZhaopinCompany>();

        public override Thread Create()
        {
            return new Thread(() =>
            {
                try
                {
                    LoadCompanyName();

                    for (var i = 0; i < 8; i++)
                    {
                        Task.Run(() => SpiderTycCompany());
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

        private static void LoadCompanyName()
        {
            var addressArray = new[] { "深圳", "广州", "上海" };

            using (var db = new MangningXssDBEntities())
            {
                var companys = db.ZhaoPinCompany.AsNoTracking().Where(w => addressArray.Any(a => w.Address.Contains(a)) && w.Balance.HasValue).ToList();

                foreach (var company in companys) companyQueue.Enqueue(company);
            }

            Console.WriteLine($"{DateTime.Now} > Companys Load complete ! Count = {companyQueue.Count}");
        }

        private static void SpiderTycCompany()
        {
            var companyInfoList = new List<CompanyInfo>();

            //var cookieContainer = "csrfToken=1OuYK5favufgGSa1gd7U8FwG; jsid=SEM-BAIDU-PP-SY-000257; ssuid=395297000; Hm_lvt_e92c8d65d92d534b0fc290df538b4758=1521686318; RTYCID=51d4bc8d802f492fb0f7ddafe3235a1a; token=8641e6b4d4ec47989e0cd5388ed10eab; _utm=461825d8c2f04fcd80e8952652550162; tyc-user-info=%257B%2522new%2522%253A%25221%2522%252C%2522token%2522%253A%2522eyJhbGciOiJIUzUxMiJ9.eyJzdWIiOiIxODkzODY1MzQwMiIsImlhdCI6MTUyMTc3NjMxOCwiZXhwIjoxNTM3MzI4MzE4fQ.MZZ1qEkosb3vcdEzFSxYR7DOH8BVSlqgGB6SiRXSTo_rW3xA6NXc4NwhH7lOhLc4iugMKbgZB0WHIizDlbe0YA%2522%252C%2522integrity%2522%253A%25220%2525%2522%252C%2522state%2522%253A%25220%2522%252C%2522redPoint%2522%253A%25220%2522%252C%2522vipManager%2522%253A%25220%2522%252C%2522vnum%2522%253A%25220%2522%252C%2522onum%2522%253A%25220%2522%252C%2522mobile%2522%253A%252218938653402%2522%257D; auth_token=eyJhbGciOiJIUzUxMiJ9.eyJzdWIiOiIxODkzODY1MzQwMiIsImlhdCI6MTUyMTc3NjMxOCwiZXhwIjoxNTM3MzI4MzE4fQ.MZZ1qEkosb3vcdEzFSxYR7DOH8BVSlqgGB6SiRXSTo_rW3xA6NXc4NwhH7lOhLc4iugMKbgZB0WHIizDlbe0YA; Hm_lpvt_e92c8d65d92d534b0fc290df538b4758=1521776322".Serialize(".tianyancha.com");

            var cookieContainer = new CookieContainer();

            while (true)
            {
                ZhaopinCompany company;

                if (!companyQueue.TryDequeue(out company))
                {
                    Thread.Sleep(100);

                    break;
                }

                var companyName = company.Name.Trim();

                var request = RequestFactory.QueryRequest($"https://www.tianyancha.com/search?key={HttpUtility.UrlEncode(companyName)}", cookieContainer: cookieContainer, isEnableFreePrxy:true);

                if (!request.IsSuccess)
                {
                    Trace.WriteLine($"{DateTime.Now} > Search error ! Search Name = {companyName}, Message = {request.ErrorMsg}.");

                    Console.WriteLine();

                    ProxyFanctory.NextProxy();

                    companyQueue.Enqueue(company);

                    continue;
                }

                if (request.Data.Contains("我们只是确认一下你不是机器人"))
                {
                    Console.WriteLine($"{DateTime.Now} > Please enter verification code !");

                    //Thread.Sleep(3000);

                    ProxyFanctory.NextProxy();

                    companyQueue.Enqueue(company);

                    continue;
                }

                if (request.Data.Contains("验证即登录"))
                {
                    Console.WriteLine($"{DateTime.Now} > Please Login !");

                    ProxyFanctory.NextProxy();

                    companyQueue.Enqueue(company);

                    continue;
                }

                var match = Regex.Match(request.Data, $"(?s)position-rel\"><img alt=\"{companyName.Replace("(","（").Replace(")","）")}\".+?<div><a href=\"(.+?)\".+?title=\"(.+?)\"");

                if (!match.Success)
                {
                    Console.WriteLine($"{DateTime.Now} > Search Name = {companyName}, Company match failed.");

                    Console.WriteLine();

                    continue;
                }

                request = RequestFactory.QueryRequest(match.Result("$1"), cookieContainer: cookieContainer, isEnableFreePrxy: true);

                if (!request.IsSuccess)
                {
                    Trace.WriteLine($"{DateTime.Now} > Watch detail error ! Search Name = {companyName}, Message = {request.ErrorMsg}.");

                    Console.WriteLine();

                    ProxyFanctory.NextProxy();

                    companyQueue.Enqueue(company);

                    continue;
                }

                if (request.Data.Contains("我们只是确认一下你不是机器人"))
                {
                    Console.WriteLine($"{DateTime.Now} > Please enter verification code !");

                    //Thread.Sleep(3000);

                    ProxyFanctory.NextProxy();

                    companyQueue.Enqueue(company);

                    continue;
                }

                var boosName = match.ResultOrDefault("$2", "");

                var email = Regex.Match(request.Data, "邮箱.+?title=\"(.*?)\"").ResultOrDefault("$1","");

                var cellphone = Regex.Match(request.Data, ">电话：</span><span>(.*?)<").ResultOrDefault("$1", "");

                Console.WriteLine($"{DateTime.Now} > Match Complete ! Count = {companyQueue.Count};");
                Console.WriteLine($"{DateTime.Now} > Match CompnayName = {companyName}, BossName = {boosName};");
                Console.WriteLine($"{DateTime.Now} > BossCellphone = {cellphone}, BossEmail = {email};");
                Console.WriteLine($"{DateTime.Now} > HRCellphone = {company.Telephone}, HREmail = {company.Email}.");
                Console.WriteLine();

                companyInfoList.Add(new CompanyInfo
                {
                    Address = company.Address,
                    Balance = company.Balance,
                    BossCellphone = cellphone,
                    BossEmail = email,
                    HRCellphone = company.Telephone,
                    HREmail = company.Email,
                    Name = company.Name,
                    BoosName = boosName
                });
            }

            var sb = new StringBuilder();

            sb.AppendLine("公司\t余额\tBoss 姓名\tBoss 电话\tBoss 邮箱\tHR 电话\tHR邮箱\t公司地址");

            companyInfoList.ForEach(f =>
            {
                sb.AppendLine($"{f.Name}\t{f.Balance}\t{f.BoosName}\t{f.BossCellphone}\t{f.BossEmail}\t{f.HRCellphone}\t{f.HREmail}\t{f.Address}");
            });

            File.WriteAllText(@"D:\深圳公司信息.txt",sb.ToString());
        }

        public static void AddName()
        {
            var rows = File.ReadLines(@"D:\深圳公司信息.txt");

            var sb = new StringBuilder();

            var index = 0;

            foreach (var row in rows)
            {
                if (index++ == 0)
                {
                    sb.AppendLine(row + "\tHR姓名");

                    continue;
                }

                var arr = row.Split("\t");

                var companyName = arr[0];

                var addressArray = new[] { "深圳", "广州", "上海" };

                var hrName = "";

                var tempRow = row;

                using (var db = new MangningXssDBEntities())
                {
                    var company = db.ZhaoPinCompany.AsNoTracking().FirstOrDefault(w => addressArray.Any(a => w.Address.Contains(a)) && w.Balance.HasValue && w.Name == companyName);

                    if (company != null)
                    {
                        hrName = company.Contactor;

                        if (!string.IsNullOrWhiteSpace(company.Cellphone))
                        {
                            Console.WriteLine($"{DateTime.Now} > Index = {index}, HrName = {company.Contactor}, OldCellphone = {arr[5]}, NewCellphone = {company.Cellphone}.");

                            arr[5] = company.Cellphone;

                            tempRow = string.Join("\t", arr);
                        }
                    }

                    sb.AppendLine($"{tempRow}\t{hrName}");
                }
            }

            File.WriteAllText(@"D:\企业信息.txt",sb.ToString());
        }
    }

    public struct CompanyInfo
    {
        public string Name { get; set; }

        public string Address { get; set; }

        public string BoosName { get; set; }

        public string BossCellphone { get; set; }

        public string BossEmail { get; set; }

        public string HRCellphone { get; set; }

        public string HREmail { get; set; }

        public int? Balance { get; set; }
    }
}
