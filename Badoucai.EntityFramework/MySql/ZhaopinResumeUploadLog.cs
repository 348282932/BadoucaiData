using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_ResumeUploadLog")]
    public class ZhaopinResumeUploadLog
    {
        [Key]
        public int Id { get; set; }

        public int ResumeId { get; set; }

        public string ReturnCode { get; set; }

        public string Tag { get; set; }

        public DateTime UploadTime { get; set; }
    }
}