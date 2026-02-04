using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CustomerPortal.Models
{
    public class Survey
    {
        public int Id { get; set; }
        public string InternalName { get; set; }
        public string Name { get; set; }
        public int Complete { get; set; }
        public int Target { get; set; }
        public List<Quota> Quota { get; set; }
        public DateTime? LaunchDate { get; set; }
        public DateTime? CompleteDate { get; set; }
        public int Status { get; set; }
        public bool IsOpen => Status == 1;
        public bool IsPaused => Status == 2;
        public bool isClosed => Status == 3;
        public int PercentComplete => Target <= 0 ? 0 : Math.Min((int)(Complete * 100.0 / Target), 100);
        public string UniqueSurveyId { get; set; }
        public string NotificationEmails { get; set; }
        public Guid FileDownloadGuid { get; set; }
        [MaxLength(450)]
        public string AuthCompanyId { get; set; }

        public string SubProductId => Id.ToString();
    }
}
