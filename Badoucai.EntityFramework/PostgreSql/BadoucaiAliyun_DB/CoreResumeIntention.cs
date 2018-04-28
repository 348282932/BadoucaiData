using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB
{
    [Table("Core_Resume_Intention", Schema = "public")]
    public class CoreResumeIntention
    {
        [Key]
        public string ResumeId { get; set; }

        public decimal MinimumSalary { get; set; }

        public decimal MaximumSalary { get; set; }
    }
}