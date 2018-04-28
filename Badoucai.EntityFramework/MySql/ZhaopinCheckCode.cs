using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_CheckCode")]
    public class ZhaopinCheckCode
    {
        [Key]
        public int Id { get; set; }

        public string Account { get; set; }

        /// <summary>
        /// 0.待处理 1.正在处理 2.处理完成
        /// </summary>
        public short Status { get; set; }

        public string Cookie { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.Now;

        public DateTime? CompleteTime { get; set; }

        public string HandleUser { get; set; }
        public short Type { get; set; } = 1;
    }
}