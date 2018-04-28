using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Badoucai.Library
{
    public class BaseFanctory
    {
        /// <summary>
        /// 获取 Unix 时间戳
        /// </summary>
        /// <returns></returns>
        public static string GetUnixTimestamp()
        {
            var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));

            return (DateTime.Now - startTime).TotalSeconds.ToString(CultureInfo.InvariantCulture);


            //return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000 + random.Next(0,1000).ToString("000");
        }

        /// <summary>
        /// Unix 时间戳转为 C# 格式时间
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <returns></returns>
        public static DateTime GetTime(string timeStamp)
        {
            DateTime dt;

            if (DateTime.TryParse(timeStamp, out dt)) return dt;

            //处理字符串,截取括号内的数字

            var strStamp = Regex.Matches(timeStamp, @"(?<=\()((?<gp>\()|(?<-gp>\))|[^()]+)*(?(gp)(?!))").Cast<Match>().Select(t => t.Value).ToArray()[0];

            //处理字符串获取+号前面的数字

            var str = Convert.ToInt64(strStamp.Substring(0, strStamp.IndexOf("+", StringComparison.Ordinal)));

            var timeTricks = new DateTime(1970, 1, 1).Ticks + str * 10000 + TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).Hours * 3600 * (long)10000000;

            return new DateTime(timeTricks);
        }

        /// <summary>
        /// 拷贝文件夹
        /// </summary>
        /// <param name="fromDir"></param>
        /// <param name="toDir"></param>
        public static void CopyDir(string fromDir, string toDir)
        {
            if (!Directory.Exists(fromDir)) return;

            if (!Directory.Exists(toDir)) Directory.CreateDirectory(toDir);

            var files = Directory.GetFiles(fromDir);

            foreach (var formFileName in files)
            {
                var fileName = Path.GetFileName(formFileName);

                var toFileName = Path.Combine(toDir, fileName);

                File.Copy(formFileName, toFileName);
            }

            var fromDirs = Directory.GetDirectories(fromDir);

            foreach (var fromDirName in fromDirs)
            {
                var dirName = Path.GetFileName(fromDirName);

                if (dirName != null)
                {
                    var toDirName = Path.Combine(toDir, dirName);

                    CopyDir(fromDirName, toDirName);
                }
            }
        }

        public static string GetRandomIp()
        {
            var random = new Random();

            return $"{random.Next(1,255)}.{random.Next(1, 255)}.{random.Next(1, 255)}.{random.Next(1, 255)}.";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static string BuildChromeCookie(string value, string domain)
        {
            var data = new ArrayList();

            foreach (var s in value.Split(';'))
            {
                var match = Regex.Match(s, @"^\s*(.*?)\s*=\s*(.*?)\s*$");

                if (match.Success)
                {
                    data.Add(new
                    {
                        domain,
                        hostOnly = true,
                        httpOnly = false,
                        name = match.Result("$1"),
                        path = "/",
                        sameSite = "no_restriction",
                        secure = false,
                        session = false,
                        storeId = "0",
                        value = match.Result("$2"),
                        id = data.Count + 1
                    });
                }
            }

            return JsonConvert.SerializeObject(data, Formatting.Indented);
        }
    }
}