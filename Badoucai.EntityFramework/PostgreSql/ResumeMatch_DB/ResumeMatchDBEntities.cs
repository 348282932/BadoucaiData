using System.Data.Entity;

namespace Badoucai.EntityFramework.PostgreSql.ResumeMatch_DB
{
    public class ResumeMatchDBEntities : DbContext
    {
        public ResumeMatchDBEntities()
            : base("name=ResumeMatchDBEntities")
        {
        }

        public virtual DbSet<OldResumeSummary> OldResumeSummary { get; set; }
    }
}