using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{
    [Table("Resume")]
    public class Resume
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Gender { get; set; }
        public DateTime Birthday { get; set; }
        public string MaritalStatus { get; set; }
        public DateTime? WorkStarts { get; set; }
        public string DegreeText { get; set; }
        public short DegreeValue { get; set; }
        public string CurrentResidenceText { get; set; }
        public int CurrentResidenceValue { get; set; }
        public string RegisteredResidencText { get; set; }
        public int RegisteredResidencValue { get; set; }
        public string Cellphone { get; set; }
        public string Email { get; set; }
        public string DesiredCityText { get; set; }
        public string DesiredCityValue { get; set; }
        public int DesiredSalaryScopeMin { get; set; }
        public int DesiredSalaryScopeMax { get; set; }
        public string CurrentCareerStatusText { get; set; }
        public int CurrentCareerStatusValue { get; set; }
        public string DesiredPositionText { get; set; }
        public string DesiredPositionValue { get; set; }
        public string DesiredIndustryText { get; set; }
        public string DesiredIndustryValue { get; set; }
        public string HistoryJobTitle { get; set; }
        public int LastJobSalaryScopeMin { get; set; }
        public int LastJobSalaryScopeMax { get; set; }
        public DateTime RefreshDate { get; set; }
    }
}