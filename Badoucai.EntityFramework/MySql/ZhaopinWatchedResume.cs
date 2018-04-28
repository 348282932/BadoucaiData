using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_Watched_Resume")]
    public class ZhaopinWatchedResume
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string ResumeNumber { get; set; }

        public DateTime? HackTime { get; set; }

        public DateTime? WatchTime { get; set; }

        public int CompanyId { get; set; }
    }
}