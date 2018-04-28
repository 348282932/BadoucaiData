using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;

namespace Badoucai.Library
{
    public class ProxyFanctory
    {
        private static readonly ConcurrentQueue<FreeProxy> queue = new ConcurrentQueue<FreeProxy>();

        private static readonly object lockObj = new object();

        private static bool isFree;

        /// <summary>
        /// 获取免费代理
        /// </summary>
        /// <returns></returns>
        public static FreeProxy GetFreeProxy()
        {
            lock (lockObj)
            {
                FreeProxy proxy;

                if (queue.TryPeek(out proxy)) return proxy;

                DataResult<string> result;

                if (isFree)
                {
                    result = HttpClientFactory.RequestForString("http://www.xdaili.cn/ipagent/freeip/getFreeIps?page=1&rows=10", HttpMethod.Get);
                }
                else
                {
                    result = HttpClientFactory.RequestForString("http://api.xdaili.cn/xdaili-api//privateProxy/applyStaticProxy?spiderId=93e117ceea9e460f83f77341073f87c6&returnType=2&count=1", HttpMethod.Get);
                }

                if (!result.IsSuccess) return null;

                var jObject = JsonConvert.DeserializeObject<dynamic>(result.Data);

                if ((int)jObject.ERRORCODE != 0)
                {
                    Console.WriteLine($"{DateTime.Now} > {jObject.RESULT}");

                    if (((string)jObject.RESULT).Contains("订单") || ((string)jObject.RESULT).Contains("上限"))
                    {
                        isFree = true;
                    }

                    Thread.Sleep(5000);

                    return GetFreeProxy();
                }

                var items = isFree ? jObject.RESULT.rows : jObject.RESULT;

                foreach (var item in items)
                {
                    proxy = new FreeProxy
                    {
                        Ip = item.ip.ToString(),
                        Port = Convert.ToInt32(item.port),
                        IsUsing = false,
                        IsEnable = true,
                    };

                    if(queue.Any(a=>a.Ip == proxy.Ip && a.Port == proxy.Port)) continue;

                    queue.Enqueue(proxy);
                }

                queue.TryPeek(out proxy);

                return proxy;
            }
        }

        public static void NextProxy()
        {
            FreeProxy proxy;

            queue.TryDequeue(out proxy);
        }
    }

    /// <summary>
    /// 免费代理类
    /// </summary>
    public class FreeProxy
    {
        public string Ip { get; set; }

        public int Port { get; set; }

        public bool IsUsing { get; set; }

        public bool IsEnable { get; set; }

        public double ResponseTime { get; set; }
    }
}