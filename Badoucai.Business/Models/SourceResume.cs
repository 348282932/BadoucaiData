using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Badoucai.Business.Model
{
    public class SourceResume
    {
        public string BdcId { get; set; }
        public string Id { get; set; }
        public string ResumeId { get; set; }
        public string UserMasterId { get; set; }
        public string UserMasterExtId { get; set; }
        public string ResumeNumber { get; set; }
        public string ResumeName { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        public string Birthday { get; set; }

        /// <summary>
        /// 最早工作时间
        /// </summary>
        public string WorkStarts { get; set; }

        /// <summary>
        /// 手机
        /// </summary>
        public string Cellphone { get; set; }

        /// <summary>
        /// 邮件
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 现居地
        /// </summary>
        public string CurrentResidence { get; set; }

        /// <summary>
        /// 户籍地址
        /// </summary>
        public string RegisteredResidenc { get; set; }

        /// <summary>
        /// 学历
        /// </summary>
        public string Degree { get; set; }

        /// <summary>
        /// 修读专业
        /// </summary>
        public string Major { get; set; }

        /// <summary>
        /// 婚姻状态
        /// </summary>
        public string MaritalStatus { get; set; }

        public string Height { get; set; }

        /// <summary>
        /// 求职状态
        /// </summary>
        public string JobStatus { get; set; }

        public string UpdateTime { get; set; }
        public ResumeIntention Intention { get; set; }
        public List<ResumeWork> Works { get; set; }
        public List<ResumeProject> Projects { get; set; }

        public List<ResumeEducation> Educations { get; set; }

        public List<ResumeTraining> Trainings { get; set; }

        public List<ResumeSkill> Skills { get; set; }

        public List<ResumeCertificate> Certificates { get; set; }

        public List<SchoolHonor> Honors { get; set; }
        public List<SchoolPractice> Practices { get; set; }

        public List<OtherInfo> Others { get; set; }
    }
}
