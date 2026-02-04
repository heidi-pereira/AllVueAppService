namespace BrandVue.SourceData.Dashboard
{
    public class PagesRepositoryMapFile : OrderedItemRepository<PageDescriptor>, IPagesRepository
    {
        protected override void SetIdentity(
            PageDescriptor target, string identity)
        {
            target.Name = identity;
        }

        public IReadOnlyCollection<PageDescriptor> GetPages()
        {
            return this;
        }
        
        public PageDescriptor GetPage(int pageId)
        {
            return this.First(x => x.Id == pageId);
        }

        public IReadOnlyCollection<PageDescriptor> GetTopLevelPagesWithChildPages()
        {
            var allPagesInOrderFromMapFile = (IEnumerable<PageDescriptor>)this;

            var levels = new Stack<PageDescriptor>(new[]
                { new PageDescriptor { Name = "Root", ChildPages = new List<PageDescriptor>() }});

            foreach (var pageFromMapFile in allPagesInOrderFromMapFile)
            {
                var cloneOfPage = (PageDescriptor)pageFromMapFile.Clone();

                int pageTypeToLevel = pageFromMapFile.PageType switch
                {
                    "Standard" => 1,
                    "SubPage" => 2,
                    "MinorPage" => 3,
                    _ => 1
                };

                while (levels.Count > pageTypeToLevel)
                {
                    levels.Pop();
                }

                var pageDescriptor = levels.Peek();
                pageDescriptor.ChildPages ??= new List<PageDescriptor>();
                pageDescriptor.ChildPages.Add(cloneOfPage);

                levels.Push(cloneOfPage);
            }

            return levels.Last().ChildPages.ToList();
        }

        public bool PageNameAlreadyExists(string pageName, int? existingPageId)
        {
            return this.Any(p => p.Name == pageName && p.Id != existingPageId);
        }

        public int CreatePage(PageDescriptor page)
        {
            throw new Exception("Creating pages in a map file is not supported!");
        }

        public void UpdatePage(PageDescriptor page)
        {
            throw new Exception("Updating pages in a map file is not supported!");
        }

        public void UpdatePageName(int pageId, string displayName, string name)
        {
            throw new Exception("Updating pages in a map file is not supported!");
        }

        public void ValidateCanDeletePage(int pageId)
        {
            throw new Exception("Updating pages in a map file is not supported!");
        }

        public void DeletePage(int pageId)
        {
            throw new Exception("Deleting pages in a map file is not supported!");
        }
    }
}
