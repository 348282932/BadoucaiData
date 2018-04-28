using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_ResumeTemp")]
    public class ZhaopinResumeTemp
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ResumeId { get; set; }

        public string ResumeNumber { get; set; }
    }
}