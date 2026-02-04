using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public interface IPowerpointChartFactory
    {
        IPowerpointChart GenerateChartForReportPart(
            SavedReport report,
            PartDescriptor part,
            Subset subset,
            Measure loadedMeasure,
            AverageDescriptor overTimeAverage,
            bool overtimeDataEnabled);
    }
}
