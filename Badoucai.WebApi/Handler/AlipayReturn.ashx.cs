using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using Aop.Api;
using Aop.Api.Domain;
using Aop.Api.Request;
using Aop.Api.Util;
using Badoucai.Library;
using Badoucai.WebApi.App_Start;
using Badoucai.WebApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Badoucai.WebApi.Handler
{
    /// <inheritdoc />
    /// <summary>
    /// 支付宝支付回调
    /// </summary>
    public class AlipayReturn : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/html";

            dynamic param = new { OrderId = "1564842", Subject = "充值", TotalAmout = "0.01", Description = "八斗才充值" };

            var dataResult = RequestFactory.QueryRequest("http://172.13.30.150:8088/api/Alipay/Payment", (string)JsonConvert.SerializeObject(param), RequestEnum.POST, contentType: ContentTypeEnum.Json.Description());

            if (!dataResult.IsSuccess)
            {
                context.Response.Write("请求异常！" + dataResult.ErrorMsg);

                return;
            }

            var str = dataResult.Data.Substring(1, dataResult.Data.Length - 2).Replace("\\","");

            context.Response.Write(str);

        }

        public bool IsReusable => false;
    }
}