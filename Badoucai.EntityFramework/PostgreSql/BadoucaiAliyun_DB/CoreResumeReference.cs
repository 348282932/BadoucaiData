using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB
{
    [Table("Core_Resume_Reference", Schema = "public")]
    public class CoreResumeReference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; }

        public string ResumeId { get; set; }

        public string Source { get; set; }
    }
}