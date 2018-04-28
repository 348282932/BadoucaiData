using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_IncompleteResume")]
    public class ZhaopinIncompleteResume
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string ResumeNumber { get; set; }

        public string Cellphone { get; set; }

        public string Email { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime CompletionTime { get; set; }
    }
}