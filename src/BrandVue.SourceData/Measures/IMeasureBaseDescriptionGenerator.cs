using BrandVue.EntityFramework.MetaData.BaseSizes;

namespace BrandVue.SourceData.Measures;

public interface IMeasureBaseDescriptionGenerator
{
    string BaseExpressionDefinitionToString(BaseExpressionDefinition baseExpressionDefinition);
    (string BaseDescription, bool HasCustomBase) GetBaseDescriptionAndHasCustomBase(Measure measure, Subset subset);
}