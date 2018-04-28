using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Dodi_UserInfomation")]
    public class DodiUserInfomation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public int BusinessId { get; set; }
        public string UserName { get; set; }
        public string Birthday { get; set; }
        public string GraduationYear { get; set; }
        public string ProfessionalTitle { get; set; }
        public string GraduatedSchool { get; set; }
        public string Gender { get; set; }
        public string Residence { get; set; }
        public string Identity { get; set; }
        public string Email { get; set; }
        public string Cellphone { get; set; }
        public string QQ { get; set; }
        public string Education { get; set; }
        public string Other { get; set; }
        public string JobName { get; set; }
        public bool IsPost { get; set; } = false;
    }
}