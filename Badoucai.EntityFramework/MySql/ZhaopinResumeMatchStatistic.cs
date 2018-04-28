using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_Resume_Match_Statistic")]
    public class ZhaopinResumeMatchStatistic
    {
        [Key]
        [Column(Order = 2)]
        public string Date { get; set; }

        public int SearchCount { get; set; }

        public int WatchCount { get; set; }

        public int MatchedCount { get; set; }

        [Key]
        [Column(Order = 1)]
        public int CompanyId { get; set; }
    }
}