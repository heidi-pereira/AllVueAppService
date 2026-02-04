namespace BrandVue.SourceData.Averages
{
    public interface IAverageDescriptorRepository : IEnumerable<AverageDescriptor>
    {
        int Count { get; }
        AverageDescriptor Get(string identity, string organisationShortCode);
        AverageDescriptor GetCustom(string identity);
        AverageDescriptor this[int index] { get; }
        bool TryGet(string identity, out AverageDescriptor stored);

        IEnumerable<AverageDescriptor> GetAllForClient(string organisationShortCode);
    }
}
