using System;

namespace CustomerPortal.Models
{
    public class QuotaCell
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Target { get; set; }
        public int Complete { get; set; }
        public int? ChoiceSetId { get; set; }

        public int PercentComplete => Math.Min((int)(Complete* 100.0 / Target), 100);

    }
}