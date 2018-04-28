using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.Crawler_DB
{
    [Table("Splider_Resume_Download", Schema = "public")]
    public class SpiderResumeDownload
    {
        [Key]
        public string ResumeNumber { get; set; }

        public long? Cellphone { get; set; }

        public string Email { get; set; }

        public string SavePath { get; set; }

        public short Weight { get; set; }

        public DateTime UpdateTime { get; set; }

        public short Status { get; set; }
    }
}