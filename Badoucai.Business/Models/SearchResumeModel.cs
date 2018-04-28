using System.Collections.Generic;

namespace Badoucai.Business.Model
{
    public class SearchResumeModel
    {
        public string SearchResumeId { get; set; }

        public string Gender { get; set; }

        public string Cellphone { get; set; }

        public string Email { get; set; }

        public bool IsMatched { get; set; } = false;

        public List<string> Companys { get; set; }
    }
}