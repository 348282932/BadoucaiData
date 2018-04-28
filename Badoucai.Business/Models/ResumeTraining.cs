using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Badoucai.Business.Model
{
    public class ResumeTraining
    {
        /// <summary>
        /// 开始时间
        /// </summary>
        public string Begin { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public string End { get; set; }

        /// <summary>
        /// 培训机构
        /// </summary>
        public string Institution { get; set; }

        /// <summary>
        /// 培训地点
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// 培训课程
        /// </summary>
        public string Course { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
    }
}
