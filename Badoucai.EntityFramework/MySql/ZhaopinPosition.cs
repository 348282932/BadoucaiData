using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_ZhaoPin_Position")]
    public class ZhaopinPosition
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string Number { get; set; }

        public int CompanyId { get; set; }

        public string Salary { get; set; }

        public string WorkPlace { get; set; }

        public string ReleaseTime { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        public bool IsEnable { get; set; } = true;
    }
}