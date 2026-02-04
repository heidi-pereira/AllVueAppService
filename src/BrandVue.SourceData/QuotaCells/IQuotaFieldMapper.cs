namespace BrandVue.SourceData.QuotaCells
{
    public interface IQuotaFieldMapper
    {
        string QuotaField { get; }
        string[] GetAllQuotaCellKeys();
        string GetDescriptionForQuotaCellKey(string quotaCellKey);
        string GetCellKeyForProfile(IReadOnlyDictionary<string, int> fieldValues);
    }
}