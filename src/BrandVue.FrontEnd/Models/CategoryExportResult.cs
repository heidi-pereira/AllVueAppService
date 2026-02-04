using NJsonSchema.Annotations;

namespace BrandVue.Models;

public record CategoryExportResult(string Name, double? FirstBaseValue, double? SecondBaseValue,
    [property: CanBeNull] int? Index = null);

public record CategoryExportResultCard(string Title, CategoryExportResult[] Results, bool IsDetailed,
    bool ContainsMarketAverage, int PaneIndex, [property: CanBeNull] string QuestionText = null);
