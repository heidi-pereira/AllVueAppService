using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VueReporting.Models;

namespace VueReporting.Services
{
    public interface IReportGeneratorService
    {
        string EgnyteReportsFolderUrl { get; }

        IEnumerable<ImageMetaData> GetAllMeta(byte[] powerpointTemplate);
        Task<byte[]> GenerateReports(ReportTemplate powerPointReport, EntitySet[] brandSets, bool currentBrands,
            bool originalBrands, DateTime reportDate);

        void GenerateAndSaveReports(ReportTemplate powerPointReport, EntitySet[] brandSets, bool currentBrands,
            bool originalBrands, DateTime reportDate);
    }
}