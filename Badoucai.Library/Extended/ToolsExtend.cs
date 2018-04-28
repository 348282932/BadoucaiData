using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Badoucai.Library
{
    public static class ToolsExtend
    {

        /// <summary>
        /// 循环执行任务
        /// </summary>
        /// <param name="action">任务体</param>
        /// <param name="timeSpan">执行一次任务的休眠间隔</param>
        /// <param name="condition">执行条件</param>
        /// <param name="threadCount">线程数</param>
        /// <returns></returns>
        public static Task LoopStartTask(this Action action, TimeSpan timeSpan, Func<bool> condition = null, int threadCount = 1)
        {
            return Task.Run(() =>
            {
                var taskList = new List<Task>();

                for (var i = 0; i < threadCount; i++)
                {
                    taskList.Add(Task.Run(() =>
                    {
                        while (true)
                        {
                            try
                            {
                                action.Invoke();
                            }
                            catch (Exception ex)
                            {
                                while (true)
                                {
                                    if (ex.InnerException == null) break;

                                    ex = ex.InnerException;
                                }

                                LogFactory.Error($"异常信息：{ex.Message} 堆栈信息：{ex.StackTrace}");
                            }

                            Thread.Sleep(timeSpan);

                            if (condition != null)
                            {
                                while (true)
                                {
                                    if(condition.Invoke()) break;

                                    Thread.Sleep(1000);
                                }
                            }
                        }
                    }));
                }

                Task.WaitAll(taskList.ToArray());
            });
        }

        /// <summary>
        /// (深复制)克隆泛型集合
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="List">The list.</param>
        /// <returns>List{``0}.</returns>
        public static List<T> Clone<T>(this object List)
        {
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objectStream, List);
                objectStream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(objectStream) as List<T>;
            }
        }

        /// <summary>
        /// (深复制)克隆类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The list.</param>
        /// <returns>List{``0}.</returns>
        public static T Clone<T>(this T obj) where T : class 
        {
            using (Stream objectStream = new MemoryStream())
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(objectStream, obj);
                objectStream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(objectStream) as T;
            }
        }

        /// <summary>
        /// 序列化 Cookie
        /// </summary>
        /// <param name="value"></param>
        /// <param name="domain"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static CookieContainer Serialize( this string value, string domain, string path = "/")
        {
            var cookie = new CookieContainer { PerDomainCapacity = 100 };

            try
            {
                foreach (Match match in Regex.Matches(value, @"(\S*?)=(.+?)(;|\s|$)"))
                {
                    if (match.Result("$2").Contains(","))
                    {
                        continue;
                    }

                    if (match.Result("$2").StartsWith(";"))
                    {
                        cookie.Add(new Cookie(match.Result("$1"), "", path, domain));

                        continue;
                    }

                    cookie.Add(new Cookie(match.Result("$1"), match.Result("$2"), path, domain));
                }

                return cookie;
            }
            catch (Exception ex)
            {
                throw new Exception("序列化 Cookie 异常！", ex);
            }
        }

        [DllImport("wininet.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool InternetSetCookie(string lpszUrlName, string lbszCookieName, string lpszCookieData);

        /// <summary>
        /// 序列化 Cookie
        /// </summary>
        /// <param name="value"></param>
        /// <param name="domain"></param>
        /// <param name="url"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static void WebBrowserSerialize(this string value, string domain, string url, string path = "/")
        {
            try
            {
                foreach (Match match in Regex.Matches(value, @"(\S*?)=(.+?)(;|\s|$)"))
                {
                    if (match.Result("$2").Contains(","))
                    {
                        continue;
                    }

                    if (match.Result("$2").StartsWith(";"))
                    {
                        InternetSetCookie(url, null, new Cookie(match.Result("$1"), "", path, domain).ToString());

                        continue;
                    }

                    InternetSetCookie(url, null, new Cookie(match.Result("$1"), match.Result("$2"), path, domain).ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("序列化 Cookie 异常！", ex);
            }
        }

        /// <summary>
        /// 获取容器里的Cookie列表
        /// </summary>
        /// <param name="cc"></param>
        /// <returns></returns>
        public static List<Cookie> GetAllCookies(this CookieContainer cc)
        {
            var listCookies = new List<Cookie>();

            if (cc == null) return listCookies;

            var table = (Hashtable)cc.GetType().InvokeMember("m_domainTable",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.GetField |
                System.Reflection.BindingFlags.Instance,
                null, cc, new object[] { });

            foreach (var pathList in table.Values)
            {
                var lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.GetField |
                    System.Reflection.BindingFlags.Instance,
                    null, pathList, new object[] { });

                listCookies.AddRange(from CookieCollection colCookies in lstCookieCol.Values from Cookie c in colCookies select c);
            }

            return listCookies;
        }


        /// <summary>
        /// 判断IP地址是否为内网IP地址
        /// </summary>
        /// <param name="ipAddress">IP地址字符串</param>
        /// <returns></returns>
        public static bool IsInnerIP(this string ipAddress)
        {
            if (ipAddress.Contains("192.168")) return true; //TODO: Error

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private static int LevenshteinDistance(string source, string target)
        {
            var columnSize = source.Length;

            var rowSize = target.Length;

            if (columnSize == 0) return rowSize;

            if (rowSize == 0) return columnSize;

            var matrix = new int[columnSize + 1, rowSize + 1];

            for (var i = 0; i <= columnSize; i++)
            {
                matrix[i, 0] = i;
            }
            for (var j = 0; j <= rowSize; j++)
            {
                matrix[0, j] = j;
            }

            for (var i = 1; i <= columnSize; i++)
            {
                for (var j = 1; j <= rowSize; j++)
                {
                    var sign = source[i - 1].Equals(target[j - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + sign);
                }
            }

            return matrix[columnSize, rowSize];
        }

        /// <summary>
        /// 字符串相似度（Levenshtein Distance 算法）
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static float StringSimilarity(this string source, string target)
        {
            var distance = LevenshteinDistance(source, target);

            float maxLength = Math.Max(source.Length, target.Length);

            return (maxLength - distance) / maxLength;
        }

        /// <summary>
        /// 序列化请求参数
        /// </summary>
        /// <param name="paramDictionary"></param>
        /// <returns></returns>
        public static string SerializeRequestDic(this Dictionary<string, string> paramDictionary)
        {
            return paramDictionary.Aggregate("", (current, temp) => current + $"&{HttpUtility.UrlEncode(temp.Key)}={HttpUtility.UrlEncode(temp.Value)}").Substring(1);
        }

        /// <summary>
        /// 转全角的函数(SBC case)
        /// 全角空格为12288，半角空格为32</summary>
        /// 其他字符半角(33-126)与全角(65281-65374)的对应关系是：均相差65248<param name="input"></param>
        /// <returns></returns>
        public static string ToSBC(this string input)
        {
            var c = input.ToCharArray();

            for (var i = 0; i < c.Length; i++)
            {
                if (c[i] == 32)
                {
                    c[i] = (char)12288;

                    continue;
                }

                if (c[i] < 127) c[i] = (char)(c[i] + 65248);
            }

            return new string(c);
        }

        /// <summary>
        /// 转半角的函数(DBC case)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string ToDBC(this string input)
        {
            var c = input.ToCharArray();

            for (var i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)
                {
                    c[i] = (char)32;

                    continue;
                }
                if (c[i] > 65280 && c[i] < 65375) c[i] = (char)(c[i] - 65248);
            }

            return new string(c);
        }

        /// <summary>
        /// 获取Md5哈希串
        /// </summary>
        /// <param name="sDataIn"></param>
        /// <returns></returns>
        public static string GetMD5(this string sDataIn)
        {
            var md5 = new MD5CryptoServiceProvider();

            var bytValue = System.Text.Encoding.UTF8.GetBytes(sDataIn);

            var bytHash = md5.ComputeHash(bytValue);

            md5.Clear();

            var sTemp = bytHash.Aggregate("", (current, t) => current + t.ToString("X").PadLeft(2, '0'));

            return sTemp.ToLower();
        }
    }
}