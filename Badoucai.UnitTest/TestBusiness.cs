using Badoucai.Business.Api;
using Badoucai.Business.Zhaopin;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Badoucai.UnitTest
{
    [TestClass]
     public class TestBusiness
    {
        [TestMethod]
        public void ZhaopinLogin()
        {
            var emailBusiness = new UserBusiness();

            emailBusiness.Login("17897770016", "a5QPyk6S");
        }
    }
}
