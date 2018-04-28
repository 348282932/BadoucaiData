using System;
using System.Web.Http;
using Aop.Api;
using Aop.Api.Domain;
using Aop.Api.Request;
using Badoucai.WebApi.App_Start;
using System.Configuration;
using Badoucai.WebApi.Models;

namespace Badoucai.WebApi.Controllers
{
    /// <inheritdoc />
    /// <summary>
    /// 支付宝 API
    /// </summary>
    [RoutePrefix("api/Alipay")]
    public class AlipayController : ApiController
    {
        private readonly DefaultAopClient client = new DefaultAopClient(AlipayConfig.gatewayUrl, AlipayConfig.app_id, AlipayConfig.private_key, "json", "1.0", AlipayConfig.sign_type, AlipayConfig.alipay_public_key, AlipayConfig.charset, false);

        private readonly string localHost = ConfigurationManager.AppSettings["LocalHost"];

        private readonly string badoucaiHost = ConfigurationManager.AppSettings["BadoucaiHost"];

        [Route("Payment")]
        public dynamic Payment(dynamic requestParam)
        {
            // 外部订单号，商户网站订单系统中唯一的订单号

            string out_trade_no = requestParam.OrderId;

            // 订单名称

            string subject = requestParam.Subject;

            // 付款金额

            string total_amout = requestParam.TotalAmout;

            // 商品描述

            string body = requestParam.Description;

            // 组装业务参数model

            var model = new AlipayTradePagePayModel
            {
                Body = body,
                Subject = subject,
                TotalAmount = total_amout,
                OutTradeNo = out_trade_no,
                ProductCode = "FAST_INSTANT_TRADE_PAY"
            };

            var request = new AlipayTradePagePayRequest();

            // 设置同步回调地址

            request.SetReturnUrl($"http://www.{badoucaiHost}/plugin/ReChargeReturn");

            // 设置异步通知接收地址

            request.SetNotifyUrl($"{localHost}/Handler/AlipayNotify.ashx");

            // 将业务model载入到request

            request.SetBizModel(model);
            
            try
            {
                var response = client.pageExecute(request, null, "post");

                return new ResponseModels<string>(response.Body);
            }
            catch (Exception ex)
            {
                return new ResponseModels { Code = "20000", Message = ex.Message};
            }
        }
    }
}
