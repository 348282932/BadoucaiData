using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB
{
    [Table("Core_Resume_Work", Schema = "public")]
    public class CoreResumeWork
    {
        [Key, Column(Order = 0)]
        public short Id { get; set; }

        public DateTime Begin { get; set; }

        [Key, Column(Order = 1)]
        public string ResumeId { get; set; }

        public string Company { get; set; }
    }
}