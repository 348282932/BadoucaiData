using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("XSS_Zhaopin_Company")]
    public class ZhaopinCompany
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }

        public string Name { get; set; }

        public string Number { get; set; }

        public string Address { get; set; }

        public DateTime UpdateTime { get; set; }

        public string Source { get; set; } = "SEARCH";

        public int? Balance { get; set; }

        public int? City { get; set; }

        public string Telephone { get; set; }

        public string Email { get; set; }

        public string Contactor { get; set; }

        public string Cellphone { get; set; }

    }
}