using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_ResumeNumber")]
    public class ZhaopinResumeNumber
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string ResumeNumber { get; set; }
    }
}