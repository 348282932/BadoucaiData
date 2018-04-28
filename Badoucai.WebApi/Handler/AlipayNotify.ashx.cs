using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Web;
using Aop.Api.Util;
using Badoucai.Library;
using Badoucai.WebApi.App_Start;
using Newtonsoft.Json;

namespace Badoucai.WebApi.Handler
{
    /// <inheritdoc />
    /// <summary>
    /// AlipayNotify 的摘要说明
    /// </summary>
    public class AlipayNotify : IHttpHandler
    {
        private readonly string badoucaiHost = ConfigurationManager.AppSettings["BadoucaiHost"];

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/plain";

            /* 实际验证过程建议商户添加以下校验。
            1、商户需要验证该通知数据中的out_trade_no是否为商户系统中创建的订单号，
            2、判断total_amount是否确实为该订单的实际金额（即商户订单创建时的金额），
            3、校验通知中的seller_id（或者seller_email) 是否为out_trade_no这笔单据的对应的操作方（有的时候，一个商户可能有多个seller_id/seller_email）
            4、验证app_id是否为该商户本身。
            */

            var sArray = GetRequestPost(context.Request.Form);

            if (sArray.Count == 0)
            {
                LogFactory.Warn("请求参数为空！请求参数字典：" + JsonConvert.SerializeObject(sArray));

                return;
            }

            if (!AlipaySignature.RSACheckV1(sArray, AlipayConfig.alipay_public_key, AlipayConfig.charset, AlipayConfig.sign_type, false))
            {
                LogFactory.Warn("支付宝验签失败！请求参数字典：" + JsonConvert.SerializeObject(sArray));

                return;
            }

            var seller_id = context.Request.Form["seller_id"];

            var app_id = context.Request.Form["app_id"];

            var trade_status = context.Request.Form["trade_status"];

            var trade_no = context.Request.Form["trade_no"];

            var out_trade_no = context.Request.Form["out_trade_no"];

            var total_amount = context.Request.Form["total_amount"];

            if (seller_id != AlipayConfig.seller_id || app_id != AlipayConfig.app_id)
            {
                LogFactory.Warn("支付宝商户信息异常！请求参数字典：" + JsonConvert.SerializeObject(sArray));

                return;
            }

            //交易状态
            //判断该笔订单是否在商户网站中已经做过处理
            //如果没有做过处理，根据订单号（out_trade_no）在商户网站的订单系统中查到该笔订单的详细，并执行商户的业务程序
            //请务必判断请求时的total_amount与通知时获取的total_fee为一致的
            //如果有做过处理，不执行商户的业务程序

            //注意：
            //退款日期超过可退款期限后（如三个月可退款），支付宝系统发送该交易状态通知

            if (trade_status == "TRADE_FINISHED" || trade_status == "TRADE_SUCCESS")
            {
                RequestFactory.QueryRequest($"https://plugin.{badoucaiHost}/api/plugin/reChargeNotify?orderid={out_trade_no}&tradeid={trade_no}&amount={total_amount}");

                LogFactory.Info("充值成功！请求参数字典：" + JsonConvert.SerializeObject(sArray));

                context.Response.Write("success");
            }
        }

        /// <summary>
        /// 获取请求参数字典
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetRequestPost(NameValueCollection form)
        {
            int i;

            var sArray = new Dictionary<string, string>();

            var coll = form;

            var requestItem = coll.AllKeys;

            for (i = 0; i < requestItem.Length; i++)
            {
                sArray.Add(requestItem[i], form[requestItem[i]]);
            }

            return sArray;
        }

        public bool IsReusable => false;
    }
}