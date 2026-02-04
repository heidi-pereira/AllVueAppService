using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Breaks;
using NJsonSchema.Annotations;

namespace BrandVue.Models.ExcelExport;

public record ExcelExportMultipleEntitiesModel(MultiEntityRequestModel MultiEntityRequestModel,
    string[] FilterDescriptions, string Name, ViewTypeEnum ViewType, string LeadVisualization,
    RequestMeasureForEntity[] MeasuresForEntity, IEnumerable<AverageTotalRequestModel> AverageRequests,
    [property: CanBeNull] CrossMeasure Breaks, string HelpText) : ISubsetIdProvider, IExcelExportModel
{
    string ISubsetIdProvider.SubsetId => MultiEntityRequestModel.SubsetId;
}