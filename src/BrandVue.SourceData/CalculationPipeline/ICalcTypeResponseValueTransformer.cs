namespace BrandVue.SourceData.CalculationPipeline
{
    internal interface ICalcTypeResponseValueTransformer
    {
        int? Transform(int? primaryFieldValue, int? secondaryFieldValue);
    }
}