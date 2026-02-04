using System.Collections.Generic;

namespace CustomerPortal.Models
{
    public class Quota
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<QuotaCell> QuotaCells { get; set; }
        public int SurveyId { get; set; }
        public int Order { get; set; }
    }
}