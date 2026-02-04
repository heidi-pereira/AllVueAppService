namespace BrandVue.SourceData.Settings
{
    public interface IInstanceSettings
    {
        DateTimeOffset? LastSignOffDate { get; }
        bool GenerateFromAnswersTable { get; }
        bool ForceBrandTypeAsDefault { get; }
    }
}
