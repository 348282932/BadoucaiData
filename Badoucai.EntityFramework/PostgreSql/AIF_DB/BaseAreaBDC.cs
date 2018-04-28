using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.PostgreSql.AIF_DB
{
    [Table("Base_Area_BDC", Schema = "public")]
    public class BaseAreaBDC
    {
        [Key]
        public int Id { get; set; }

        public int PId { get; set; }

        public string Name { get; set; }
    }
}