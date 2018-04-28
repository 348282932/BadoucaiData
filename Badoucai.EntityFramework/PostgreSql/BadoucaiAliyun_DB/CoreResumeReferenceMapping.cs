using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB
{
    [Table("Core_Resume_Reference_Mapping", Schema = "public")]
    public class CoreResumeReferenceMapping
    {
        [Key]
        public string Id { get; set; }

        public string Source { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }
    }
}