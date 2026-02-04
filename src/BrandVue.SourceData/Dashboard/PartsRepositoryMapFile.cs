namespace BrandVue.SourceData.Dashboard
{
    public class PartsRepositoryMapFile : OrderedItemRepository<PartDescriptor>, IPartsRepository
    {
        protected override void SetIdentity(
            PartDescriptor target, string identity)
        {
            target.FakeId = identity;
        }

        public void CreatePart(PartDescriptor part)
        {
            throw new Exception("Creating parts in a map file is not supported!");
        }

        public void CreateParts(IEnumerable<PartDescriptor> parts)
        {
            throw new Exception("Creating parts in a map file is not supported!");
        }

        public void UpdatePart(PartDescriptor part)
        {
            throw new Exception("Updating parts in a map file is not supported!");
        }

        public void UpdateParts(IEnumerable<PartDescriptor> parts)
        {
            throw new Exception("Updating parts in a map file is not supported!");
        }

        public void DeletePart(int partId)
        {
            throw new Exception("Deleting parts in a map file is not supported!");
        }

        public void DeletePartsForPane(string paneId)
        {
            throw new Exception("Deleting parts in a map file is not supported!");
        }

        public PartDescriptor GetById(int partId)
        {
            return this.Single(p => p.Id == partId);
        }

        public IReadOnlyCollection<PartDescriptor> GetParts()
        {
            return this;
        }
    }
}
