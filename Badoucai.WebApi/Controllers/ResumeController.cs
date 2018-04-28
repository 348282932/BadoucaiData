using System.Web.Http;
using Badoucai.Library;
using Badoucai.WebApi.Models;
using Badoucai.Business.Api;

namespace Badoucai.WebApi.Controllers
{
    [RoutePrefix("api/Resume")]
    public class ResumeController : ApiController
    {
        [HttpPost]
        [Route("WorkExperienceEdit")]
        public dynamic WorkExperienceEdit(dynamic requestParam)
        {
            foreach (var item in requestParam)
            {
                var param = $"Language_ID=1&ext_id={item.ResumeNumber}&Resume_ID={item.ResumeId}&Version_Number=1&RowID=0&SaveType=0&cmpany_name=%E6%B7%B1%E5%9C%B3%E8%89%AF%E9%A3%9F%E7%BD%91&industry=210500&customSubJobtype=%E8%BF%90%E8%90%A5%E6%80%BB%E7%9B%91&SchJobType=160200&subJobType=2047&jobTypeMain=160200&subJobTypeMain=2047&workstart_date_y=2014&workstart_date_m=05&workend_date_y=2017&workend_date_m=09&salary_scope=1000115000&job_description={item.JobDescription}&company_type=&company_size=";

                var dataResult = RequestFactory.QueryRequest("https://i.zhaopin.com/Resume/WorkExperienceEdit/Save", param, RequestEnum.POST, ((string)item.Cookie).Serialize("zhaopin.com"));

                if(!dataResult.IsSuccess) LogFactory.Warn($"简历ID：{item.ResumeId} 修改失败！ {dataResult.ErrorMsg}");
            }

            return new ResponseModels();
        }

        [HttpPost]
        [Route("UploadZhaopinResume")]
        public dynamic UploadZhaopinResume(dynamic requestParam)
        {
            var business = new ResumeBusiness();

            var result = business.UploadZhaopinResume((string)requestParam.resumeData, (int)requestParam.resumeId);

            if (!result.IsSuccess) return new ResponseModels { Code = "10001", Message = result.ErrorMsg };

            return new ResponseModels();
        }

        [HttpPost]
        [Route("TryGetContactInfo")]
        public dynamic TryGetContactInfo(dynamic requestParam)
        {
            var business = new ResumeBusiness();

            var result = business.TryGetContactInfo(requestParam);

            if (!result.IsSuccess) return new ResponseModels { Code = "10001", Message = result.ErrorMsg };

            return new ResponseModels<dynamic>(result.Data);
        }
    }
}
