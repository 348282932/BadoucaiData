using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace Badoucai.Library
{
    public class HttpClientFactory
    {
        private const string defaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36";

        private const string defaultAccept = "application/json, text/javascript, */*; q=0.01";

        private static readonly string defaultContentType = ContentTypeEnum.Form.Description();

        private static readonly object locker = new object();

        private static HttpClient _client;

        private static readonly CookieContainer _cookieContainer = new CookieContainer();

        private static readonly WebProxy _webProxy = new WebProxy();

        private static HttpClient GetClient(CookieContainer cookieContainer, WebProxy webProxy)
        {
            if (webProxy != null) _webProxy.Address = webProxy.Address;

            if (cookieContainer != null)
            {
                _cookieContainer.GetAllCookies().Select(s => s.Domain).Distinct().ToList().ForEach(f =>
                {
                    foreach (var cookie in _cookieContainer.GetCookies(new Uri("http://www" + f)).OfType<Cookie>())
                    {
                        cookie.Expired = false;
                    }
                });

                cookieContainer.GetAllCookies().ForEach(_cookieContainer.Add);
            }
            if (_client == null)
            {
                lock (locker)
                {
                    if (_client == null)
                    {
                        _client = new HttpClient(new HttpClientHandler
                        {
                            CookieContainer = _cookieContainer ?? new CookieContainer(),
                            Proxy = _webProxy,
                            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None
                        });
                        
                        _client.DefaultRequestHeaders.Connection.Add("keep-alive");
                    }
                }
            }

            return _client;
        }

        /// <summary>
        /// Http 请求（返回响应源码）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="dictionary"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="referer"></param>
        /// <param name="contentType"></param>
        /// <param name="accept"></param>
        /// <param name="userAgent"></param>
        /// <param name="isRandomIP"></param>
        /// <param name="webProxy"></param>
        /// <returns></returns>
        public static DataResult<string> RequestForString(string url, HttpMethod method, IEnumerable<KeyValuePair<string, string>> dictionary = null, CookieContainer cookieContainer = null, string referer = "", string contentType = "", string accept = "", string userAgent = "", bool isRandomIP = false, WebProxy webProxy = null)
        {
            var dataResult = new DataResult<string>();

            var responseResult = Request(url, method, dictionary, cookieContainer, referer, contentType, accept, userAgent, isRandomIP, webProxy);

            if (!responseResult.IsSuccess)
            {
                dataResult.IsSuccess = false;

                dataResult.ErrorMsg = responseResult.ErrorMsg;

                return dataResult;
            }

            dataResult.Data = responseResult.Data.ReadAsStringAsync().Result;

            return dataResult;
        }

        /// <summary>
        /// Http 请求（返回响应流）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="dictionary"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="referer"></param>
        /// <param name="contentType"></param>
        /// <param name="accept"></param>
        /// <param name="userAgent"></param>
        /// <param name="isRandomIP"></param>
        /// <param name="webProxy"></param>
        /// <returns></returns>
        public static DataResult<Stream> RequestForStream(string url, HttpMethod method, IEnumerable<KeyValuePair<string, string>> dictionary = null, CookieContainer cookieContainer = null, string referer = "", string contentType = "", string accept = "", string userAgent = "", bool isRandomIP = false, WebProxy webProxy = null)
        {
            var dataResult = new DataResult<Stream>();

            var responseResult = Request(url, method, dictionary, cookieContainer, referer, contentType, accept, userAgent, isRandomIP, webProxy);

            if (!responseResult.IsSuccess)
            {
                dataResult.IsSuccess = false;

                dataResult.ErrorMsg = responseResult.ErrorMsg;

                return dataResult;
            }

            dataResult.Data = responseResult.Data.ReadAsStreamAsync().Result;

            return dataResult;
        }

        /// <summary>
        /// HttpClient 请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="method"></param>
        /// <param name="dictionary"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="referer"></param>
        /// <param name="contentType"></param>
        /// <param name="accept"></param>
        /// <param name="userAgent"></param>
        /// <param name="isRandomIP"></param>
        /// <param name="webProxy"></param>
        /// <returns></returns>
        private static DataResult<HttpContent> Request(string url, HttpMethod method, IEnumerable<KeyValuePair<string,string>> dictionary = null, CookieContainer cookieContainer = null, string referer = "", string contentType = "", string accept = "", string userAgent = "", bool isRandomIP = false, IWebProxy webProxy = null)
        {
            var dataResult = new DataResult<HttpContent>();

            if(url.Contains("https")) ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;

            try
            {
                using (var client = new HttpClient(new HttpClientHandler
                {
                    CookieContainer = cookieContainer ?? new CookieContainer(),
                    Proxy = webProxy,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None
                }))
                {
                    client.DefaultRequestHeaders.Add("Accept", string.IsNullOrWhiteSpace(accept) ? defaultAccept : accept);

                    client.DefaultRequestHeaders.Add("User-Agent", string.IsNullOrWhiteSpace(userAgent) ? defaultUserAgent : userAgent);

                    //if (isRandomIP) client.DefaultRequestHeaders.Add("X-Forwarded-For", BaseFanctory.GetRandomHost());

                    if (!string.IsNullOrEmpty(referer)) client.DefaultRequestHeaders.Add("Referer", referer);

                    var httpRequestMessage = new HttpRequestMessage(method, new Uri(url));

                    if (method == HttpMethod.Post && dictionary != null)
                    {
                        var httpContent = new FormUrlEncodedContent(dictionary);

                        httpContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrEmpty(contentType) ? defaultContentType : contentType);

                        httpRequestMessage.Content = httpContent;
                    }

                    dataResult.Data = client.SendAsync(httpRequestMessage).Result.Content;

                    return dataResult;
                }
            }
            catch (WebException ex)
            {
                dataResult.IsSuccess = false;

                dataResult.ErrorMsg = $"Web响应异常，请求Url：{url}，{JsonConvert.SerializeObject(dictionary)} 异常信息：{ex.Message}";

                return dataResult;
            }
            catch (Exception ex)
            {
                while (true)
                {
                    if (ex.InnerException == null) break;

                    ex = ex.InnerException;
                }

                LogFactory.Error($"请求异常！请求Url：{url} 请求参数：{JsonConvert.SerializeObject(dictionary)} {Environment.NewLine}异常信息：{ex.Message}{Environment.NewLine}{ex.StackTrace}");

                dataResult.IsSuccess = false;

                dataResult.ErrorMsg = $"请求异常！请求Url：{url} 异常信息：{ex.Message} 详情请错误看日志！";

                return dataResult;
            }
            finally
            {
                if (cookieContainer != null)
                {
                    cookieContainer.GetAllCookies().Select(s => s.Domain).Distinct().ToList().ForEach(f =>
                    {
                        foreach (var cookie in cookieContainer.GetCookies(new Uri("http://www" + f)).OfType<Cookie>())
                        {
                            cookie.Expired = false;
                        }
                    });

                    _cookieContainer.GetAllCookies().ForEach(cookieContainer.Add);
                }
            }
        }
    }
}