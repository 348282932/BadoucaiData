using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Badoucai.UnitTest
{
    [TestClass]
    public class TestEmailService
    {
        [TestMethod]
        public void EmailService()
        {
            Badoucai.EmailService.EmailService.SendEmail();

            var emailBusiness = new Business.Services.EmailService.EmailBusiness();

            emailBusiness.GetTodayData();
        }
    }
}