namespace BrandVue.SourceData.Dashboard
{
    public class PanesRepositoryMapFile : OrderedItemRepository<PaneDescriptor>, IPanesRepository
    {
        protected override void SetIdentity(PaneDescriptor target, string identity)
        {
            target.Id = identity;
        }

        public IReadOnlyCollection<PaneDescriptor> GetPanes()
        {
            return this;
        }

        public void CreatePane(PaneDescriptor pane)
        {
            throw new Exception("Creating panes in a map file is not supported!");
        }

        public void DeletePane(string paneId)
        {
            throw new Exception("Deleting panes in a map file is not supported!");
        }

        public void UpdatePane(PaneDescriptor pane)
        {
            throw new Exception("Updating panes in a map file is not supported!");
        }
    }
}
