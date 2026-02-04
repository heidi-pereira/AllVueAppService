using System.Threading;
using BrandVue.Models.ExcelExport;

namespace BrandVue.Services.Exporter
{
    public interface IExcelChartExportService
    {
        Task<ExportToExcel> CreateExporter(ExcelExportModel model, CancellationToken cancellationToken);
        Task<ExportToExcel> CreateExporterForMultiple(ExcelExportMultipleEntitiesModel model,
            CancellationToken cancellationToken, CancellationToken cancellationToken1);
        Task<ExportToExcel> CreateExporterForCategory(ExcelExportCategoryModel model);

        Task<ExportToExcel> CreateExporterForSplitMetric(ExcelExportSplitMetricModel model,
            CancellationToken cancellationToken);
    }
}