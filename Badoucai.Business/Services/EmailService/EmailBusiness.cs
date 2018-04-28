using System;
using System.Linq;
using Badoucai.Business.Model;
using Badoucai.EntityFramework.MySql;

namespace Badoucai.Business.Services.EmailService
{
    public class EmailBusiness
    {
        public EmailSendModel GetTodayData()
        {
            var emailSendModel = new EmailSendModel();

            using (var db = new MangningXssDBEntities())
            {
                var todayTime = DateTime.UtcNow.AddHours(-24);

                var dayOfWeek = (int)DateTime.Now.DayOfWeek;

                if (dayOfWeek == 0) dayOfWeek = 7;

                var dayOfWeekTime = DateTime.Today.AddDays(-(dayOfWeek - 1));

                var query = from a in db.ZhaopinResume
                    join b in db.ZhaopinUser on a.UserId equals b.Id
                    where !string.IsNullOrEmpty(b.Cellphone) && a.UpdateTime > todayTime
                    select a;

                emailSendModel.ResumeCount = query.Count();

                emailSendModel.DeliverCount = query.Count(w => w.Source.Contains("Deliver"));

                var time = DateTime.Today.AddDays(-1);

                emailSendModel.UploadCount = db.ZhaopinResumeUploadLog.Count(w => w.UploadTime > time && !string.IsNullOrEmpty(w.Tag));

                emailSendModel.CreateCount = db.ZhaopinResumeUploadLog.Count(w => w.UploadTime > time && w.Tag == "C");

                emailSendModel.UpdateCount = db.ZhaopinResumeUploadLog.Count(w => w.UploadTime > time && w.Tag != "C" && !string.IsNullOrEmpty(w.Tag));

                emailSendModel.SurplusCount = db.ZhaopinResume.Count(w => w.Flag == 14);

                emailSendModel.NoJsonCount = db.ZhaopinResume.Count(w => w.UpdateTime > todayTime && w.Flag == 2);

                emailSendModel.AvgCreateCount = db.ZhaopinResumeUploadLog.Count(w => w.UploadTime > dayOfWeekTime && w.Tag == "C") / (dayOfWeek - 1);
            }

            return emailSendModel;
        }
    }
}