using System;
using System.Net.Http;
using Tencent.WeChat.API;


namespace Badoucai.Library
{
    public class WeChatFactory
    {
        private static readonly HttpClient http = new HttpClient();

        private const string appId = "wx402fb2e8a343d7ed";

        private const string appSecret = "3ec878b2905af753395a281e9b3c5804";

        /// <summary>
        /// 推送微信公众号消息
        /// </summary>
        /// <param name="content"></param>
        /// <param name="module"></param>
        /// <param name="status"></param>
        /// <param name="remark"></param>
        public static void Send(string content, string module, string status, string remark)
        {
            new TemplateMessageRequestData
            {
                Destination = "obUzxvxf3iPYq6SPovL73zl0tYKQ",
                TemplateId = "nKT1vFYgx2-djgGcP9DALKrYKOxqgvCdFYIMDQ852JQ",
                Data = new
                {
                    First = new { value = content, color = "#ff0000" },
                    Module = new { value = module, color = "#0000ff" },
                    Status = new { value = status, color = "#0000ff" },
                    Time = new { value = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}", color = "#0000ff" },
                    Remark = new { value = remark, color = "#00ff00" }
                }
            }.Send(http, TokenMemoryCache.GetValue(http, appId, appSecret, $"{appId}:{appSecret}:Token"));
        }
    }
}