namespace BrandVue.SourceData.Dashboard
{
    public interface IPartsRepository
    {
        PartDescriptor GetById(int partId);

        IReadOnlyCollection<PartDescriptor> GetParts();

        void CreatePart(PartDescriptor part);

        void CreateParts(IEnumerable<PartDescriptor> parts);

        void UpdatePart(PartDescriptor part);

        void UpdateParts(IEnumerable<PartDescriptor> parts);

        void DeletePart(int partId);

        void DeletePartsForPane(string paneId);
    }
}
