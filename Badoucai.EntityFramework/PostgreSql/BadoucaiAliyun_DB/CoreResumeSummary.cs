using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB
{
    [Table("Core_Resume_Summary", Schema = "public")]
    public class CoreResumeSummary
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; }

        public int CurrentResidence { get; set; }

        public long Cellphone { get; set; }

        public string Email { get; set; }

        public DateTime UpdateTime { get; set; }

        public int RegisteredResidenc { get; set; }

        public string Gender { get; set; }

        public DateTime Birthday { get; set; }

        public DateTime WorkStarts { get; set; }

        public string Degree { get; set; }

        public string Name { get; set; }

    }
}