using System.Data.Entity;

namespace Badoucai.EntityFramework.MySql
{
    public class MangningXssDBEntities : DbContext
    {
        public MangningXssDBEntities()
            : base("name=MangningXssDBEntities")
        {
            
        }

        public virtual DbSet<ZhaopinCompany> ZhaoPinCompany { get; set; }

        public virtual DbSet<ZhaopinPosition> ZhaopinPosition { get; set; }

        public virtual DbSet<ZhaopinDeilverTask> ZhaopinDeilverTask { get; set; }

        public virtual DbSet<ZhaopinDeilverUserResume> ZhaopinDeilverUserResume { get; set; }

        public virtual DbSet<ZhaopinDeilveryRecord> ZhaopinDeilveryRecord { get; set; }

        public virtual DbSet<ZhaopinUser> ZhaopinUser { get; set; }

        public virtual DbSet<ZhaopinResume> ZhaopinResume { get; set; }

        public virtual DbSet<ZhaopinStaff> ZhaopinStaff { get; set; }

        public virtual DbSet<ZhaopinTargetCompany> ZhaopinTargetCompany { get; set; }

        public virtual DbSet<DodiUserInfomation> DodiUserInfomation { get; set; }

        public virtual DbSet<DodiBusiness> DodiBusiness { get; set; }

        public virtual DbSet<ZhaopinDelivery> ZhaopinDelivery { get; set; }

        public virtual DbSet<ZhaopinDeliveryLog> ZhaopinDeliveryLog { get; set; }

        public virtual DbSet<ZhaopinResumeTemp> ZhaopinResumeTemp { get; set; }

        public virtual DbSet<ZhaopinMatchedResume> ZhaopinMatchedResume { get; set; }

        public virtual DbSet<ZhaopinWatchedResume> ZhaopinWatchedResume { get; set; }

        public virtual DbSet<ZhaopinResumeMatchLimit> ZhaopinResumeMatchLimit { get; set; }

        public virtual DbSet<ZhaopinResumeMatchStatistic> ZhaopinResumeMatchStatistic { get; set; }

        public virtual DbSet<ZhaopinMatchedCache> ZhaopinMatchedCache { get; set; }

        public virtual DbSet<ZhaopinCheckCode> ZhaopinCheckCode { get; set; }

        public virtual DbSet<ZhaopinCleaningProcedure> ZhaopinCleaningProcedure { get; set; }

        public virtual DbSet<ZhaopinSearchCondition> ZhaopinSearchCondition { get; set; }

        public virtual DbSet<ZhaopinResumeNumber> ZhaopinResumeNumber { get; set; }

        public virtual DbSet<ZhaopinResumeUploadLog> ZhaopinResumeUploadLog { get; set; }

        public virtual DbSet<ZhaopinIncompleteResume> ZhaopinIncompleteResume { get; set; }
    }
}