using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_TargetCompany")]
    public class ZhaopinTargetCompany
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public DateTime? ExpireDate { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        public bool IsUse { get; set; } = false;
    }
}