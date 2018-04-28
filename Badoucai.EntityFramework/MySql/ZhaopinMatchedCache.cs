using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{

    [Table("XSS_Zhaopin_MatchedCache")]
    public class ZhaopinMatchedCache
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ResumeId { get; set; }

        public string ResumeNumber { get; set; }

        public string Name { get; set; }

        public int UserId { get; set; }

        public string Path { get; set; }

        public DateTime ModifyTime { get; set; }

        public string Cellphone { get; set; }

        public string Email { get; set; }

        public string UserExtId { get; set; }

        public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    }
}