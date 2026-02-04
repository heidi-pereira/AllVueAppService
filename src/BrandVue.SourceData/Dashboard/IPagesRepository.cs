namespace BrandVue.SourceData.Dashboard
{
    public interface IPagesRepository
    {
        IReadOnlyCollection<PageDescriptor> GetPages();

        IReadOnlyCollection<PageDescriptor> GetTopLevelPagesWithChildPages();

        int CreatePage(PageDescriptor page);

        void UpdatePage(PageDescriptor page);

        void UpdatePageName(int pageId, string displayName, string name);

        void ValidateCanDeletePage(int pageId);

        void DeletePage(int pageId);

        bool PageNameAlreadyExists(string pageName, int? existingPageId);
        PageDescriptor GetPage(int pageId);
    }
}
