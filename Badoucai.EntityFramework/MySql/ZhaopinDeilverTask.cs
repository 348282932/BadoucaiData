using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_DeilverTask")]
    public class ZhaopinDeilverTask
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public short ExpectedDeilverCount { get; set; }

        public short ActualDeilverCount { get; set; }

        /// <summary>
        /// 0：未完成 1.已完成 2.强制完成（存在无法投递的职位）
        /// </summary>
        public short Status { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? CompleteTime { get; set; }

        public short Priority { get; set; }
    }
}