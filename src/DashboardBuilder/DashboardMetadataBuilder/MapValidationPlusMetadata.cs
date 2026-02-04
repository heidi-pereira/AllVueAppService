using System;
using System.Collections.Generic;

namespace DashboardMetadataBuilder
{
    public class MapValidationPlusMetadata
    {
        public IReadOnlyCollection<string> Issues { get; }
        public string UploadedBy { get; }
        public DateTime UploadedAt { get; }
        public string FullPath { get;  }

        public MapValidationPlusMetadata(IReadOnlyCollection<string> issues, string fullPath, string uploadedBy,
            DateTime uploadedAt)
        {
            Issues = issues;
            UploadedBy = uploadedBy;
            UploadedAt = uploadedAt;
            FullPath = fullPath;
        }
    }
}