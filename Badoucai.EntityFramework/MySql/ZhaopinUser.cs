using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_User")]
    public class ZhaopinUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Cookie { get; set; }

        public DateTime UpdateTime { get; set; }

        public string Source { get; set; }

        public string Status { get; set; }

        public string Name { get; set; }

        public string Cellphone { get; set; }

        public string Email { get; set; }

        public string Referer { get; set; }

        public DateTime ModifyTime { get; set; }

        public DateTime? CreateTime { get; set; }


    }
}