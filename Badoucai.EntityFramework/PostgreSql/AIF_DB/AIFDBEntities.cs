using System.Data.Entity;

namespace Badoucai.EntityFramework.PostgreSql.AIF_DB
{
    public class AIFDBEntities : DbContext
    {
        public AIFDBEntities()
            : base("name=AIFDBEntities")
        {

        }

        public virtual DbSet<BaseAreaBDC> BaseAreaBDC { get; set; }
    }
}