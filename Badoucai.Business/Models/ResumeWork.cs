using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Badoucai.Business.Model
{
    public class ResumeWork
    {/// <summary>
     /// 开始时间
     /// </summary>
        public string Begin { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public string End { get; set; }

        /// <summary>
        /// 单位名称
        /// </summary>
        public string Company { get; set; }

        /// <summary>
        /// 所在行业
        /// </summary>
        public string Industry { get; set; }

        /// <summary>
        /// 公司规模
        /// </summary>
        public string Size { get; set; }

        /// <summary>
        /// 公司性质
        /// </summary>
        public string Nature { get; set; }

        /// <summary>
        /// 所属部门
        /// </summary>
        public string Department { get; set; }

        /// <summary>
        /// 所在岗位
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// 工作描述
        /// </summary>
        public string Description { get; set; }
    }
}
