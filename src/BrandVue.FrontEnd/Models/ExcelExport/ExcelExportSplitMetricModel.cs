using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework;
using NJsonSchema.Annotations;

namespace BrandVue.Models.ExcelExport;

public record ExcelExportSplitMetricModel(MultiEntityRequestModel MultiEntityRequestModel,
    string[] FilterDescriptions, string Name, ViewTypeEnum ViewType, string LeadVisualization,
    RequestMeasureForEntity[] MeasuresForEntity, IEnumerable<AverageTotalRequestModel> AverageRequests,
    [property: CanBeNull] CrossMeasure Breaks, string HelpText, string[] MeasureNames) : ISubsetIdProvider, IExcelExportModel
{
    string ISubsetIdProvider.SubsetId => MultiEntityRequestModel.SubsetId;
}