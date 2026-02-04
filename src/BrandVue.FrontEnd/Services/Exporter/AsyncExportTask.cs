using Microsoft.AspNetCore.Mvc;
using NJsonSchema.Annotations;

namespace BrandVue.Services.Exporter
{
    public class AsyncExportTask
    {
        public string CacheKey { get; set; }
        public string FileDownloadName { get; set; }
        public string ContentType { get; set; }
        public ExportStatus Status { get; set; }
        public byte[] ExportResult { get; set; }
    }

    public enum ExportStatus
    {
        Pending,
        Error,
        Complete
    }
}
