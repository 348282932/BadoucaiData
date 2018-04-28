using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Badoucai.Business.Model;
using Badoucai.Library;
using Newtonsoft.Json;

namespace Badoucai.Business.Zhaopin
{
    public static class Format
    {
        /// <summary>
        /// 解析智联为八斗才格式简历
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Resume Convert_V0(SourceResume source)
        {
            if (source != null)
            {
                var tag = new Resume
                {
                    Id = source.BdcId,
                    Name = source.Name,
                    Gender = FormatGender(source.Gender, "source-value", "").ToCharArray().First(),
                    Birthday = Convert.ToDateTime(source.Birthday)
                };
                var workYear = Regex.Match(source.WorkStarts, "^[0-9]*").Value;
                tag.WorkStarts = Convert.ToDateTime(DateTime.Now.Year-Convert.ToInt32(workYear)+"-01-01");
                tag.Cellphone = string.IsNullOrWhiteSpace(source.Cellphone)?0:Convert.ToInt64(source.Cellphone.ToDBC());
                tag.Email = source.Email;
                tag.CurrentResidence = int.Parse(FormatCurrentResidence(source.CurrentResidence, "source-value","").Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                tag.RegisteredResidenc = int.Parse(FormatCurrentResidence(source.RegisteredResidenc, "source-value","").Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                tag.Degree = FormatDegree(source.Degree, "source-value", "").ToCharArray().First();
                tag.Major = source.Major;
                tag.MaritalStatus = FormatMaritalStatus(source.MaritalStatus, "source-value", "").ToCharArray().First();
                tag.JobStatus = FormatJobStatus(source.JobStatus, "source-value", "").ToCharArray().First();
                tag.UpdateTime = Convert.ToDateTime(source.UpdateTime);

                tag.Intention = FormatIntention(source.Intention, "source-value", "");
                tag.Intention.ResumeId = source.BdcId;
                tag.Works = FormatWorks(source.Works, "source-value", source.BdcId, "");
                tag.Projects = FormatProjects(source.Projects, source.BdcId);
                tag.Educations = FormatEducations(source.Educations, "source-value", source.BdcId, "");
                tag.Trainings = FormatTrainings(source.Trainings, source.BdcId);
                tag.Reference = new Reference { HasContract = true, Id = source.ResumeId, ResumeId = source.BdcId, Tag = "O", Source = "ZHAOPIN", Template = "", UpdateTime = Convert.ToDateTime(source.UpdateTime) };
                var mappings = new List<ReferenceMapping> { new ReferenceMapping { Id = source.ResumeId, Key = "ResumeNumber", Value = source.ResumeNumber, Source = "ZHAOPIN" } };
                if (!string.IsNullOrWhiteSpace(source.UserMasterExtId))
                    mappings.Add(new ReferenceMapping { Id = source.ResumeId, Key = "UserMasterExtId", Value = source.UserMasterExtId, Source = "ZHAOPIN" });
                if (!string.IsNullOrWhiteSpace(source.UserMasterId))
                    mappings.Add(new ReferenceMapping { Id = source.ResumeId, Key = "UserMasterId", Value = source.UserMasterId, Source = "ZHAOPIN" });
                tag.Reference.Mapping = mappings;

                return tag;
            }

            return null;
        }

        /// <summary>
        /// 解析前程为八斗才格式
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Resume Convert_V1(SourceResume source)
        {
            if (source != null)
            {
                var tag = new Resume();
                tag.Id = source.BdcId;
                tag.Name = source.Name;
                tag.Gender = FormatGender(source.Gender, "source-value", "51").ToCharArray().First();
                tag.Birthday = Convert.ToDateTime(source.Birthday);
                tag.Cellphone = Convert.ToInt64(source.Cellphone);
                tag.Email = source.Email;
                tag.CurrentResidence =int.Parse(FormatCurrentResidence(source.CurrentResidence, "source-value", "51").Replace("0x",""), System.Globalization.NumberStyles.HexNumber);
                tag.RegisteredResidenc = int.Parse(FormatCurrentResidence(source.RegisteredResidenc, "source-key", "51").Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                tag.Degree = FormatDegree(source.Degree, "source-value", "51").ToCharArray().First();
                tag.Major = source.Major;
                tag.MaritalStatus = FormatMaritalStatus(source.MaritalStatus, "source-value", "51").ToCharArray().First();
                tag.JobStatus = FormatJobStatus(source.JobStatus, "source-value", "51").ToCharArray().First();
                tag.UpdateTime = Convert.ToDateTime(source.UpdateTime);

                tag.Intention = FormatIntention(source.Intention, "source-value", "51");
                tag.Intention.ResumeId = source.BdcId;
                tag.Works = FormatWorks(source.Works, "source-value", source.BdcId, "51");
                tag.Projects = FormatProjects(source.Projects, source.BdcId);
                tag.Educations = FormatEducations(source.Educations, "source-value", source.BdcId, "51");
                tag.Trainings = FormatTrainings(source.Trainings, source.BdcId);
                tag.Reference=new Reference {HasContract = true,Id = source.ResumeId,ResumeId = source.BdcId,Tag = "O",Source = "51JOB",Template = "JSON.2017.1",UpdateTime = Convert.ToDateTime(source.UpdateTime)};
                if(tag.Works != null && tag.Works.Count > 0)
                {
                    tag.WorkStarts = tag.Works.Min(a => a.Begin);
                }
                else
                {
                    tag.WorkStarts = DateTime.Now;
                }

                return tag;
            }

            return null;
        }

        /// <summary>
        /// 解析为前程格式Json对象
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static dynamic ConvertToZhaopin(SourceResume source)
        {
            return new
            {
                Flag = -1,
                detialJSonStr = new
                {
                    ResumeId = Convert.ToInt32(source.ResumeId),
                    UserMasterId = Convert.ToInt32(source.UserMasterId),
                    WorkYearsRangeId = Convert.ToInt32(Regex.Match(source.WorkStarts,"(\\d+)").ResultOrDefault("$1","0")),
                    DesiredSalaryScope = source.Intention.Salary.Contains("不显示") || string.IsNullOrWhiteSpace(source.Intention.Salary) ? 0 : Convert.ToInt64(source.Intention.Salary.Replace("-","").Replace("元/月", "").Replace("以下","").Replace("以上", "")),
                    UserMasterName = source.Name,
                    source.Gender,
                    source.MaritalStatus,
                    BirthYear = source.Birthday,
                    //Name = source.ResumeName,
                    source.UserMasterExtId,
                    source.ResumeNumber,
                    CurrentEducationLevel = source.Degree,
                    DateLastReleased = new DateTime(1970, 1, 1),
                    DateLastViewed = new DateTime(1970, 1, 1),
                    DateModified = new DateTime(1970, 1, 1),
                    DateCreated = new DateTime(1970, 1, 1),
                    DesiredPosition = new[]
                    {
                        new
                        {
                            DesiredEmploymentType =  source.Intention.JobType,
                            DesiredSalaryScope = source.Intention.Salary,
                            DesiredIndustry = source.Intention.Industry,
                            DesiredJobType = source.Intention.Function,
                            DesiredCity = source.Intention.Location
                        }
                    },
                    WorkExperience = source.Works?.Select(s => new
                    {
                        DateStart = s.Begin,
                        DateEnd = s.End,
                        CompanyName = s.Company,
                        CompanyIndustry = s.Industry,
                        CompanySize = s.Size,
                        CompanyProperty = s.Nature,
                        ResideDepartment = s.Department,
                        JobTitle = s.Position,
                        WorkDescription = s.Description
                    }),
                    ProjectExperience = source.Projects?.Select(s=> new
                    {
                        DateStart = s.Begin,
                        DateEnd = s.End,
                        ProjectName = s.Name,
                        ProjectDescription = s.Description,
                        ProjectResponsibility = s.Duty
                    }),
                    EducationExperience = source.Educations?.Select(s=> new
                    {
                        DateStart = s.Begin,
                        DateEnd = s.End,
                        SchoolName = s.School,
                        EducationLevel = s.Degree,
                        MajorName = s.Major
                    }),
                    Training = source.Trainings?.Select(s=> new
                    {
                        DateStart = s.Begin,
                        DateEnd = s.End,
                        s.Course,
                        s.Institution,
                        TrainingDescription = s.Description,
                        s.Location
                    }),
                    ProfessionnalSkill = source.Skills?.Where(w=>!w.Level.Contains("听说") && !w.Level.Contains("读写")).Select(s=> new
                    {
                        SkillName= s.Name,
                        MasterDegree = s.Level.Split("|")[0].Trim(),
                        UsedMonths = s.Level.Split("|")[1].Replace("个月","").Trim()
                    }),
                    LanguageSkill = source.Skills?.Where(w => w.Level.Contains("听说") || w.Level.Contains("读写")).Select(s=> new
                    {
                        LanguageName = s.Name,
                        HearSpeakSkill = s.Level.Contains("听说") ? (s.Level.Split("|").Length > 1 ? s.Level.Split("|")[1].LanguageSkillReplace("听说能力", "一般").Trim() : s.Level.Split("|")[0].LanguageSkillReplace("听说能力", "一般").Trim()) : "",
                        ReadWriteSkill = s.Level.Contains("读写") ? s.Level.Split("|")[0].LanguageSkillReplace("读写能力", "一般").Trim() : ""
                    }),
                    AchieveCertificate = source.Certificates?.Select(s=> new
                    {
                        AchieveDate = s.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                        CertificateName = s.CertName,
                        CertificateDescription = s.Description
                    }),
                    AchieveAward = source.Honors?.Select(s=> new
                    {
                        AchieveDate = s.Time,
                        AwardName = s.Honor,
                        s.Description
                    }),
                    PracticeExperience = source.Practices?.Select(s=> new
                    {
                        PracticeName = s.Name,
                        DateStart = s.Begin,
                        DateEnd = s.End,
                        PracticeDescription = s.Description
                    }),
                    Other = source.Others?.Select(s => new 
                    {
                        Name = s.Title,
                        s.Description
                    })
                },

                resumeId = source.ResumeId,
                resumeNo = source.ResumeNumber,

                userDetials = new
                {
                    userName = source.Name,
                    gender = source.Gender,
                    maritalStatus = source.MaritalStatus,
                    mobilePhone = source.Cellphone,
                    email = source.Email,
                    birthStr = source.Birthday,
                    cityId = source.CurrentResidence,
                    hUKOUCityId = source.RegisteredResidenc,
                    userMasterId = source.UserMasterId
                }
            };
        }


        public static SourceResume ConvertTo_Dtl_V5(string content)
        {
            var dto = new SourceResume();
            var source = JsonConvert.DeserializeObject<dynamic>(content);
            dto.Id = source.resumeNo;
            var Dtl = JsonConvert.DeserializeObject<dynamic>(source.detialJSonStr.ToString());
            if (source.userDetials != null)
            {
                dto.Name = source.userDetials.userName;
                dto.Gender = source.userDetials.gender;
                dto.MaritalStatus = source.userDetials.maritalStatus;
                dto.Cellphone = source.userDetials.mobilePhone;
                dto.Email = source.userDetials.email;
                dto.Birthday = source.userDetials.birthStr;
                dto.CurrentResidence = source.userDetials.cityId;
                dto.RegisteredResidenc = source.userDetials.hUKOUCityId;
            }
            else
            {
                dto.Name = Dtl.UserMasterName;
                dto.Gender = Dtl.Gender;
                dto.MaritalStatus = Dtl.MaritalStatus;
                dto.Birthday = Dtl.BirthYear;
            }
            dto.ResumeId = Dtl.ResumeId;
            dto.ResumeName = Dtl.Name;
            dto.UserMasterId = Dtl.UserMasterId;
            dto.UserMasterExtId = Dtl.UserMasterExtId;
            dto.ResumeNumber = Dtl.ResumeNumber;
            dto.WorkStarts = Dtl.WorkYearsRangeId;
            dto.Degree = Dtl.CurrentEducationLevel;

            dto.JobStatus = Dtl.DesiredPosition[0].CurrentCareerStatus;
            dto.Intention = new ResumeIntention();
            if (Dtl.DesiredPosition != null && Dtl.DesiredPosition.Count > 0)
            {
                dto.Intention.JobType = Dtl.DesiredPosition[0].DesiredEmploymentType;
                dto.Intention.Salary = Dtl.DesiredPosition[0].DesiredSalaryScope;
                dto.Intention.Industry = Dtl.DesiredPosition[0].DesiredIndustry;
                dto.Intention.Function = Dtl.DesiredPosition[0].DesiredJobType;
                dto.Intention.Location = Dtl.DesiredPosition[0].DesiredCity;
            }
            else
            {
                dto.Intention.JobType = "默认";
                dto.Intention.Salary = "面议";
                dto.Intention.Industry = "默认";
                dto.Intention.Function = "中国";
                dto.Intention.Location = "中国";
            }

            if (Dtl.SelfEvaluate != null && Dtl.SelfEvaluate.Count > 0)
                dto.Intention.Evaluation = Dtl.SelfEvaluate[0]?.CommentContent;
            else
                dto.Intention.Evaluation = "";

            #region 工作经验

            if (Dtl.WorkExperience != null && Dtl.WorkExperience.Count > 0)
            {
                dto.Works = new List<ResumeWork>();
                foreach (var o in Dtl.WorkExperience)
                {
                    var work = new ResumeWork();
                    work.Begin = o.DateStart;
                    work.End = o.DateEnd;
                    work.Company = o.CompanyName;
                    work.Industry = o.CompanyIndustry;
                    work.Size = o.CompanySize;
                    work.Nature = o.CompanyProperty;
                    work.Department = o.ResideDepartment;
                    work.Position = o.JobTitle;
                    work.Description = o.WorkDescription;
                    dto.Works.Add(work);
                }
            }

            #endregion

            #region 项目经验

            if (Dtl.ProjectExperience != null && Dtl.ProjectExperience.Count > 0)
            {
                dto.Projects = new List<ResumeProject>();
                foreach (var o in Dtl.ProjectExperience)
                {
                    var project = new ResumeProject();
                    project.Begin = o.DateStart;
                    project.End = o.DateEnd;
                    project.Company = "";
                    project.Name = o.ProjectName;
                    project.Description = o.ProjectDescription;
                    project.Duty = o.ProjectResponsibility;
                    dto.Projects.Add(project);
                }
            }

            #endregion

            #region 教育经历

            if (Dtl.EducationExperience != null && Dtl.EducationExperience.Count > 0)
            {
                dto.Educations = new List<ResumeEducation>();
                foreach (var o in Dtl.EducationExperience)
                {
                    var edu = new ResumeEducation();
                    edu.Begin = o.DateStart;
                    edu.End = o.DateEnd;
                    edu.School = o.SchoolName;
                    edu.Degree = o.EducationLevel;
                    edu.Description = "";
                    edu.Major = o.MajorName;
                    dto.Educations.Add(edu);
                }
            }

            #endregion

            #region 培训经历

            if (Dtl.Training != null && Dtl.Training.Count > 0)
            {
                dto.Trainings = new List<ResumeTraining>();
                foreach (var o in Dtl.Training)
                {
                    var train = new ResumeTraining();
                    train.Begin = o.DateStart;
                    train.End = o.DateEnd;
                    train.Course = o.Course;
                    train.Institution = o.Institution;
                    train.Description = o.TrainingDescription;
                    train.Location = o.Location;
                    dto.Trainings.Add(train);
                }
            }

            #endregion

            #region 技能/语言
            if (Dtl.ProfessionnalSkill != null && Dtl.ProfessionnalSkill.Count > 0)
            {
                dto.Skills = new List<ResumeSkill>();
                foreach (var o in Dtl.ProfessionnalSkill)
                {
                    var skill = new ResumeSkill { Name = o.SkillName, Level = o.MasterDegree.ToString() + "|" + o.UsedMonths.ToString() + "个月", Description = "" };
                    dto.Skills.Add(skill);
                }

            }
            if (Dtl.LanguageSkill != null && Dtl.LanguageSkill.Count > 0)
            {
                if (dto.Skills == null)
                    dto.Skills = new List<ResumeSkill>();
                foreach (var o in Dtl.LanguageSkill)
                {
                    var skill = new ResumeSkill { Name = o.LanguageName, Level = "听说" + o.HearSpeakSkill.ToString() + "|读写" + o.ReadWriteSkill.ToString(), Description = "" };
                    dto.Skills.Add(skill);
                }
            }
            #endregion

            #region 证书

            if (Dtl.AchieveCertificate != null && Dtl.AchieveCertificate.Count > 0)
            {
                dto.Certificates = new List<ResumeCertificate>();
                foreach (var o in Dtl.AchieveCertificate)
                {
                    try
                    {
                        var cert = new ResumeCertificate { Time = Convert.ToDateTime(o.AchieveDate), CertName = o.CertificateName, Description = o.CertificateDescription };

                        dto.Certificates.Add(cert);
                    }
                    catch
                    {
                        // ignored
                    }
                }

            }
            #endregion

            #region 校内荣誉
            if (Dtl.AchieveAward != null && Dtl.AchieveAward.Count > 0)
            {
                dto.Honors = new List<SchoolHonor>();
                foreach (var o in Dtl.AchieveAward)
                {
                    try
                    {
                        var cert = new SchoolHonor { Time = o.AchieveDate, Honor = o.AwardName, Description = o.Description };

                        dto.Honors.Add(cert);
                    }
                    catch
                    {
                        // ignored
                    }
                }

            }
            #endregion

            #region 校内实践

            if (Dtl.PracticeExperience != null && Dtl.PracticeExperience.Count > 0)
            {
                dto.Practices = new List<SchoolPractice>();
                foreach (var o in Dtl.PracticeExperience)
                {
                    try
                    {
                        var cert = new SchoolPractice { Name = o.PracticeName, Begin = o.DateStart, End = o.DateEnd, Description = o.PracticeDescription };

                        dto.Practices.Add(cert);
                    }
                    catch
                    {
                        // ignored
                    }
                }

            }
            #endregion

            #region 其他

            if (Dtl.Other != null && Dtl.Other.Count > 0)
            {
                dto.Others = new List<OtherInfo>();
                foreach (var o in Dtl.Other)
                {
                    try
                    {
                        var cert = new OtherInfo { Title = o.Name, Description = o.Description };

                        dto.Others.Add(cert);
                    }
                    catch
                    {
                        // ignored
                    }
                }

            }
            #endregion
            return dto;
        }

        #region 子项解析

        private static List<Training> FormatTrainings(List<ResumeTraining> sources,string resumeid)
        {
            if (sources == null)
                return null;
            var trainings = new List<Training>();
            short index = 1;
            sources.ForEach(t =>
            {
                trainings.Add(new Training
                {
                    Id = index,
                    ResumeId = resumeid,
                    Begin = DateTime.Parse(t.Begin),
                    End = string.IsNullOrEmpty(t.End.Trim()) || t.End == "0" || t.End.IndexOf("至今", StringComparison.Ordinal) > -1 ? DateTime.MaxValue : DateTime.Parse(t.End),
                    Institution = t.Institution,
                    Location = t.Location,
                    Course = t.Course,
                    Description = t.Description
                });
                index++;
            });

            return trainings;
        }

        //tag xml文件名后缀字符
        private static List<Education> FormatEducations(List<ResumeEducation> sources, string attr, string resumeid,string tag)
        {
            if (sources == null)
                return null;
            var educations = new List<Education>();
            short index = 1;
            sources.ForEach(t =>
            {
                var document = new HtmlAgilityPack.HtmlDocument();

                var degree = "*";

                if (!string.IsNullOrEmpty(t.Degree))
                {
                    document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\Degree"+tag+".xml"));

                    var node = document.DocumentNode.SelectSingleNode("nodes[1]/node[@"+attr+" = '"+t.Degree+"']")?.Attributes["result-key"];
                    if (node != null)
                        degree = node.Value;
                }

                educations.Add(new Education
                {
                    Id = index,
                    ResumeId = resumeid,
                    Begin = DateTime.Parse(t.Begin),
                    End = string.IsNullOrEmpty(t.End.Trim()) || t.End == "0" || t.End.IndexOf("至今", StringComparison.Ordinal) > -1 ? DateTime.MaxValue : DateTime.Parse(t.End),
                    School = t.School,
                    Degree = degree.ToCharArray().First(),
                    Major = t.Major
                });
                index++;
            });

            return educations;
        }

        private static List<Project> FormatProjects(List<ResumeProject> sources, string resumeid)
        {
            if (sources == null)
                return null;

            var projects = new List<Project>();
            short index = 1;
            sources.ForEach(t =>
            {
                projects.Add(new Project
                {
                    Id = index,
                    ResumeId = resumeid,
                    Begin = DateTime.Parse(t.Begin),
                    End = string.IsNullOrEmpty(t.End.Trim()) || t.End == "0"||t.End.IndexOf("至今", StringComparison.Ordinal)>-1 ? DateTime.MaxValue : DateTime.Parse(t.End),
                    Name = t.Name,
                    Description = t.Description,
                    Duty = t.Duty,
                    Company = t.Company
                });
                index++;
            });

        return projects;
        }

        private static List<Work> FormatWorks(List<ResumeWork> sources,string attr,string resumeId,string tag)
        {
            if (sources == null)
                return null;

            var works = new List<Work>();
            short index = 1;
            sources.ForEach(t =>
            {
                var document = new HtmlAgilityPack.HtmlDocument();

                var str = t.Industry;

                var industry = "0x0";

                if (!string.IsNullOrEmpty(str))
                {
                    str = str.Split(',')[0];
                    str = str.Replace(" ", "");
                    document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\Industry" + tag + ".xml"));

                    industry = document.DocumentNode.SelectSingleNode("nodes[1]/node[@"+attr+" = '"+str+"']")?.Attributes["result-key"]?.Value;
                }
                industry = industry ?? "0x0";

                str = t.Nature;

                var nature = "*";

                if (!string.IsNullOrEmpty(str))
                {
                    document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\CompanyType" + tag + ".xml"));

                    var node = document.DocumentNode.SelectSingleNode("nodes[1]/node[@" + attr + " = '" + str + "']");

                    if (node != null)
                    {
                        nature = node.Attributes["result-key"].Value;
                    }
                }

                str = t.Size;

                var size = "*";

                if (!string.IsNullOrEmpty(str))
                {
                    document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\CompanySize" + tag + ".xml"));

                    var node = document.DocumentNode.SelectSingleNode("nodes[1]/node[@" + attr + " = '" + str + "']");
                    if (node != null)
                        size = node.Attributes["result-key"].Value;
                }

                works.Add(new Work
                {
                    ResumeId = resumeId,
                    Id = index,
                    Begin = DateTime.Parse(t.Begin),
                    End = string.IsNullOrEmpty(t.End.Trim()) || t.End == "0" || t.End.IndexOf("至今", StringComparison.Ordinal) > -1 ? DateTime.MaxValue : DateTime.Parse(t.End),
                    Company = t.Company,
                    Industry = short.Parse(industry.Replace("0x",""), System.Globalization.NumberStyles.HexNumber),
                    Size = size.ToCharArray().First(),
                    Nature = nature.ToCharArray().First(),
                    Department = t.Department,
                    Position = t.Position,
                    Description = t.Description
                });
                index++;
            });

            return works;
        }

        private static Intention FormatIntention(ResumeIntention intention,string attr,string taget)
        {
            var tag = new Intention();
            var document = new HtmlAgilityPack.HtmlDocument();
            
            if (string.IsNullOrWhiteSpace(intention.Salary))
            {
                tag.MinimumSalary = 0;
                tag.MinimumSalary = 0;
            }
            else if(intention.Salary.IndexOf("以上") > -1)
            {
                tag.MaximumSalary = 0;
                var match=Regex.Match(intention.Salary, "\\d*");
                if (match.Success)
                    tag.MinimumSalary = Convert.ToDecimal(match.Value);
            }
            else if (intention.Salary.IndexOf("以下") > -1)
            {
                tag.MinimumSalary = 0;
                var match = Regex.Match(intention.Salary, "\\d*");
                if (match.Success)
                    tag.MaximumSalary = Convert.ToDecimal(match.Value);
            }
            else if(intention.Salary.IndexOf('-') > -1)
            {
                tag.MinimumSalary = Convert.ToDecimal(intention.Salary.Split('-')[0]);
                var match = Regex.Match(intention.Salary.Split('-')[1], "\\d*");
                if (match.Success)
                    tag.MaximumSalary = Convert.ToDecimal(match.Value);

            }
            else
            {
                tag.MinimumSalary = 0;
                tag.MinimumSalary = 0;
            }

            document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\Area" + taget + ".xml"));

            if (!string.IsNullOrWhiteSpace(intention.Location))
            {
                intention.Location = Regex.Replace(intention.Location, "[\\s|\\-|，|、]{1}", ",");
                foreach (var item in intention.Location.Split(','))
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;
                    var node = document.DocumentNode.SelectSingleNode("nodes[1]/node[@" + attr + " = '" + item + "']");

                    if (node == null)
                    {
                        tag.Location += string.IsNullOrEmpty(intention.Location) ? "530000000" : ",530000000";
                    }
                    else
                    {
                        var local = string.IsNullOrEmpty(intention.Location) ? "" : node.Attributes["result-key"].Value;
                        if (!string.IsNullOrWhiteSpace(local))
                        {
                            tag.Location += "," + Convert.ToInt32(local, 16);
                        }
                    }
                }

                tag.Location = tag.Location?.Trim(',') ?? "530000000";
            }
            else
                tag.Location = "530000000";

            document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\Industry" + taget + ".xml"));
            if (!string.IsNullOrWhiteSpace(intention.Industry))
            {
                intention.Industry = Regex.Replace(intention.Industry, "[\\s|，|、]{1}", ",");
                foreach (var item in intention.Industry.Split(','))
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;
                    var subNode = document.DocumentNode.SelectSingleNode("nodes[1]/node[@" + attr + " = '" + item + "']");
                    if (subNode != null)
                    {
                        var sub = string.IsNullOrEmpty(intention.Industry) ? "" : subNode.Attributes["result-key"].Value;
                        if (!string.IsNullOrWhiteSpace(sub))
                            tag.Industry += "," + Convert.ToInt32(sub, 16);
                    }

                }
                tag.Industry = tag.Industry?.Trim(',') ?? "0";
            }
            else
                tag.Industry = "0";
            
            document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\Function" + taget + ".xml"));
            if (!string.IsNullOrWhiteSpace(intention.Function))
            {
                intention.Function = Regex.Replace(intention.Function, "[\\s|，|、]{1}", ",");
                foreach (var item in intention.Function.Split(','))
                {
                    if (string.IsNullOrWhiteSpace(item))
                        continue;
                    var subnode = document.DocumentNode.SelectSingleNode("nodes[1]/node[@" + attr + " = '" + item + "']");
                    if (subnode == null)
                        continue;
                    var sub = string.IsNullOrEmpty(intention.Function) ? "" : subnode.Attributes["result-key"].Value;
                    if (!string.IsNullOrWhiteSpace(sub))
                        tag.Function += "," + Convert.ToInt32(sub, 16);
                }

                tag.Function = tag.Function?.Trim(',') ?? "0";
            }
            else
                tag.Function = "0";

            document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\PresentStatus" + taget + ".xml"));

            var nodeDutyTime = document.DocumentNode.SelectSingleNode("nodes[1]/node[@" + attr + " = '" + intention.DutyTime + "']")?.Attributes["result-key"];
            if (!string.IsNullOrWhiteSpace(nodeDutyTime?.Value))
            {
                tag.DutyTime = nodeDutyTime.Value.ToCharArray().First();
            }
            else
            {
                tag.DutyTime = '*';
            }

            document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\PositionType" + taget + ".xml"));
            if (!string.IsNullOrWhiteSpace(intention.JobType))
            {
                intention.JobType= Regex.Replace(intention.JobType, "[\\s|\\-|/|\\|\\\\|，|、|\\(|\\)|（|）]{1}", ",");
            }
            intention.JobType = intention.JobType.Split(',')[0]??"";
            var nodeJobType = document.DocumentNode.SelectSingleNode("nodes[1]/node[@" + attr + " = '" + intention.JobType + "']")?.Attributes["result-key"];
            
            if (!string.IsNullOrWhiteSpace(nodeJobType?.Value))
            {
                tag.JobType = nodeJobType.Value.ToCharArray().First();
            }
            else
            {
                tag.JobType = '*';
            }

            tag.Evaluation = intention.Evaluation;

            return tag;
        }

        private static string FormatJobStatus(string source, string attr,string tag)
        {
            var document = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\PresentStatus" + tag + ".xml"));

            var node = document.DocumentNode.SelectSingleNode("nodes[1]/node[@" + attr + " = '" + source + "']");
            if (node?.Attributes["result-key"] != null)
                return node.Attributes["result-key"].Value;
            return "*";
        }

        private static string FormatMaritalStatus(string source, string attr,string tag)
        {
            var document = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\MaritalStatus" + tag + ".xml"));

            var node = document.DocumentNode.SelectSingleNode("nodes[1]/node[@"+attr+" = '"+source+"']");
            if (node?.Attributes["result-key"] != null)
                return node.Attributes["result-key"].Value;
            return "*";
        }

        private static string FormatDegree(string source, string attr,string tag)
        {
            var document = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\XML\\Degree" + tag + ".xml"));

            var node = document.DocumentNode.SelectSingleNode("nodes[1]/node[@"+attr+" = '"+source+"']");
            if (node?.Attributes["result-key"] != null)
                return node.Attributes["result-key"].Value;
            return "*";
        }

        private static string FormatGender(string source,string attr,string tag)
        {
            var document = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\" + "XML\\Gender" + tag + ".xml"));

            var node = document.DocumentNode.SelectSingleNode("nodes[1]/node[@"+attr+" ='"+source+"']")?.Attributes["result-key"];
            return node == null ? "*" : node.Value;
        }

        private static string FormatCurrentResidence(string source, string attr, string tag)
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                var document = new HtmlAgilityPack.HtmlDocument();

                document.LoadHtml(File.ReadAllText(Application.StartupPath + "\\" + "XML\\Area" + tag + ".xml"));

                source = Regex.Replace(source, "[\\s|\\-|/|\\|\\\\|，|、]{1}", ",");
                var arr = source.Split(',');
                var value = "";
                foreach (var s in arr)
                {
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        var node = document.DocumentNode.SelectSingleNode("nodes[1]/node[@" + attr + " = '" + s + "']");
                        if (!string.IsNullOrWhiteSpace(node?.Attributes["result-key"]?.Value))
                            value =node.Attributes["result-key"].Value;
                    }
                }
                if (string.IsNullOrWhiteSpace(value))
                    return "0x5600000";
                return value;
            }
            return "0x5600000";
        }

        #endregion

        /// <summary>
        /// 带条件的替换
        /// </summary>
        /// <param name="input"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        /// <returns></returns>
        public static string LanguageSkillReplace(this string input, string oldValue, string newValue)
        {
            if (input.Length == 4)
            {
                return input.Replace(oldValue, newValue);
            }

            return input.Replace(oldValue, "");
        }
    }
}
