using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.ResumeMatch_DB
{
    [Table("OldResumeSummary", Schema = "public")]
    public class OldResumeSummary
    {
        public int Id { get; set; }

        /// <summary>
        /// 简历ID
        /// </summary>
        public string ResumeId { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string Cellphone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 匹配时间
        /// </summary>
        public DateTime MatchTime { get; set; }

        /// <summary>
        /// 是否匹配过
        /// </summary>
        public bool IsMatched { get; set; }

        /// <summary>
        /// 源
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// 模板
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public short Status { get; set; }
    }
}