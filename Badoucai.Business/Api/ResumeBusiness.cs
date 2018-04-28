using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Aliyun.OSS;
using Badoucai.Business.Model;
using Badoucai.EntityFramework.MySql;
using Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB;
using Badoucai.Library;
using Newtonsoft.Json;
using System.Collections.Generic;
using Badoucai.EntityFramework.PostgreSql.AIF_DB;

namespace Badoucai.Business.Api
{
    public class ResumeBusiness
    {
        private static readonly OssClient mangningOssClient = new OssClient
        (
            ConfigurationManager.AppSettings["Oss.Mangning.Url"],
            ConfigurationManager.AppSettings["Oss.Mangning.KeyId"],
            ConfigurationManager.AppSettings["Oss.Mangning.KeySecret"]
        );

        private static readonly string mangningBucketName = ConfigurationManager.AppSettings["Oss.Mangning.Bucket"];

        private static readonly string uploadFilePath = ConfigurationManager.AppSettings["File.Path"];

        private static readonly Dictionary<string, string> degreeDic = new Dictionary<string, string>
        {
            { "初中及以下", "A" }, { "中技", "B" }, { "高中", "C" },
            { "中专", "D" }, { "大专", "E" }, { "本科", "F" },
            { "硕士", "G" }, { "MBA", "H" }, { "博士", "J" }
        };

        /// <summary>
        /// 上传智联 Josn 格式简历
        /// </summary>
        /// <param name="json"></param>
        /// <param name="jsonResumeId"></param>
        /// <returns></returns>
        public DataResult UploadZhaopinResume(string json, int jsonResumeId)
        {
            try
            {
                var resumeData = JsonConvert.DeserializeObject<dynamic>(json);

                var resumeDetail = JsonConvert.DeserializeObject(resumeData.detialJSonStr.ToString());

                var refreshTime = BaseFanctory.GetTime((string)resumeDetail.DateLastReleased).ToUniversalTime();

                resumeData.detialJSonStr = resumeDetail;

                var resumeNumber = ((string)resumeData.resumeNo).Substring(0, 10);

                var userId = (int)resumeData.userDetials.userMasterId;

                var resumeId = resumeData.resumeId != null ? (int)resumeData.resumeId : resumeDetail.ResumeId != null ? (int)resumeDetail.ResumeId : 0;

                using (var db = new MangningXssDBEntities())
                {
                    var resume = db.ZhaopinResume.FirstOrDefault(f => f.Id == resumeId);

                    if (!(resume?.RefreshTime != null && resume.RefreshTime.Value.Date >= refreshTime.Date))
                    {
                        if (resume != null)
                        {
                            resume.RandomNumber = resumeNumber;
                            resume.RefreshTime = refreshTime;
                            resume.UpdateTime = DateTime.UtcNow;
                            if (string.IsNullOrEmpty(resume.UserExtId)) resume.UserExtId = resumeDetail.UserMasterExtId.ToString();
                            resume.Source = !resume.Source.Contains("Download") ? resume.Source += ",Download" : resume.Source;
                            resume.Flag = 0xE;
                        }
                        else
                        {
                            resume = new ZhaopinResume
                            {
                                Id = resumeId,
                                RandomNumber = resumeNumber,
                                UserId = userId,
                                RefreshTime = refreshTime,
                                UpdateTime = DateTime.UtcNow,
                                UserExtId = resumeDetail.UserMasterExtId.ToString(),
                                DeliveryNumber = null,
                                Source = "Download",
                                Flag = 0xE
                            };

                            db.ZhaopinResume.Add(resume);
                        }

                        var user = db.ZhaopinUser.FirstOrDefault(f => f.Id == userId);

                        if (user != null)
                        {
                            if (!user.Source.Contains("MANUAL"))
                            {
                                user.ModifyTime = BaseFanctory.GetTime((string)resumeDetail.DateModified).ToUniversalTime();
                                user.CreateTime = BaseFanctory.GetTime((string)resumeDetail.DateCreated).ToUniversalTime();
                                user.Cellphone = resumeData.userDetials.mobilePhone.ToString();
                                user.Email = resumeData.userDetials.email.ToString();
                                user.Name = resumeData.userDetials.userName.ToString();
                                user.UpdateTime = DateTime.UtcNow;
                                user.Username = resumeData.userDetials.email.ToString();
                            }
                        }
                        else
                        {
                            user = new ZhaopinUser
                            {
                                Id = userId,
                                Source = "Download",
                                ModifyTime = BaseFanctory.GetTime((string)resumeDetail.DateModified).ToUniversalTime(),
                                CreateTime = BaseFanctory.GetTime((string)resumeDetail.DateCreated).ToUniversalTime(),
                                Cellphone = resumeData.userDetials.mobilePhone.ToString(),
                                Email = resumeData.userDetials.email.ToString(),
                                Name = resumeData.userDetials.userName.ToString(),
                                UpdateTime = DateTime.UtcNow,
                                Username = resumeData.userDetials.email.ToString()
                            };

                            db.ZhaopinUser.Add(user);
                        }

                        var resumeContent = JsonConvert.SerializeObject(resumeData);

                        using (var jsonStream = new MemoryStream(GZip.Compress(Encoding.UTF8.GetBytes(resumeContent))))
                        {
                            mangningOssClient.PutObject(mangningBucketName, $"Zhaopin/Resume/{resumeId}", jsonStream);
                        }

                        var resumePath = $"{uploadFilePath}{resumeId}.json";

                        File.WriteAllText(resumePath, JsonConvert.SerializeObject(resumeData));

                        db.SaveChanges();
                    }
                }

                return new DataResult();
            }
            catch (Exception ex)
            {
                LogFactory.Warn($"简历上传异常！异常信息：{ex.Message}, Json：{json}");

                return new DataResult { ErrorMsg = $"Josn 格式异常，resumeNo：{jsonResumeId}，{ex.Message}", IsSuccess = false };
            }
        }

        public DataResult<dynamic> TryGetContactInfo(dynamic model)
        {
            var data = new List<CoreResumeSummary>();

            using (var db = new BadoucaiAliyunDBEntities())
            {
                var sb = new StringBuilder();

                sb.Append($"SELECT * FROM \"Core_Resume_Summary\" WHERE substring(\"Cellphone\"::VARCHAR(32),8,4) = '{model.CellphonePart}' AND \"UpdateTime\" > '1970-01-01 08:00:00' ");

                if (!string.IsNullOrEmpty((string)model.Birthday)) sb.Append($"AND \"Birthday\" = '{((string)model.Birthday).Remove(((string)model.Birthday).LastIndexOf("-", StringComparison.Ordinal))}-01' ");

                if (!string.IsNullOrEmpty((string)model.Degree)) sb.Append($"AND \"Degree\" = '{degreeDic[(string)model.Degree]}' ");

                if (!string.IsNullOrEmpty((string)model.Gender))
                {
                    var gender = model.Gender == "男" ? "M" : "F";

                    sb.Append($"AND \"Gender\" = '{gender}' ");
                }

                sb.Append(";");

                var list = db.Database.SqlQuery<CoreResumeSummary>(sb.ToString()).ToList();

                using (var aif = new AIFDBEntities())
                {
                    foreach (var item in list)
                    {
                        var address = aif.BaseAreaBDC.AsNoTracking().FirstOrDefault(f => f.Id == item.CurrentResidence);

                        if(address == null) continue;

                        if (string.IsNullOrEmpty((string)model.CurrentResidence) || ((string)model.CurrentResidence).Contains(address.Name)) data.Add(item);
                    }
                }
            }

            return new DataResult<dynamic>(data.Take(5));
        }
    }
}