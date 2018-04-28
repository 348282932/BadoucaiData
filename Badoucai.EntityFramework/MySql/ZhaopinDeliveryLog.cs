using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_DeliveryLog")]
    public class ZhaopinDeliveryLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long DeliveryId { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        public int CompanyId { get; set; }
    }
}