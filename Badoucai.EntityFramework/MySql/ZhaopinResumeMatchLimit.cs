using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_Resume_Match_Limit")]
    public class ZhaopinResumeMatchLimit
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CompanyId { get; set; }

        public int DailySearchCount { get; set; }

        public int DailyWatchCount { get; set; }
    }
}