namespace BrandVue.SourceData.Dashboard
{
    public interface IPanesRepository
    {
        IReadOnlyCollection<PaneDescriptor> GetPanes();

        void CreatePane(PaneDescriptor pane);

        void UpdatePane(PaneDescriptor pane);

        void DeletePane(string paneId);
    }
}
