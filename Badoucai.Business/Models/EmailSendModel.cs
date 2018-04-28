namespace Badoucai.Business.Model
{
    public class EmailSendModel
    {
        public int ResumeCount { get; set; }
        public int UploadCount { get; set; }
        public int CreateCount { get; set; }
        public int UpdateCount { get; set; }
        public int SurplusCount { get; set; }
        public int DeliverCount { get; set; }
        public int NoJsonCount { get; set; }
        public int AvgCreateCount { get; set; }
    }
}