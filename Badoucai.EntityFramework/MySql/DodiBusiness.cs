using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Badoucai.EntityFramework.MySql
{

    [Table("XSS_Dodi_Business")]
    public class DodiBusiness
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public string Consultant { get; set; }
        public string OrderId { get; set; }
        public string CustomerService { get; set; }
        public DateTime? CreateTime { get; set; }
        public string PromoteBrand { get; set; }
        public string CreateUser { get; set; }
        public string Method { get; set; }
        public string OwnBrand { get; set; }
        public string Sources { get; set; }
        public string Type { get; set; }
        public string WithOrTransfer { get; set; }
        public string BranchOffice { get; set; }
        public string ChannelManager { get; set; }
        public string PromoteKeywords { get; set; }
        public string InviteUser { get; set; }
        public string IntentionCourse { get; set; }
        public string PromotePeople { get; set; }
        public DateTime? PositioningTime { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? SignedTime { get; set; }
        public DateTime? ToClassTime { get; set; }
        public string TransactionStatus { get; set; }
        public DateTime? FailureTime { get; set; }
        public string TrialState { get; set; }
        public bool IsInvite { get; set; }
        public bool IsRegister { get; set; }
        public bool IsSendSms { get; set; }
        public short Status { get; set; }
    }
}