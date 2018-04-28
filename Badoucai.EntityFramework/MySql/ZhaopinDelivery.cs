using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_Delivery")]
    public class ZhaopinDelivery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }

        public int CompanyId { get; set; }

        public int ResumeId { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        public string JobNumber { get; set; }

        public string ResumeNumber { get; set; }

        public string JobName { get; set; }
    }
}