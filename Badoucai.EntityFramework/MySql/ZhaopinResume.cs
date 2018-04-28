using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_Resume")]
    public class ZhaopinResume
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string DeliveryNumber { get; set; }

        public int UserId { get; set; }

        public string UserExtId { get; set; }

        public string Source { get; set; } = "XSS";

        public DateTime? RefreshTime { get; set; }

        public DateTime? UpdateTime { get; set; }

        public string RandomNumber { get; set; }

        /// <summary>
        /// 简历收录时间
        /// </summary>
        public DateTime? IncludeTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 二进制标记（8421：1.是否有源Json 2.json 是否完整 3.json是否有联系方式 4.是否上传过）
        /// </summary>
	    public short Flag { get; set; } = 0x0;
    }
}