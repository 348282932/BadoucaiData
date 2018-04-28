using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB
{
    [Table("Core_Resume_Download", Schema = "public")]
    public class CoreResumeDownload
    {
        public string Id { get; set; }

        public string ResumeNumber { get; set; }
    }
}