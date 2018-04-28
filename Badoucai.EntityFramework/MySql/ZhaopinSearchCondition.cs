using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{   
    [Table("XSS_Zhaopin_SearchCondition")]
    public class ZhaopinSearchCondition
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public short Gender { get; set; }

        public short Age { get; set; }

        public short Degrees { get; set; }

        public short WorkYears { get; set; }

        public short Status { get; set; }

        public int Quantity { get; set; }

        public DateTime? LastSearchDate { get; set; }

        public string HandlerAccuont { get; set; }

        public int LastWatchPage { get; set; } = 1;
    }
}