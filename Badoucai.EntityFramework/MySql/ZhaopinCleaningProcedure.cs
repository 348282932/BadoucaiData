using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_CleaningProcedure")]
    public class ZhaopinCleaningProcedure
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public short Id { get; set; }

        public string Account { get; set; }

        public string Password { get; set; }

        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        public bool IsOnline { get; set; } = true;

        public bool IsEnable { get; set; } = true;

        public string Cookie { get; set; }

        public int TodayWatch { get; set; }
    }
}