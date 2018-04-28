using System.Net;
using System.Web;
using Badoucai.Library;

namespace Badoucai.Business.Zhaopin
{
    public class UserBusiness
    {
        public CookieContainer Login(string userName, string password)
        {
            var cookieContainer = new CookieContainer();

            var param = $"int_count=999&errUrl=https%3A%2F%2Fpassport.zhaopin.com%2Faccount%2Flogin&RememberMe=true&requestFrom=portal&loginname={HttpUtility.UrlEncode(userName)}&Password={HttpUtility.UrlEncode(password)}";

            var dataResult = RequestFactory.QueryRequest("https://passport.zhaopin.com/account/login", param, RequestEnum.POST, cookieContainer);

            if (!dataResult.IsSuccess) LogFactory.Warn($"用户：{userName} 登录失败！ {dataResult.ErrorMsg}");

            return cookieContainer;
        }
    }
}