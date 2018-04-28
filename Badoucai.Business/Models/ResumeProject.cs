using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Badoucai.Business.Model
{
    public class ResumeProject
    {/// <summary>
     /// 开始时间
     /// </summary>
        public string Begin { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public string End { get; set; }

        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 所在单位
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 项目描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 职责描述
        /// </summary>
        public string Duty { get; set; }
    }
}
