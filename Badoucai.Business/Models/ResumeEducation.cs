using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Badoucai.Business.Model
{
    public class ResumeEducation
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
        /// 学校
        /// </summary>
        public string School { get; set; }

        /// <summary>
        /// 学历
        /// </summary>
        public string Degree { get; set; }

        /// <summary>
        /// 修读专业
        /// </summary>
        public string Major { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
    }
}
