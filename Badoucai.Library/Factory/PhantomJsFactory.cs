using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using OSP = OpenQA.Selenium.PhantomJS;
using OS = OpenQA.Selenium;

namespace Badoucai.Library
{
    public class PhantomJsFactory
    {
        public OSP.PhantomJSDriver _driver;

        public PhantomJsFactory()
        {
            var option = new OSP.PhantomJSOptions();

            //var preferences = new KeyValuePair<string, object>("chrome.contentSettings.images", "block");

            //option.AddAdditionalCapability("chrome.prefs", preferences);

            _driver = new OSP.PhantomJSDriver(GetPhantomJSDriverService(), option);
        }

        /// <summary>
        /// 使用 JS 引擎请求页面
        /// </summary>
        /// <param name="url"></param>
        /// <param name="cookieContainer"></param>
        /// <returns></returns>
        public string QueryRequest(string url, CookieContainer cookieContainer = null)
        {
            var domain = Regex.Matches(url, "(?i)(https*?://)*(.+?)/")[0].Result("$2");

            var index = domain.IndexOf(".", StringComparison.Ordinal);

            if (index != domain.LastIndexOf(".", StringComparison.Ordinal)) domain = domain.Substring(index);

            cookieContainer.GetAllCookies().ForEach(f =>
            {
                _driver.Manage().Cookies.AddCookie(new OS.Cookie(f.Name, f.Value, domain, "/", DateTime.Now.AddDays(1)));
            });

            _driver.Url = url;

            return _driver.FindElementByTagName("body").Text;
        }

        private static OSP.PhantomJSDriverService GetPhantomJSDriverService()
        {
            var service = OSP.PhantomJSDriverService.CreateDefaultService();

            //设置代理服务器地址

            //service.Proxy = "47.92.105.147:16978";

            //设置代理服务器认证信息

            //service.ProxyAuthentication = GetProxyAuthorization();

            return service;
        }

        public static List<Cookie> GetAllCookies(CookieContainer cc)
        {
            var listCookies = new List<System.Net.Cookie>();

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

                listCookies.AddRange(from CookieCollection colCookies in lstCookieCol.Values from System.Net.Cookie c in colCookies select c);
            }

            return listCookies;
        }
    }
}