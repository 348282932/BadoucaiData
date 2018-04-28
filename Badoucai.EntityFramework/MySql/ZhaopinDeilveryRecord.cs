using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_DeliveryRecord")]
    public class ZhaopinDeilveryRecord
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int CompanyId { get; set; }

        public int PositionId { get; set; }

        public DateTime CreateTime { get; set; }
    }
}