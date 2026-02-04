using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Breaks;
using NJsonSchema.Annotations;

namespace BrandVue.Models.ExcelExport;

public record ExcelExportModel(CuratedResultsModel CuratedResultsModel, string[] FilterDescriptions, string Name,
    ViewTypeEnum ViewType, string LeadVisualization, RequestMeasureForEntity[] MeasuresForEntity,
    List<AverageTotalRequestModel> AverageRequests, [property: CanBeNull] CrossMeasure Breaks,
    string HelpText) : ISubsetIdProvider, IExcelExportModel
{
    string ISubsetIdProvider.SubsetId => CuratedResultsModel.SubsetId;
}