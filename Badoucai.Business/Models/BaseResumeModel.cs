namespace Badoucai.Business.Model
{
    public class BaseResumeModel
    {
        public UserInformation UserInformation { get; set; }
        public string WorkDescription { get; set; }
        public string WorkDateStartYear { get; set; }
        public string WorkDateStartMonth { get; set; }
        public string WorkDateEndYear { get; set; }
        public string WorkDateEndMonth { get; set; }
        public string SubJobType { get; set; }
        public string SchoolName { get; set; }
        public string Salary { get; set; }
        public string MajorType { get; set; }
        public string MajorSubType { get; set; }
        public string MajorName { get; set; }
        public string JobType { get; set; }
        public string JobTitle { get; set; }
        public string Industry { get; set; }
        public string EduDateStartYear { get; set; }
        public string EduDateStartMonth { get; set; }
        public string EduDateEndYear { get; set; }
        public string EduDateEndMonth { get; set; }
        public string EducationLevel { get; set; }
        public string DesiredSalaryScope { get; set; }
        public string DesiredJobType { get; set; }
        public string DesiredIndustry { get; set; }
        public string DesiredEmploymentType { get; set; }
        public string DesiredCity { get; set; }
        public string DesiredJobTypeId { get; set; }
        public string DesiredIndustrySuper { get; set; }
        public string CompanyName { get; set; }
        public string CompanyIndustry { get; set; }
    }

    public class UserInformation
    {
        public string UserName { get; set; }

        public string Residence_P { get; set; }

        public string Residence { get; set; }

        public string Hukou_P { get; set; }

        public string Hukou { get; set; }

        public string Gender { get; set; }

        public string ExperienceMonth { get; set; }

        public string ExperienceYear { get; set; }

        public string Email { get; set; }

        private string _cellphone;

        public string Cellphone { get{ return _cellphone.Remove(3, 4).Insert(3, "****"); } set { _cellphone = value; }}

        public string BirthDateYear { get; set; }

        public string BirthDateMonth { get; set; }
    }
}