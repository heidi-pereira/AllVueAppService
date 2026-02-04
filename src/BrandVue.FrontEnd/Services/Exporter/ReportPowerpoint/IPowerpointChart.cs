using Aspose.Slides;
using System.Threading;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public interface IPowerpointChart
    {
        Task AddChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken);
    }
}
