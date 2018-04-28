using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.ComponentModel;

namespace Badoucai.Library
{
    public class RequestFactory
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";

        private const string defaultAccept = "application/json, text/javascript, */*; q=0.01";

        private static readonly string defaultContentType = ContentTypeEnum.Form.Description();

        /// <summary>
        /// Http请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestParams"></param>
        /// <param name="requestType"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="referer"></param>
        /// <param name="contentType"></param>
        /// <param name="accept"></param>
        /// <param name="isNeedSleep"></param>
        /// <param name="host"></param>
        /// <param name="isEnableFreePrxy"></param>
        /// <returns></returns>
        public static DataResult<string> QueryRequest(string url, string requestParams = "", RequestEnum requestType = RequestEnum.GET, CookieContainer cookieContainer = null, string referer = "", string contentType = "", string accept = "", bool isNeedSleep = false, string host = "", bool isEnableFreePrxy = false)
        {
            var dataResult = new DataResult<string>();

            var retryCount = 2;

            if (isNeedSleep) SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(new Random().Next(1, 2)));

            Retry:

            try
            {
                HttpWebRequest httpRequest;

                if (requestType == RequestEnum.POST)
                {
                    httpRequest = (HttpWebRequest)WebRequest.Create(url);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(requestParams))
                    {
                        httpRequest = (HttpWebRequest)WebRequest.Create(url);
                    }
                    else
                    {
                        httpRequest = (HttpWebRequest)WebRequest.Create(url + "?" + requestParams.Trim());
                    }
                }

                httpRequest.Method = requestType.ToString();

                httpRequest.Timeout = 30 * 1000;

                httpRequest.ContentType = string.IsNullOrWhiteSpace(contentType) ? defaultContentType : contentType;

                httpRequest.Accept = string.IsNullOrWhiteSpace(accept) ? defaultAccept : accept;

                httpRequest.Headers.Add("Accept-Encoding", "gzip, deflate");

                httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                httpRequest.UserAgent = userAgent;

                httpRequest.CookieContainer = cookieContainer;

                if(string.IsNullOrEmpty(requestParams)) httpRequest.ContentLength = 0;

                httpRequest.Headers["X-Forwarded-For"] = BaseFanctory.GetRandomIp();

                if (!url.IsInnerIP() && (!string.IsNullOrEmpty(host) || isEnableFreePrxy))
                {
                    if (!string.IsNullOrEmpty(host))
                    {
                        var index = host.IndexOf(":", StringComparison.Ordinal);

                        httpRequest.Proxy = new WebProxy(host.Substring(0, index), Convert.ToInt32(host.Substring(index + 1)));
                    }
                    else
                    {
                        var proxy = ProxyFanctory.GetFreeProxy();

                        httpRequest.Proxy = new WebProxy(proxy.Ip, proxy.Port);
                    }
                }

                if (!string.IsNullOrWhiteSpace(referer)) httpRequest.Referer = referer;

                if (requestType == RequestEnum.POST && !string.IsNullOrWhiteSpace(requestParams))
                {
                    var encoding = Encoding.GetEncoding("utf-8");

                    var bytesToPost = encoding.GetBytes(requestParams);

                    httpRequest.ContentLength = bytesToPost.Length;

                    var requestStream = httpRequest.GetRequestStream();

                    requestStream.Write(bytesToPost, 0, bytesToPost.Length);

                    requestStream.Close();
                }

                var response = (HttpWebResponse)httpRequest.GetResponse();

                var stream = response.GetResponseStream();

                var reStr = string.Empty;

                if (stream != null)
                {
                    var sr = new StreamReader(stream, Encoding.GetEncoding("utf-8"));

                    reStr = sr.ReadToEnd();

                    sr.Close();
                }
                else
                {
                    dataResult.IsSuccess = false;
                }

                response.Close();

                dataResult.Data = reStr;

                return dataResult;
            }
            catch (WebException ex)
            {
                if(isEnableFreePrxy) ProxyFanctory.NextProxy();

                if (string.IsNullOrWhiteSpace(host) && --retryCount >= 0)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError) SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(2));

                    goto Retry;
                }

                dataResult.IsSuccess = false;

                var hostStr = !string.IsNullOrEmpty(host) ? "Host:" + host : "";

                dataResult.ErrorMsg = $"Web响应异常，请求Url：{url}，{hostStr} 异常信息：{ex.Message}";

                return dataResult;
            }
            catch (Exception ex)
            {
                var hostStr = !string.IsNullOrEmpty(host) ? "Host:" + host : "";

                LogFactory.Error($"请求异常！请求Url：{url} 请求参数：{requestParams} {hostStr} {Environment.NewLine}异常信息：{ex.Message}{Environment.NewLine}{ex.StackTrace}");

                dataResult.IsSuccess = false;

                dataResult.ErrorMsg = $"请求异常！请求Url：{url} {hostStr} 异常信息：{ex.Message} 详情请错误看日志！";

                return dataResult;
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url"></param>
        /// <param name="path"></param>
        /// <param name="requestParams"></param>
        /// <param name="cookieContainer"></param>
        /// <returns></returns>
        public static DataResult HttpDownloadFile(string url, string path, string requestParams = "", CookieContainer cookieContainer = null)
        {
            var dataResult = new DataResult();

            try
            {
                var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

                var httpRequest = (HttpWebRequest)WebRequest.Create(url);

                httpRequest.ContentType = "application/x-www-form-urlencoded";

                httpRequest.Headers["Upgrade-Insecure-Requests"] = "1";

                httpRequest.Method = "POST";

                httpRequest.ContentLength = 0;

                httpRequest.CookieContainer = cookieContainer;

                httpRequest.Headers.Add("Accept-Encoding", "gzip, deflate");

                httpRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                httpRequest.UserAgent = userAgent;

                httpRequest.Accept = defaultAccept;

                if (!string.IsNullOrWhiteSpace(requestParams))
                {
                    var encoding = Encoding.GetEncoding("utf-8");

                    var bytesToPost = encoding.GetBytes(requestParams);

                    httpRequest.ContentLength = bytesToPost.Length;

                    var requestStream = httpRequest.GetRequestStream();

                    requestStream.Write(bytesToPost, 0, bytesToPost.Length);

                    requestStream.Close();
                }

                using (var response = httpRequest.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        var bArr = new byte[1024];

                        if (responseStream != null)
                        {
                            var size = responseStream.Read(bArr, 0, bArr.Length);

                            while (size > 0)
                            {
                                fs.Write(bArr, 0, size);

                                size = responseStream.Read(bArr, 0, bArr.Length);
                            }

                            responseStream.Close();
                        }

                        fs.Close();
                    }
                }

            }
            catch (Exception ex)
            {
                LogFactory.Error($"下载异常，请求Url:{url},{Environment.NewLine}异常信息：{ex.Message}{Environment.NewLine}{ex.StackTrace}");

                dataResult.IsSuccess = false;

                dataResult.ErrorMsg = $"请求异常，请求Url:{url},异常信息：{ex.Message}，详情请错误看日志！";
            }

            return dataResult;
        }

    }

    public enum RequestEnum
    {
        POST, GET
    }

    public enum ContentTypeEnum
    {
        [Description("application/x-www-form-urlencoded")]
        Form,
        [Description("application/json")]
        Json,
        [Description("text/html")]
        Text
    }
}


       


