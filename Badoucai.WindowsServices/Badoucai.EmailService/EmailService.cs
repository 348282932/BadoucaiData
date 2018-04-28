using System;
using System.Text;
using Badoucai.Library;

namespace Badoucai.EmailService
{
    public class EmailService
    {
        private static System.Timers.Timer timer;

        /// <summary>
        /// 停止任务
        /// </summary>
        public static void Stop()
        {
            timer.Stop();

            timer.Close();
        }

        /// <summary>
        /// 开始任务
        /// </summary>
        public static void Start()
        {
            timer = new System.Timers.Timer { Interval = 1000, Enabled = true };

            timer.Elapsed += Timer_Elapsed;

            timer.Start();
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (e.SignalTime.Hour == 00 && e.SignalTime.Minute == 00 && e.SignalTime.Second == 00)
            {
                while (true)
                {
                    try
                    {
                        SendEmail();

                        break;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                
            }
        }

        /// <summary>
        /// 发送邮件
        /// </summary>
        public static void SendEmail()
        {
            var emailServiceBusiness = new Business.Services.EmailService.EmailBusiness();

            var email = emailServiceBusiness.GetTodayData();
            
            var emailFactory = new EmailFactory("lijingqing@badoucai.net", "longzhijie@badoucai.net");

            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"进来简历总量：{email.ResumeCount}");
            stringBuilder.AppendLine($"投递进来简历量：{email.DeliverCount}");
            stringBuilder.AppendLine($"Cookie 进来简历量：{email.ResumeCount - email.DeliverCount}");
            stringBuilder.AppendLine($"无Json源简历量：{email.NoJsonCount }");
            stringBuilder.AppendLine($"上传简历量：{email.UploadCount}");
            stringBuilder.AppendLine($"简历新增量：{email.CreateCount}");
            stringBuilder.AppendLine($"简历更新量：{email.UpdateCount}");
            stringBuilder.AppendLine($"简历剩余量：{email.SurplusCount}");
            stringBuilder.AppendLine($"本周平均新增量：{email.AvgCreateCount}");

            emailFactory.Body = stringBuilder.ToString();

            emailFactory.IsBodyHtml = false;

            emailFactory.Subject = $"{DateTime.Now.AddDays(-1):yyyy-MM-dd} 每日数据指标统计（龙志杰）";

            emailFactory.Send();
        }
    }
}
