using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB;
using Badoucai.Library;

namespace Badoucai.Business._51Job
{
    public class MatchResumeBusiness
    {
        public DataResult<string> MatchByResumeId(string cookie, string resumeId, long cellphone, DateTime updateTime)
        {
            var referenceNeedUpdate = false;

            using (var db = new BadoucaiAliyunDBEntities())
            {
                var resume = db.CoreResumeSummary.FirstOrDefault(f => f.Cellphone == cellphone);

                var reference = new CoreResumeReference();

                if (resume != null)
                {
                    reference = db.CoreResumeReference.FirstOrDefault(f => f.ResumeId == resume.Id && f.Source == "51JOB");

                    if (reference != null)
                    {
                        long id;

                        if (!long.TryParse(reference.Id, out id))
                        {
                            if (updateTime <= resume.UpdateTime)
                            {
                                return new DataResult<string> { IsSuccess = true, Data = string.Empty }; // 库里已经存在，并且更新时间小于库里时间
                            }

                            return new DataResult<string> { IsSuccess = true, Data = reference.Id };
                        }

                        referenceNeedUpdate = true;
                    }
                }

                var cookieContainer = cookie.Serialize("ehire.51job.com");

                var requestResult = HttpClientFactory.RequestForString("https://ehire.51job.com/Candidate/SearchResumeIndexNew.aspx", HttpMethod.Get, null, cookieContainer);

                if (!requestResult.IsSuccess) return new DataResult<string>("请求异常！");

                var matchResult = Regex.Match(requestResult.Data, "__VIEWSTATE.+?value=\"(\\S+)\"");

                if (!matchResult.Success) return new DataResult<string>("匹配 __VIEWSTATE 异常！");

                var __VIEWSTATE = matchResult.Result("$1");

                var dictionary = new Dictionary<string, string>
                {
                    { "__VIEWSTATE", __VIEWSTATE },
                    {"sex_ch","99|不限" },
                    {"sex_en","99|Unlimited" },
                    {"send_cycle","1" },
                    {"send_time","7" },
                    {"send_sum","10" },
                    {"searchValueHid",resumeId + "##0##########99############1#0###0#0#0" }
                };

                requestResult = HttpClientFactory.RequestForString("https://ehire.51job.com/Candidate/SearchResumeNew.aspx ", HttpMethod.Post, dictionary, cookieContainer);

                if (!requestResult.IsSuccess) return new DataResult<string>("请求异常！");

                matchResult = Regex.Match(requestResult.Data, "hidKey =(\\S+)\"");

                if (!matchResult.Success) return new DataResult<string> {IsSuccess = true, Data = string.Empty};

                var matchResumeId = matchResult.Result("$1");

                matchResult = Regex.Match(requestResult.Data, "hidCheckKey.+?value=\"(\\S+)\"");

                if (!matchResult.Success) return new DataResult<string>("匹配 hidCheckKey 异常！");

                var hidCheckKey = matchResult.Result("$1");

                if(matchResumeId != hidCheckKey) return new DataResult<string> { IsSuccess = true, Data = string.Empty };

                if (referenceNeedUpdate)
                {
                    reference.Id = matchResumeId;

                    
                }

                return new DataResult<string> { IsSuccess = true, Data = matchResumeId };
            }
        }
    }
}