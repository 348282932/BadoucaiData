using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Net;
using Badoucai.Business.Model;

namespace Badoucai.Business.Zhaopin
{
    public class ZhaopinHelper
    {
        #region 智联网聘5.0

        /// <summary>
        /// 智联搜索——网聘5.0
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static SourceResume ConvertTo_Dtl_V0(HtmlDocument document)
        {
            var dto = new SourceResume();
            dto.ResumeNumber = document.DocumentNode.SelectSingleNode("//input[@id='extId']")?.Attributes["value"]?.Value;
            dto.ResumeId = document.DocumentNode.SelectSingleNode("//input[@id='resume_id']")?.Attributes["value"]?.Value;
            dto.UserMasterId = document.DocumentNode.SelectSingleNode("//input[@id='resumeUserId']")?.Attributes["value"]?.Value;
            dto.Height = "";
            //if (string.IsNullOrWhiteSpace(dto.Id))
            //return null;
            dto.Name = document.DocumentNode.SelectSingleNode("//input[@id='tt_username']")?.Attributes["value"]?.Value;
            dto.Gender = DeserializeGender(document);
            dto.MaritalStatus = DeserializeMaritalStatus(document);
            dto.Cellphone = DeserializeCellphone(document);
            dto.Email = DeserializeEmail(document);
            dto.Birthday = DeserializeBirthday(document);
            dto.CurrentResidence = DeserializePresentLocus(document);
            dto.RegisteredResidenc = DeserializeRegisteredAddress(document);
            dto.UpdateTime = document.DocumentNode.SelectSingleNode("//strong[@id='resumeUpdateTime']")?.InnerText;
            dto.WorkStarts = DeserializeServiceYear(document);
            dto.Degree = DeserializeEducationalBackground(document);
            dto.JobStatus = DeserializePresentStatus(document);
            dto.Intention = new ResumeIntention();
            dto.Intention.JobType = DeserializeExpectedNature(document);
            dto.Intention.Salary = DeserializeExpectedSalary(document);
            dto.Intention.Industry = DeserializeExpectedIndustry(document);
            dto.Intention.Function = DeserializeExpectedPosition(document);
            dto.Intention.Location = DeserializeExpectedLocus(document);
            dto.Intention.Evaluation = DeserializeSelfAssessment(document);
            dto.Intention.DutyTime = "";
            dto.Works = DeserializeWorkingExperiences(document);
            dto.Projects = DeserializeProjectExperiences(document);
            dto.Educations = DeserializeEducationExperiences(document);

            if (dto.Educations != null && dto.Educations.Count > 0)
                dto.Major = dto.Educations[0].Major;
            else
            {
                dto.Major = "";
            }
            dto.Trainings = DeserializeTrainingExperiences(document);
            dto.Skills = DeserializeSkill(document);
            dto.Certificates = DeserializeCert(document);
            dto.Honors = DeserializeHonor(document);
            dto.Practices = DeserializePractice(document);
            dto.Others = DeserializeOther(document);

            return dto;
        }

        #region 子项解析

        public static List<OtherInfo> DeserializeOther(HtmlDocument document)
        {
            string[] options = { "兴趣爱好", "获得荣誉", "专业组织", "著作/论文", "专利", "宗教信仰", "特长职业目标", "特殊技能", "社会活动", "荣誉", "推荐售信" };
            var others = new List<OtherInfo>();
            foreach (var option in options)
            {
                var node = document.DocumentNode.SelectSingleNode("//h3[text()='" + option + "']");
                if (node != null)
                {
                    var other = new OtherInfo();
                    other.Title = node.InnerText;
                    if (node.NextSibling?.NextSibling != null && node.NextSibling.NextSibling.Name.Equals("div"))
                    {
                        other.Description = node.NextSibling.NextSibling.InnerText;
                        other.Description = other.Description.Replace("\r\n", "").Replace(" ", "");
                        others.Add(other);
                    }
                }
            }
            return others;
        }
        public static List<SchoolPractice> DeserializePractice(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//h3[text()='在校实践经验']");
            var nodes = node?.ParentNode.SelectNodes("./h2");
            if (nodes != null && nodes.Count > 0)
            {
                var practices = new List<SchoolPractice>();

                foreach (var sub in nodes)
                {
                    var str = sub.InnerText;
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        var practice = new SchoolPractice();
                        str = str.Replace("&nbsp;&nbsp;", "|");
                        var arr = str.Split('|');
                        var timeArr = arr[0].Split('-');
                        practice.Begin = timeArr.Length > 0 ? timeArr[0] : "";
                        practice.End = timeArr.Length > 1 ? timeArr[1] : "";
                        practice.Name = arr.Length > 1 ? arr[1] : "";
                        if (sub.NextSibling?.NextSibling != null && sub.NextSibling.NextSibling.Name.Equals("div"))
                        {
                            practice.Description = sub.NextSibling.NextSibling.InnerText;
                            practice.Description = practice.Description.Replace("\r\n", "").Replace(" ", "");
                        }
                        else
                        {
                            practice.Description = "";
                        }
                        practices.Add(practice);
                    }
                }

                return practices;
            }
            return null;
        }
        public static List<SchoolHonor> DeserializeHonor(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//h3[text()='在校学习情况']");
            var nodes = node?.ParentNode.SelectNodes("./h2");
            if (nodes != null && nodes.Count > 0)
            {
                var honors = new List<SchoolHonor>();
                foreach (var sub in nodes)
                {
                    var honorStr = sub.InnerText;
                    if (!string.IsNullOrWhiteSpace(honorStr))
                    {
                        var honor = new SchoolHonor();
                        var arr = honorStr.Split(" ");
                        if (arr.Length == 3)
                        {
                            honor.Time = arr[0];
                            honor.Honor = arr[1] + " " + arr[2];
                        }
                        else if (arr.Length == 4)
                        {
                            honor.Time = arr[0] + " " + arr[1];
                            honor.Honor = arr[2] + " " + arr[3];
                        }
                        if (!string.IsNullOrWhiteSpace(honor.Time))
                        {
                            if (sub.NextSibling?.NextSibling != null && sub.NextSibling.NextSibling.Name.Equals("div"))
                            {
                                honor.Description = sub.NextSibling.NextSibling.InnerText;
                                honor.Description = honor.Description.Replace("\r\n", "").Replace(" ", "");
                            }
                            else
                            {
                                honor.Description = "";
                            }
                            honors.Add(honor);
                        }
                    }

                }

                return honors;
            }
            return null;
        }
        public static List<ResumeCertificate> DeserializeCert(HtmlDocument document)
        {

            var node = document.DocumentNode.SelectSingleNode("//h3[text()='证书']");
            var certNodes = node?.ParentNode.SelectNodes("./h2");
            if (certNodes != null && certNodes.Count > 0)
            {
                var resumeCerts = new List<ResumeCertificate>();
                foreach (var cert in certNodes)
                {
                    var certArr = Regex.Split(cert.InnerText, "&nbsp;");
                    if (certArr.Length > 1)
                    {
                        try
                        {
                            var resumeCert = new ResumeCertificate { Time = Convert.ToDateTime(certArr[0]), CertName = certArr.Last() };
                            resumeCerts.Add(resumeCert);
                        }
                        catch { }
                    }
                }
                return resumeCerts;
            }
            return null;
        }
        public static List<ResumeSkill> DeserializeSkill(HtmlDocument document)
        {
            var resumeSkills = new List<ResumeSkill>();

            var node = document.DocumentNode.SelectSingleNode("//h3[text()='专业技能']");
            var skillStr = node?.ParentNode?.SelectSingleNode("./div[1]")?.InnerHtml;
            if (!string.IsNullOrWhiteSpace(skillStr))
            {
                skillStr = skillStr.Replace("\r\n", "");
                var skills = Regex.Split(skillStr, "<br>");
                if (skills.Length > 0)
                {
                    resumeSkills.AddRange(from skill in skills where skill.Contains("|") select skill.Split('：') into skllArr where skllArr.Length == 2 select new ResumeSkill { Name = skllArr[0], Level = skllArr[1] });
                }
            }

            node = document.DocumentNode.SelectSingleNode("//h3[text()='语言能力']");
            var langStr = node?.ParentNode?.SelectSingleNode("./div[1]")?.InnerHtml;
            if (!string.IsNullOrWhiteSpace(langStr))
            {
                langStr = langStr.Replace("\r\n", "");
                var langs = Regex.Split(langStr, "<br>");
                if (langs != null && langs.Length > 0)
                {
                    foreach (var lang in langs)
                    {
                        var langArr = lang.Split('：');
                        if (langArr.Length > 1)
                        {
                            var resumeSkill = new ResumeSkill { Name = langArr[0], Level = langArr[1] };
                            resumeSkills.Add(resumeSkill);
                        }
                    }
                }
            }
            return resumeSkills;
        }
        public static List<ResumeEducation> DeserializeEducationExperiences(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[h3[1] = '教育经历']/div[1]");

            // From DeserializeEducationExperiences/Not-Contains.html

            if (node == null)
            {
                return null;
            }

            var experiences = new List<ResumeEducation>();

            foreach (var value in node.SelectNodes("text()").Select(t => t.InnerText.Trim()).Where(t => t.Length > 0))
            {
                var match = Regex.Match(value, @"^(\d{4}.\d{2})\s+-\s+(至今|\d{4}.\d{2})((&nbsp;|\s){2,}(.+))$");

                // Sample was not found.

                if (!match.Success)
                {
                    throw new FormatException("Education experience format was unexpected.");
                }

                // From DeserializeEducationExperiences/Standard.doc

                // From DeserializeEducationExperiences/Not-Contains-School.html

                // From DeserializeEducationExperiences/Not-Contains-Major.html

                var experience = new ResumeEducation();

                experience.Begin = match.Result("$1");

                experience.End = match.Result("$2");

                match = Regex.Match(match.Result("$3"), @"^(.*?)((&nbsp;|\s){2,}(初中|中技|高中|中专|大专|本科|硕士|MBA|EMBA|博士|其他)|)$");

                if (!match.Success)
                {
                    throw new FormatException("Education experience format was unexpected.");
                }

                experience.Degree = match.Result("$4") ?? "";

                match = Regex.Match(match.Result("$1"), @"^((&nbsp;|\s){2,}(.*?)|)((&nbsp;|\s){2,}(.*?)|)$");

                if (!match.Success)
                {
                    throw new FormatException("Education experience format was unexpected.");
                }

                experience.School = match.Result("$3") ?? "";

                experience.Major = match.Result("$6") ?? "";
                experience.Description = "";

                experiences.Add(experience);
            }

            return experiences;
        }
        public static List<ResumeTraining> DeserializeTrainingExperiences(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div/h3[1][. = '培训经历']");

            // From DeserializeTrainingExperiences/Not-Contains.html

            if (node == null)
            {
                return null;
            }

            var experiences = new List<ResumeTraining>();

            while ((node = node.NextSibling) != null)
            {
                if (node.Name == "#text")
                {
                    continue;
                }

                if (node.Name != "h2")
                {
                    throw new FormatException($"Training experience node ({node.Name}) was unexpected.");
                }

                var experience = default(ResumeTraining);

                do
                {
                    if (node.Name == "#text")
                    {
                        continue;
                    }

                    if (node.Name != "h2")
                    {
                        break;
                    }

                    var match = Regex.Match(node.InnerText.Trim(), @"^(\d{4}\.\d{2})\s+-\s+(至今|\d{4}\.\d{2})(&nbsp;|\s){2,}(.*?)$");

                    // Sample was not found yet.

                    if (!match.Success)
                    {
                        throw new FormatException("Training experience format was unexpected.");
                    }

                    experiences.Add(experience = new ResumeTraining
                    {
                        Begin = match.Result("$1"),
                        End = match.Result("$2"),
                        Course = match.Result("$4")
                    });
                }
                while ((node = node.NextSibling) != null);

                if (node == null)
                {
                    break;
                }

                if (node.Name != "div")
                {
                    throw new FormatException($"Training experience node ({node.Name}) was unexpected.");
                }

                // From DeserializeTrainingExperiences/Standard.html

                var mapping = new Dictionary<string, string>
                {
                    { "培训机构", "" },
                    { "培训地点", "" },
                    { "培训描述", "" }
                };

                // From DeserializeTrainingExperiences/Standard.html

                foreach (var td in node.SelectNodes("table[1]/tr/td[1]"))
                {
                    var name = Regex.Match(td.InnerText.Trim(), "^(.{4})：$").Result("$1");

                    // Sample was not found yet.

                    if (name == null)
                    {
                        throw new FormatException("Training experience detail format was unexpected.");
                    }

                    // Sample was not found yet.

                    if (mapping.ContainsKey(name))
                    {
                        //throw new FormatException($"Training experience ({name}) was undefined.");
                        mapping[name] = td.SelectSingleNode("../td[2]").InnerHtml.Trim();
                    }

                }

                if (experience == null)
                {
                    continue;
                }

                experience.Institution = mapping["培训机构"];

                experience.Location = mapping["培训地点"];

                experience.Description = mapping["培训描述"];
                experience.Description = RemoveHtmlTag(experience.Description);
            }

            return experiences;
        }
        public static List<ResumeProject> DeserializeProjectExperiences(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div/h3[1][. = '项目经历']");

            // From DeserializeProjectExperiences/Not-Contains.html

            if (node == null)
            {
                return null;
            }

            var experiences = new List<ResumeProject>();

            while ((node = node.NextSibling) != null)
            {
                if (node.Name == "#text")
                {
                    continue;
                }

                if (node.Name != "h2")
                {
                    throw new FormatException($"Project experience node ({node.Name}) was unexpected.");
                }

                var experience = default(ResumeProject);

                // From DeserializeProjectExperiences/Standard.html

                // From DeserializeProjectExperiences/Not-Contains-Detail.html

                do
                {
                    if (node.Name == "#text")
                    {
                        continue;
                    }

                    if (node.Name != "h2")
                    {
                        break;
                    }

                    var match = Regex.Match(node.InnerText.Trim(), @"^(\d{4}\.\d{2})\s+-\s+(至今|\d{4}\.\d{2})((&nbsp;|\s)+(.+?)|)$");

                    // From DeserializeProjectExperiences/Not-Contains-Name.html

                    if (!match.Success)
                    {
                        throw new FormatException("Project experience format was unexpected.");
                    }

                    // From DeserializeProjectExperiences/Standard.html

                    experiences.Add(experience = new ResumeProject
                    {
                        Begin = match.Result("$1"),
                        End = match.Result("$2"),
                        Name = match.Result("$5")
                    });
                }
                while ((node = node.NextSibling) != null);

                if (node == null)
                {
                    break;
                }

                if (node.Name != "div")
                {
                    throw new FormatException($"Project experience node ({node.Name}) was unexpected.");
                }

                var table = node.SelectSingleNode("table[1][tr]");

                // From DeserializeProjectExperiences/Not-Contains-Detail.html

                if (table == null)
                {
                    continue;
                }

                // From DeserializeProjectExperiences/Standard.html

                var mapping = new Dictionary<string, string>
                {
                    { "责任描述", null },
                    { "项目描述", null }
                };

                foreach (var td in table.SelectNodes("tr/td[1]"))
                {
                    var name = Regex.Match(td.InnerText.Trim(), "^(.{4})：$").Result("$1");

                    // Sample was not found yet.

                    if (name == null)
                    {
                        throw new FormatException("Project experience detail format was unexpected.");
                    }

                    // Sample was not found yet.

                    if (mapping.ContainsKey(name))
                    {
                        //throw new FormatException($"Project experience ({name}) was undefined.");
                        mapping[name] = td.SelectSingleNode("../td[2]").InnerHtml.Trim();
                    }

                }

                if (experience == null)
                {
                    break;
                }

                // From DeserializeProjectExperiences/Standard.doc

                experience.Duty = mapping["责任描述"];
                experience.Duty = RemoveHtmlTag(experience.Duty);

                experience.Description = mapping["项目描述"];
                experience.Description = RemoveHtmlTag(experience.Description);
            }

            return experiences;
        }
        public static List<ResumeWork> DeserializeWorkingExperiences(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div/h3[1][. = '工作经历']");

            if (node == null)
            {
                return null;
            }

            var experiences = new List<ResumeWork>();

            while ((node = node.NextSibling) != null)
            {
                if (node.Name == "#text")
                {
                    continue;
                }

                if (node.Name != "h2")
                {
                    throw new Exception("格式化工作经验失败！");
                }

                ResumeWork experience;

                do
                {
                    // From DeserializeWorkingExperiences/Standard.html

                    // e.g. 2015.02 - 2015.06&nbsp;&nbsp;北京星石投资管理有限公司&nbsp;&nbsp;（4个月）

                    // e.g. 2012.08 - 至今&nbsp;&nbsp;HiAll&nbsp;&nbsp;（3年4个月）

                    // e.g. 2012.09 - 2012.09&nbsp;&nbsp;强生

                    var match = Regex.Match(node.InnerText.Trim(), @"^(?s:(\d{4}\.\d{2})\s+-\s+(至今|\d{4}\.\d{2})(&nbsp;|\s){2,}(.*?))$");

                    // Sample was not found yet.

                    if (!match.Success)
                    {
                        throw new Exception("格式化工作经验失败！");
                    }

                    experience = new ResumeWork();

                    experience.Begin = match.Result("$1");

                    experience.End = match.Result("$2");


                    match = Regex.Match(match.Result("$4"), @"^(?s:(.*?)((&nbsp;|\s){2,}|)(（((\d+年|)(\d+个月|))）|))$");

                    // Sample was not found yet.

                    if (!match.Success)
                    {
                        throw new FormatException("Working experience summary format was unexpected.");
                    }

                    experience.Company = match.Result("$1") ?? "";
                    // From DeserializeWorkingExperiences/Standard.html



                    // From DeserializeWorkingExperiences/Not-Contains-Duration.html

                    //experience.Duration = match.Result("$5");

                    experiences.Add(experience);

                    while ((node = node.NextSibling)?.Name == "#text")
                    {
                    }

                    if (node == null)
                    {
                        break;
                    }

                    if (node.Name != "h5")
                    {
                        throw new FormatException($"Working experience node ({node.Name}) was unexpected.");
                    }

                    // From DeserializeWorkingExperiences/Standard.html

                    // e.g. 杭州分销办事处 | 销售主管 | 6001-8000元/月

                    // e.g. 系统部高级专员 | 2001-4000元/月

                    match = Regex.Match(node.InnerText, @"^(.*?)(\s+[|]\s+((\d+-|)\d+元/月(以下|以上|)|保密|)|)\s*$");

                    // Sample was not found yet.

                    if (!match.Success)
                    {
                        throw new FormatException("Working experience position format was unexpected.");
                    }

                    //experience.Salary = match.Result("$3");

                    match = Regex.Match(match.Result("$1"), @"^\s*((.*?)\s+[|]\s+|)\s*(.*?)\s*$");

                    // Sample was not found yet.

                    if (match.Success)
                    {
                        //throw new FormatException("Working experience position format was unexpected.");
                        experience.Department = match.Result("$2");
                        experience.Position = match.Result("$3");
                    }
                    experience.Department = experience.Department ?? "";
                    experience.Position = experience.Position ?? "";

                    // From DeserializeWorkingExperiences/Not-Contains-Salary.html

                    // e.g. 社会科学处 | 助理研究员

                    // From DeserializeWorkingExperiences/Not-Contains-Department.html

                    // e.g. 网络管理员

                    // From DeserializeWorkingExperiences/Not-Contains-Position.html


                    while ((node = node.NextSibling)?.Name == "#text")
                    {
                    }

                    if (node == null)
                    {
                        break;
                    }

                    if (node.Name != "div")
                    {
                        throw new FormatException($"Working experience node ({node.Name}) was unexpected.");
                    }

                    // From DeserializeWorkingExperiences/Standard.html

                    // From DeserializeWorkingExperiences/Not-Contains-Nature.html

                    // e.g. 教育/培训/院校

                    // e.g. 耐用消费品（服饰/纺织/皮革/家具/家电） | 企业性质：民营

                    // e.g. 房地产/建筑/建材/工程 | 企业性质：民营 | 规模：1000-9999人

                    foreach (var data in node.InnerText.Split('|'))
                    {
                        match = Regex.Match(data.Trim(), @"^(.+?)：(.+?)$");

                        if (match.Success)
                        {
                            var name = match.Result("$1");

                            if (name == "规模")
                            {
                                experience.Size = match.Result("$2");
                            }
                            else if (name == "企业性质")
                            {
                                experience.Nature = match.Result("$2");
                            }
                            else
                            {
                                throw new FormatException($"Working experience company ({name}) was undefined.");
                            }
                        }
                        else
                        {
                            experience.Industry = data.Trim();
                        }
                    }
                    experience.Nature = experience.Nature ?? "";
                    experience.Size = experience.Size ?? "";
                    experience.Industry = experience.Industry ?? "";

                    while ((node = node.NextSibling)?.Name == "#text")
                    {
                    }
                }
                while (node?.Name == "h2");

                if (node == null)
                {
                    break;
                }

                if (node.Name != "div")
                {
                    throw new FormatException($"Working experience node ({node.Name}) was unexpected.");
                }

                //experience.Managements = new List<Management>();

                // From DeserializeWorkingExperiences/Standard.html

                if (node.SelectNodes("table/tr[1]/td[1]") != null)
                {
                    foreach (var td in node.SelectNodes("table/tr[1]/td[1]"))
                    {
                        var name = td.InnerText.Trim();

                        if (name == "工作描述：")
                        {
                            experience.Description = td.SelectSingleNode("../td[2]").InnerHtml.Trim();
                            experience.Description = RemoveHtmlTag(experience.Description);
                        }
                        /*else if (name == "管理经验：")
                        {
                            var match = Regex.Match(td.SelectSingleNode("../td[2]").InnerHtml.Trim(), @"^(?s:\s*(.*?)\s*(<br>|)(业绩描述：\s*(.*?)\s*|))$");

                            // Sample was not found yet.

                            if (!match.Success)
                            {
                                throw new FormatException("Working experience management format was unexpected.");
                            }

                            var management = new Management
                            {
                                Achievement = match.Result("$4")
                            };

                            experience.Managements.Add(management);

                            // From DeserializeWorkingExperiences/Standard.html

                            var value = match.Result("$1");

                            if (string.IsNullOrEmpty(value))
                            {
                                continue;
                            }

                            var mapping = new Dictionary<string, string>
                            {
                                { "汇报对象", null },
                                { "下属人数", null },
                                { "直接下属", null },
                                { "年收入", null }
                            };

                            foreach (var data in match.Result("$1").Split('|'))
                            {
                                match = Regex.Match(data, @"^\s*(.+?)：(.+?)\s*$");

                                if (!match.Success)
                                {
                                    throw new FormatException("Working experience management format was unexpected.");
                                }

                                name = match.Result("$1");

                                if (!mapping.ContainsKey(name))
                                {
                                    throw new FormatException($"Working experience management ({name}) was undefined.");
                                }

                                mapping[name] = match.Result("$2");
                            }

                            // From DeserializeWorkingExperiences/Standard.doc

                            management.Leader = mapping["汇报对象"];

                            management.SubordinateCount = mapping["下属人数"];

                            management.SubordinateType = mapping["直接下属"];

                            management.AnnualEarnings = mapping["年收入"];
                        }
                        else
                        {
                            throw new FormatException($"Working experience ({name}) was undefined.");
                        }*/
                    }
                }

                experience.Description = experience.Description ?? "";
            }

            return experiences;
        }
        public static string DeserializeSelfAssessment(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[h3 = '自我评价']/div[1]");
            if (node == null)
                return "";

            var content = node.InnerHtml.Trim();
            content = RemoveHtmlTag(content);
            return content;
        }
        public static string DeserializeExpectedLocus(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[3][h3[1] = '求职意向']/div[1]/table[1]/tr[td[1] = '期望工作地区：']/td[2]");
            if (node == null)
                return "";

            return node.InnerText.Trim();
        }
        public static string DeserializeExpectedPosition(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[3][h3[1] = '求职意向']/div[1]/table[1]/tr[td[1] = '期望从事职业：']/td[2]");
            if (node == null)
                return "";

            return node.InnerText.Trim();
        }
        public static string DeserializeExpectedIndustry(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[3][h3[1] = '求职意向']/div[1]/table[1]/tr[td[1] = '期望从事行业：']/td[2]");
            if (node == null)
                return "";
            return node.InnerText.Trim();
        }
        public static string DeserializeExpectedSalary(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[3][h3[1] = '求职意向']/div[1]/table[1]/tr[td[1] = '期望月薪：']/td[2]");
            if (node == null)
                return "";

            return node.InnerText.Trim();
        }
        public static string DeserializeExpectedNature(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[3][h3[1] = '求职意向']/div[1]/table[1]/tr[td[1] = '期望工作性质：']/td[2]");
            if (node == null)
                return "";

            return node.InnerText.Trim();
        }
        public static string DeserializePresentStatus(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[3][h3[1] = '求职意向']/div[1]/table[1]/tr[td[1] = '目前状况：']/td[2]");
            if (node == null)
                return "";

            return node.InnerText.Trim();
        }
        public static string DeserializeEducationalBackground(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[2]/div[1][@class = 'summary-top']/span[1]");
            if (node == null)
            {
                return "";
            }
            var match = Regex.Match(node.InnerText, "初中|中技|高中|中专|大专|本科|硕士|MBA|EMBA|博士|其他");
            if (match.Success)
                return match.Result("$0");

            return "其他";
        }
        public static string DeserializeServiceYear(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[2]/div[1][@class = 'summary-top']/span[1]");

            if (node == null)
            {
                return "0";
            }
            if (Regex.Match(node.InnerText, @"\d+年工作经验").Success)
                return Regex.Match(node.InnerText, @"\d+年工作经验")?.Result("$0");

            return "0";
        }
        public static string DeserializePresentLocus(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[2]/div[1][@class = 'summary-top']/text()[contains(., '现居住地：')]");
            if (node != null)
            {
                var match = Regex.Match(node.InnerText, @"现居住地：\s*([^|]+?)\s*([|]|$)");

                if (match.Success)
                {
                    return match.Result("$1");
                }
            }

            return "";
        }
        public static string DeserializeRegisteredAddress(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[2]/div[1][@class = 'summary-top']/text()[contains(., '现居住地：')]");
            if (node != null)
            {
                var match = Regex.Match(node.InnerText, @"户口：\s*([^|]+?)\s*([|]|$)");

                if (match.Success)
                {
                    return match.Result("$1") ?? "";
                }
            }

            return "";
        }
        public static string DeserializeBirthday(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[2]/div[1][@class = 'summary-top']/span[1]");
            if (node == null)
            {
                return "";
            }

            var match = Regex.Match(node.InnerText, @"\d+年\d+月");
            if (match.Success)
            {
                return match.Value;
            }

            return "";
        }
        public static string DeserializeEmail(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[2]/div[2]/div[1]/div[1]/div[1]/div[1]/span[1]/em[2]/i[1][@class = 'mail']");

            if (node == null)
            {
                return "";
            }
            return node.InnerText.Trim();
        }
        public static string DeserializeGender(HtmlDocument doc)
        {
            var subNode = doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[2]/div[1][@class = 'summary-top']/span[1]");
            if (subNode != null)
            {
                var match = Regex.Match(subNode.InnerText, "男|女");
                if (match.Success)
                {
                    return match.Value;
                }
            }
            return "";
        }
        public static string DeserializeMaritalStatus(HtmlDocument doc)
        {
            var subNode = doc.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[2]/div[1][@class = 'summary-top']/span[1]");
            if (subNode != null)
            {
                var match = Regex.Match(subNode.InnerText, "未婚|已婚|离异");
                if (match.Success)
                {
                    return match.Value;
                }
            }
            return "";
        }
        public static string DeserializeCellphone(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("/html[1]/body[1]/div[1]/div[1]/div[2]/div[1]/div[1]/div[1]/div[2]/div[2]/div[1]/div[1]/div[1]/div[1]/span[1]/em[1]/b[1][normalize-space(.) != '']");

            if (node == null)
            {
                return "";
            }
            var phone = node.InnerText.Trim();
            if (!string.IsNullOrWhiteSpace(phone))
            {
                var match = Regex.Match(phone, @"\d{11}");
                if (match.Success)
                {
                    phone = match.Value;
                }
                else
                    phone = "";
            }
            return phone;
        }

        #endregion

        #endregion
        public static string RemoveHtmlTag(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return "";
            Regex reg = new Regex("(?!<br>)(?!<br/>)<[^>]*>|<\\/[^>]*>", RegexOptions.IgnoreCase);
            var result = reg.Replace(source, "");
            return result;
        }
    }
}
