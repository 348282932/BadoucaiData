using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_Staff")]
    public class ZhaopinStaff
    {
        [Key]
        public int Id { get; set; }

        public int CompanyId { get; set; }

        public string Cookie { get; set; }

        public DateTime? UpdateTime { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Source { get; set; }
    }
}