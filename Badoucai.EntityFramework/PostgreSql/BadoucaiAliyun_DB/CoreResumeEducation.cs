using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB
{
    [Table("Core_Resume_Education", Schema = "public")]
    public class CoreResumeEducation
    {
        [Key, Column(Order = 0)]
        public short Id { get; set; }

        [Key, Column(Order = 1)]
        public string ResumeId { get; set; }

        public string School { get; set; }

        public string Degree { get; set; }

        public string Major { get; set; }

    }
}