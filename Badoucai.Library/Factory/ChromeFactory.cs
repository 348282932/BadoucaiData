using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using OS = OpenQA.Selenium;
using OSC = OpenQA.Selenium.Chrome;

namespace Badoucai.Library
{
    public class ChromeFactory
    {
        public OSC.ChromeDriver _driver;

        public CookieContainer _cookieContainer;

        public ChromeFactory()
        {
            var option = new OSC.ChromeOptions();

            _cookieContainer = new CookieContainer();

            _driver = new OSC.ChromeDriver(GetChromeDriverService(), option);
        }

        /// <summary>
        /// 使用 JS 引擎请求页面
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public void QueryRequest(string url)
        {
            //var domain = Regex.Matches(url, "(?i)(https*?://)*(.+?)/")[0].Result("$2");

            //var index = domain.IndexOf(".", StringComparison.Ordinal);

            //if (index != domain.LastIndexOf(".", StringComparison.Ordinal)) domain = domain.Substring(index);

            //_cookieContainer.GetAllCookies().ForEach(f =>
            //{
            //    _driver.Manage().Cookies.AddCookie(new OS.Cookie(f.Name, f.Value, domain, "/", DateTime.Now.AddDays(1)));
            //});

            //_driver.Url = url;

            //return _driver.FindElementByTagName("body").Text;

            _driver.ExecuteScript("window.location.href = 'https://rd2.zhaopin.com/s/homepage.asp'");
        }

        private static OSC.ChromeDriverService GetChromeDriverService()
        {
            var service = OSC.ChromeDriverService.CreateDefaultService();

            //设置代理服务器地址

            //service.PortServerAddress = "47.92.105.147:16978";

            //设置代理服务器认证信息

            //service.ProxyAuthentication = GetProxyAuthorization();

            return service;
        }
    }
}