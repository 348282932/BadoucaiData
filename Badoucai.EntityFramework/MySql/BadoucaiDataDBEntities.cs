using System.Data.Entity;

namespace Badoucai.EntityFramework.MySql
{
    public class BadoucaiDataDBEntities : DbContext
    {
        public BadoucaiDataDBEntities()
            : base("name=BadoucaiDataDBEntities")
        {

        }

        public virtual DbSet<Resume> Resume { get; set; }
    }
}