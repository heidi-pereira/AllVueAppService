using BrandVue.EntityFramework;
using NJsonSchema.Annotations;

namespace BrandVue.Models.ExcelExport;

public record ExcelExportCategoryModel(string SubsetId, string PageName, string ActiveBrand, CategorySortKey SortKey,
    CategoryExportResultCard[] CategoryResultCards, [property: CanBeNull] string FirstBaseVariableName,
    [property: CanBeNull] string SecondBaseVariableName) : ISubsetIdProvider;

public enum CategorySortKey
{
    None,
    BestScores,
    WorstScores,
    OverPerforming,
    UnderPerforming
}