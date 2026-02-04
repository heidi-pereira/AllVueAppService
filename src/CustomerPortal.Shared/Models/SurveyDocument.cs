using System;

namespace CustomerPortal.Shared.Models
{
    public class SurveyDocument
    {
        public string Name { get; set; }
        public DateTime LastModified { get; set; }
        public string Id { get; set; }
        public long Size { get; set; }
        public bool IsClientDocument { get; set; }
        public string ClientName { get; set; }
        public string DownloadUrl { get; set; }
        public bool IsFolder { get; set; }
    }
}