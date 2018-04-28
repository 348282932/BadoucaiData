using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_DeilverUserResume")]
    public class ZhaopinDeilverUserResume
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public string ResumeNumber { get; set; }
    }
}