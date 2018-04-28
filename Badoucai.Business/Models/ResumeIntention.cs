using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Badoucai.Business.Model
{
    public class ResumeIntention
    {
        public string Salary { get; set; }

        /// <summary>
        /// 期望工作地点
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// 期望行业
        /// </summary>
        public string Industry { get; set; }

        /// <summary>
        /// 期望职能
        /// </summary>
        public string Function { get; set; }

        /// <summary>
        /// 到岗时间
        /// </summary>
        public string DutyTime { get; set; }

        /// <summary>
        /// 工作类型
        /// </summary>
        public string JobType { get; set; }

        /// <summary>
        /// 自我评价
        /// </summary>
        public string Evaluation { get; set; }
    }
}
