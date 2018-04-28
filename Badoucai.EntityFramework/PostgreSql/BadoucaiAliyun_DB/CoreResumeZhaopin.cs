using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB
{

    [Table("Core_Resume_Zhaopin", Schema = "public")]
    public class CoreResumeZhaopin
    {
        [Key]
        public int Id { get; set; }

        public string ResumeKey { get; set; }

        public string Type { get; set; }

        public string Cellphone { get; set; }

        public string Email { get; set; }

        public bool IsMatched { get; set; } = false;

        public DateTime? MatchedTime { get; set; }
    }
    
}