using System.Data.Entity;

namespace Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB
{
    public class BadoucaiAliyunDBEntities : DbContext
    {
        public BadoucaiAliyunDBEntities()
            : base("name=BadoucaiAliyunDBEntities")
        {

        }

        public virtual DbSet<CoreResumeSummary> CoreResumeSummary { get; set; }
        public virtual DbSet<CoreResumeReferenceMapping> CoreResumeReferenceMapping { get; set; }
        public virtual DbSet<CoreResumeReference> CoreResumeReference { get; set; }
        public virtual DbSet<CoreResumeZhaopin> CoreResumeZhaopin { get; set; }
        public virtual DbSet<CoreResumeEducation> CoreResumeEducation { get; set; }
        public virtual DbSet<CoreResumeWork> CoreResumeWork { get; set; }
        public virtual DbSet<CoreResumeIntention> CoreResumeIntention { get; set; }

    }
}