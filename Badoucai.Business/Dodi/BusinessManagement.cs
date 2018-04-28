using System;
using System.Linq;
using Badoucai.EntityFramework.MySql;
using HtmlAgilityPack;

namespace Badoucai.Business.Dodi
{
    public class BusinessManagement
    {
        public DodiBusiness FormatBusiness(HtmlDocument document)
        {
            var bus = new DodiBusiness();

            bus.Id = Convert.ToInt32(MatchBusinessId(document));
            bus.BranchOffice = MatchBranchOffice(document);
            bus.ChannelManager = MatchChannelManager(document);
            bus.CheckInTime = MatchCheckInTime(document);
            bus.Consultant = MatchConsultant(document);
            bus.CreateTime = MatchCreateTime(document);
            bus.CreateUser = MatchCreateUser(document);
            bus.CustomerService = MatchCustomerService(document);
            bus.FailureTime = MatchFailureTime(document);
            bus.IntentionCourse = MatchIntentionCourse(document);
            bus.InviteUser = MatchInviteUser(document);
            bus.Method = MatchMethod(document);
            bus.OrderId = MatchOrderId(document);
            bus.OwnBrand = MatchOwnBrand(document);
            bus.PositioningTime = MatchPositioningTime(document);
            bus.PromoteBrand = MatchPromoteBrand(document);
            bus.PromoteKeywords = MatchPromoteKeywords(document);
            bus.PromotePeople = MatchPromotePeople(document);
            bus.SignedTime = MatchSignedTime(document);
            bus.Sources = MatchSources(document);
            bus.ToClassTime = MatchToClassTime(document);
            bus.TransactionStatus = MatchTransactionStatus(document);
            bus.TrialState = MatchTrialState(document);
            bus.Type = MatchType(document);
            bus.WithOrTransfer = MatchWithOrTransfer(document);
            bus.IsInvite = MatchIsInvite(document);
            bus.IsRegister = MatchIsRegister(document);
            bus.IsSendSms = MatchIsSendSms(document);

            return bus;
        }

        public DodiUserInfomation FormatUserInfomation(HtmlDocument document)
        {
            var info = new DodiUserInfomation();
            info.Id = Convert.ToInt32(MatchUserId(document));
            info.Birthday = MatchBirthday(document);
            info.BusinessId = Convert.ToInt32(MatchBusinessId(document));
            info.Cellphone = MatchCellphone(document);
            info.Education = MatchEducation(document);
            info.Email = MatchEmail(document);
            info.Gender = MatchGender(document);
            info.GraduatedSchool = MatchGraduatedSchool(document);
            info.GraduationYear = MatchGraduationYear(document).Replace("\t","").Replace("\n", "");
            info.Identity = MatchIdentity(document);
            info.Other = MatchOther(document);
            info.QQ = MatchQQ(document);
            info.Residence = MatchResidence(document);
            info.ProfessionalTitle = MatchProfessionalTitle(document);
            info.UserName = MatchUserName(document);
            info.JobName = MatchJobName(document);
            return info;
        }

        #region 商机信息

        /// <summary>
        /// 商机ID
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchBusinessId(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='business_id'][1]");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value;
        }

        /// <summary>
        /// 咨 询 师
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchConsultant(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@id='user_id']/option[@selected][1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 订单ID
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchOrderId(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='order']");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value;
        }

        /// <summary>
        /// 客服
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchCustomerService(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@id='costomer_id']/option[@selected][1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static DateTime? MatchCreateTime(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_t']/div[2]/label[2]");

            if (node == null) return null;

            return ParseTime(node.InnerText.Trim());
        }

        /// <summary>
        /// 推广品牌
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchPromoteBrand(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@id='extend_id']/option[@selected][1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 创建人
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchCreateUser(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_t']//label[contains(.,'创建者')]");

            if (node == null) return string.Empty;

            return node.NextSibling.NextSibling.InnerText.Trim();
        }

        /// <summary>
        /// 创建人
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchMethod(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_t']//label[contains(.,'获取方式')]");

            if (node == null) return string.Empty;

            return node.NextSibling.NextSibling.InnerText;
        }

        /// <summary>
        /// 所属品牌
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchOwnBrand(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_t']//label[contains(.,'所属品牌')]");

            if (node == null) return string.Empty;

            return node.NextSibling.NextSibling.InnerText;
        }

        /// <summary>
        /// 信息来源
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchSources(HtmlDocument document)
        {
            var sources = string.Empty;

            var nodes = document.DocumentNode.SelectNodes("//div[@class='laiyuan'][1]/select").Take(3);

            sources = nodes.Select(node => node.SelectSingleNode("option[@selected][1]")).Where(temp => temp != null).Aggregate(sources, (current, temp) => current + $"_{temp.NextSibling.InnerText}");

            if (sources.StartsWith("_")) sources = sources.Remove(0, 1);

            return sources;
        }

        /// <summary>
        /// 商机类型
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchType(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_t']//label[contains(.,'商机类型')]");

            if (node == null) return string.Empty;

            return node.NextSibling.NextSibling.InnerText.Trim();
        }

        /// <summary>
        /// 商机类型
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchWithOrTransfer(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_t']//label[contains(.,'同岗/转岗')]");

            if (node == null) return string.Empty;

            return node.NextSibling.NextSibling.InnerText.Trim();
        }

        /// <summary>
        /// 分 公 司
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchBranchOffice(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@id='school_id']/option[1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 渠道经理
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchChannelManager(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@name='qdjl']/option[@selected][1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 推广关键词
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchPromoteKeywords(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='keyword'][1]");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value;
        }

        /// <summary>
        /// 邀约者
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchInviteUser(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@name='invite_id']/option[@selected][1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 意向课程
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchIntentionCourse(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@name='kc_id']/option[@selected][1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 推广专员
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchPromotePeople(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@name='promote_id']/option[@selected][1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 定位登记
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static DateTime? MatchPositioningTime(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_b']//label[contains(.,'定位登记')]");

            if (node == null) return null;

            return ParseTime(node.NextSibling.NextSibling.InnerText.Trim());
        }

        /// <summary>
        /// 报到登记
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static DateTime? MatchCheckInTime(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_b']//label[contains(.,'报到登记')]");

            if (node == null) return null;

            return ParseTime(node.NextSibling.NextSibling.InnerText.Trim());
        }

        /// <summary>
        /// 签约登记
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static DateTime? MatchSignedTime(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_b']//label[contains(.,'签约登记')]");

            if (node == null) return null;

            return ParseTime(node.NextSibling.NextSibling.InnerText.Trim());
        }

        /// <summary>
        /// 到班登记
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static DateTime? MatchToClassTime(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_b']//label[contains(.,'到班登记')]");

            if (node == null) return null;

            return ParseTime(node.NextSibling.InnerText.Trim());
        }

        /// <summary>
        /// 成交状态
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchTransactionStatus(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_b']//label[contains(.,'成交状态')]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText.Trim();
        }

        /// <summary>
        /// 失败登记
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static DateTime? MatchFailureTime(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='main_sa_bs']//label[contains(.,'失败登记')]");

            if (node == null) return null;

            return ParseTime(node.NextSibling.InnerText.Trim());
        }

        /// <summary>
        /// 失败登记
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchTrialState(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//p[@class='main_sa_fb'][1]");

            if (node == null) return string.Empty;

            node = node.ParentNode.SelectSingleNode("ul[1]/li[1]/span[2]");

            if (node == null) return string.Empty;

            return node.InnerText.Trim();
        }

        /// <summary>
        /// 是否有邀约记录
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static bool MatchIsInvite(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_b'][1]/div[1]/a[1]");

            if (node == null) return false;

            if (node.InnerText.Contains("（0）")) return false;

            return true;
        }

        /// <summary>
        /// 是否上门登记
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static bool MatchIsRegister(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='xq_b'][1]/div[2]/a[1]");

            if (node == null) return false;

            if (node.InnerText.Contains("（0）")) return false;

            return true;
        }

        /// <summary>
        /// 是否发送短信
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static bool MatchIsSendSms(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='main_sa_f'][1]");

            if (node == null) return false;

            if (!node.InnerText.Contains("发送短信")) return false;

            return true;
        }

        #endregion

        #region 用户信息

        /// <summary>
        /// 用户ID
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchUserId(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='main_x_bs_b']//label[contains(.,'客 户 ID')]");

            if (node == null) return string.Empty;

            return node.NextSibling.NextSibling.InnerText.Trim();
        }

        /// <summary>
        /// 用户姓名
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchUserName(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='name'][1]");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value.Trim();
        }

        /// <summary>
        /// 出生年月
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchBirthday(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//div[@class='main_x_bs_b']//input[@name='bornTime']");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value.Trim();
        }

        /// <summary>
        /// 毕业年份
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchGraduationYear(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@name='rxtime']/option[@selected][1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 专业名称
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchProfessionalTitle(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='zymc']");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value.Trim();
        }

        /// <summary>
        /// 毕业学校
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchGraduatedSchool(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='school']");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value.Trim();
        }

        /// <summary>
        /// 性别
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchGender(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@name='gender']/option[@selected][1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 现居住地
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchResidence(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='area']");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value.Trim();
        }

        /// <summary>
        /// 身  份
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchIdentity(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@name='sfid']/option[@selected][1]");

            if (node == null) return string.Empty;

            return node.NextSibling.InnerText;
        }

        /// <summary>
        /// 电子邮箱
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchEmail(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='email']");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value.Trim();
        }

        /// <summary>
        /// 手机号
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchCellphone(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='phone']");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value.Trim();
        }

        /// <summary>
        /// QQ
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchQQ(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='qq']");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value.Trim();
        }

        /// <summary>
        /// 学历
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchEducation(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//select[@name='education']/option[@selected][1]");

            if(node == null)
            {
                node = document.DocumentNode.SelectSingleNode("//select[@name='education'][1]");

                if (node == null) return string.Empty;

                node = node.ParentNode.SelectSingleNode("label[2]");

                if (node == null) return string.Empty;

                return node.InnerText.Trim();
            }

            return node.Attributes["value"]?.Value.Trim();
        }

        /// <summary>
        /// 其他信息
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchOther(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='restsinfo']");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value.Trim();
        }

        /// <summary>
        /// 应聘岗位
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static string MatchJobName(HtmlDocument document)
        {
            var node = document.DocumentNode.SelectSingleNode("//input[@name='job']");

            if (node == null) return string.Empty;

            return node.Attributes["value"]?.Value.Trim();
        }

        #endregion

        /// <summary>
        /// 转换时间
        /// </summary>
        /// <param name="timeString"></param>
        /// <returns></returns>
        private static DateTime? ParseTime(string timeString)
        {
            try
            {
                return DateTime.Parse(timeString);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class DodiFormatException : FormatException
    {
        public DodiFormatException(string msg) : base(msg) { }
    }
}