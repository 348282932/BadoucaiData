using System.Data.Entity;

namespace Badoucai.EntityFramework.PostgreSql.Crawler_DB
{
    public class BadoucaiDBEntities : DbContext
    {
        public BadoucaiDBEntities()
            : base("name=BadoucaiDBEntities")
        {

        }

        public virtual DbSet<SpiderResumeDownload> SpiderResumeDownload { get; set; }

    }
}