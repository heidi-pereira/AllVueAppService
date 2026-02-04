using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.EntityFrameworkCore;
using BrandVue.EntityFramework.Exceptions;

namespace BrandVue.SourceData.Dashboard
{

    public class PagesRepositorySql : IPagesRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;
        private readonly IPanesRepository _panesRepository;
        private readonly PanesRepositorySql _panesRepositorySql;
        private readonly string[] _allowedPageTypes;

        public PagesRepositorySql(IProductContext productContext, IDbContextFactory<MetaDataContext> dbContextFactory, IPanesRepository panesRepository)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
            _panesRepository = panesRepository;
            _panesRepositorySql = _panesRepository as PanesRepositorySql;
            _allowedPageTypes = new string[] { "Standard", "SubPage", "MinorPage" };
        }

        public IReadOnlyCollection<PageDescriptor> GetPages()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            return GetPageQuery(dbContext).ToList()
                .Select(ConvertToPageDescriptor).ToList();
        }
        
        public PageDescriptor GetPage(int pageId)
        {            
            using var dbContext = _dbContextFactory.CreateDbContext();

            return ConvertToPageDescriptor(GetPageQuery(dbContext).First(x => x.Id == pageId));
        }

        private IEnumerable<DbPage> GetPageQuery(MetaDataContext dbContext)
        {
            return dbContext.Pages
                .Where(p => p.ProductShortCode == _productContext.ShortCode &&
                            p.SubProductId == _productContext.SubProductId)
                .OrderBy(p => p.PageDisplayIndex)
                .Include(p => p.PageSubsetConfiguration)
                .ThenInclude(ps => ps.Subset);
        }

        public IReadOnlyCollection<PageDescriptor> GetTopLevelPagesWithChildPages()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var allDbPages = dbContext.Pages
                .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId)
                .Include(p=>p.PageSubsetConfiguration)
                .ThenInclude(ps => ps.Subset)
                .ToList();
            var orderedTopLevelPages = allDbPages.Where(p => !p.ParentId.HasValue).OrderBy(p => p.PageDisplayIndex).Select(ConvertToPageDescriptor).ToList();

            foreach (var topLevelPage in orderedTopLevelPages)
            {
                PopulateChildPages(topLevelPage, allDbPages);
            }

            return orderedTopLevelPages;
        }

        public int CreatePage(PageDescriptor page)
        {
            ValidatePage(page);
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                using var transaction = dbContext.Database.BeginTransaction();
                var dbPage = ConvertToDbPage(page,dbContext.SubsetConfigurations.Where(x=>x.ProductShortCode == _productContext.ShortCode).ToList());
                dbContext.Pages.Add(dbPage);
                dbContext.SaveChanges();
                page.Id = dbPage.Id;
                foreach (var paneToCreate in page.Panes)
                {
                    if (_panesRepositorySql != null)
                    {
                        _panesRepositorySql.CreatePane(paneToCreate, dbContext);
                    }
                    else
                    {
                        _panesRepository.CreatePane(paneToCreate);
                    }
                }
                dbContext.SaveChanges();
                transaction.Commit();
                return page.Id;
            }
        }

        public void UpdatePage(PageDescriptor page)
        {
            if (page.Id == 0)
            {
                CreatePage(page);
                return;
            }

            ValidatePage(page, page.Id);
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                var pageToUpdate = ConvertToDbPage(page, dbContext.SubsetConfigurations.Where(x=>x.ProductShortCode == _productContext.ShortCode).ToList());
                var inputPageSubsets = pageToUpdate.PageSubsetConfiguration.Select(x=>x.SubsetId).ToArray();
                using var transaction = dbContext.Database.BeginTransaction();
                string oldPageName = dbContext.Pages.AsNoTracking().Single(p => p.Id == page.Id).Name;
                dbContext.Pages.Attach(pageToUpdate);
                dbContext.Entry(pageToUpdate).State = EntityState.Modified;
                AttachSubsetConfigurations(dbContext, pageToUpdate);
                dbContext.SaveChanges();

                // If a pane was removed from parentPage.Panes, we want to remove it from the database as well.
                var existingPanesForThisPage = dbContext.Panes.Where(p =>
                    p.ProductShortCode == _productContext.ShortCode &&
                    p.SubProductId == _productContext.SubProductId && p.PageName == oldPageName).ToArray();
                // Delete and re-create all panes
                foreach (var paneToDelete in existingPanesForThisPage)
                {
                    if (_panesRepositorySql != null)
                    {
                        _panesRepositorySql.DeletePane(paneToDelete.PaneId, dbContext);
                    }
                    else
                    {
                        _panesRepository.DeletePane(paneToDelete.PaneId);
                    }
                }
                foreach (var paneToCreate in page.Panes)
                {
                    if (_panesRepositorySql != null)
                    {
                        _panesRepositorySql.CreatePane(paneToCreate, dbContext);
                    }
                    else
                    {
                        _panesRepository.CreatePane(paneToCreate);
                    }
                }
                transaction.Commit();

                dbContext.PageSubsetConfigurations.RemoveRange(dbContext
                        .PageSubsetConfigurations
                        .Where(ps => ps.PageId == page.Id).ToList()
                        .Where(existing => 
                            inputPageSubsets.All(incoming => existing.SubsetId != incoming))
                        .ToArray());

                dbContext.SaveChanges();
                
            }
        }

        private static void AttachSubsetConfigurations(MetaDataContext dbContext, DbPage pageToUpdate)
        {
            var existingSubsets = dbContext.PageSubsetConfigurations
                .Where(psc => psc.PageId == pageToUpdate.Id)
                .ToList();
            foreach (var incoming in pageToUpdate.PageSubsetConfiguration)
            {
                if (existingSubsets.Any(existing => existing.SubsetId == incoming.SubsetId))
                {
                    dbContext.Update(incoming);
                }
                else
                {
                    dbContext.Add(incoming);
                }
            }
        }

        public void UpdatePageName(int pageId, string displayName, string name)
        {
            ValidateRequiredString(name, nameof(PageDescriptor.Name));
            ValidateRequiredString(displayName, nameof(PageDescriptor.DisplayName));
            if (PageNameAlreadyExists(name, pageId))
            {
                ThrowValidationException($"Page already exists with name: {name}");
            }

            using var dbContext = _dbContextFactory.CreateDbContext();
            using var transaction = dbContext.Database.BeginTransaction();
            var page = dbContext.Pages
                .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId)
                .Single(p => p.Id == pageId);
            var panes = dbContext.Panes
                .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId)
                .Where(p => p.PageName == page.Name).ToArray();
            page.DisplayName = displayName;
            page.Name = name;
            foreach (var pane in panes)
            {
                pane.PageName = name;
            }
            dbContext.SaveChanges();
            transaction.Commit();
        }

        public void ValidateCanDeletePage(int pageId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var pageToDelete = dbContext.Pages.SingleOrDefault(p => p.Id == pageId);
            if (pageToDelete == null)
            {
                ThrowValidationException($"No page found with id: {pageId}");
            }
            if (dbContext.Pages.Any(p => p.ParentId == pageToDelete.Id))
            {
                ThrowValidationException("Can't delete a page with child pages. Delete all child pages first.");
            }
        }

        public void DeletePage(int pageId)
        {
            ValidateCanDeletePage(pageId);
            using (var dbContext = _dbContextFactory.CreateDbContext())
            {
                using var transaction = dbContext.Database.BeginTransaction();

                var pageToDelete = dbContext.Pages.SingleOrDefault(p => p.Id == pageId);
                var paneIdsToDelete = dbContext.Panes.Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId && p.PageName == pageToDelete.Name).Select(p => p.PaneId).ToArray();
                foreach (var paneIdToDelete in paneIdsToDelete)
                {
                    if (_panesRepositorySql != null)
                    {
                        _panesRepositorySql.DeletePane(paneIdToDelete, dbContext);
                    }
                    else
                    {
                        _panesRepository.DeletePane(paneIdToDelete);
                    }
                }
                dbContext.Pages.Remove(pageToDelete);
                dbContext.SaveChanges();
                transaction.Commit();
            }
        }

        private void PopulateChildPages(PageDescriptor parentPage, List<DbPage> allDbPages)
        {
            var orderedPages = allDbPages
                .OrderBy(p => p.PageDisplayIndex)
                .ThenBy(p => p.Id)
                .ToList();
            foreach (var dbPage in orderedPages
                         .Where(p => p.ParentId.HasValue && p.ParentId == parentPage.Id)
                         .Select(p => ConvertToPageDescriptor(p)))
            {
                parentPage.ChildPages.Add(dbPage);
                PopulateChildPages(dbPage, orderedPages);
            }
        }

        private DbPage ConvertToDbPage(PageDescriptor page, ICollection<SubsetConfiguration> productSubsets)
        {
            return new DbPage
            {
                Id = page.Id,
                ParentId = page.ParentId,
                ProductShortCode = DbFieldConverter.EncodeString(_productContext.ShortCode),
                SubProductId = DbFieldConverter.EncodeString(_productContext.SubProductId),
                Name = DbFieldConverter.EncodeString(page.Name),
                DisplayName = DbFieldConverter.EncodeString(page.DisplayName),
                MenuIcon = DbFieldConverter.EncodeString(page.MenuIcon),
                PageType = DbFieldConverter.EncodeString(page.PageType),
                HelpText = DbFieldConverter.EncodeString(page.HelpText),
                PageSubsetConfiguration = page.PageSubsetConfiguration?
                    .Where(x=> productSubsets.Any(sc=>sc.Identifier == x.Subset))
                    .Select(x=> new PageSubsetConfiguration
                    {
                        PageId = page.Id,
                        HelpText = x.HelpText,
                        SubsetId = productSubsets.First(sc=>sc.Identifier == x.Subset).Id,
                        Enabled = x.Enabled
                    }).ToList() ?? new List<PageSubsetConfiguration>(),
                MinUserLevel = page.MinUserLevel,
                StartPage = page.StartPage,
                Layout = DbFieldConverter.EncodeString(page.Layout),
                PageTitle = DbFieldConverter.EncodeString(page.PageTitle),
                AverageGroup = DbFieldConverter.EncodeArrayOfStrings(page.AverageGroup),
                Roles = DbFieldConverter.EncodeArrayOfStrings(page.Roles),
                PageDisplayIndex = page.PageDisplayIndex,
                DefaultBase = page.DefaultBase,
                DefaultPaneViewType = page.DefaultPaneViewType
            };
        }

        private PageDescriptor ConvertToPageDescriptor(DbPage dbPage)
        {
            return new PageDescriptor
            {
                Id = dbPage.Id,
                ParentId = dbPage.ParentId,
                Name = dbPage.Name,
                DisplayName = dbPage.DisplayName,
                MenuIcon = dbPage.MenuIcon ?? string.Empty,
                PageType = dbPage.PageType,
                HelpText = dbPage.HelpText ?? string.Empty,
                PageSubsetConfiguration = dbPage.PageSubsetConfiguration.Select(x=>
                    new PageSubsetConfigurationModel(subset: x.Subset.Identifier, helpText: x.HelpText,
                        enabled: x.Enabled)).ToList(),
                MinUserLevel = dbPage.MinUserLevel,
                StartPage = dbPage.StartPage,
                Layout = dbPage.Layout ?? string.Empty,
                PageTitle = dbPage.PageTitle ?? string.Empty,
                PageDisplayIndex = dbPage.PageDisplayIndex,
                AverageGroup = DbFieldConverter.DecodeArrayOfStrings(dbPage.AverageGroup),
                Roles = DbFieldConverter.DecodeArrayOfStrings(dbPage.Roles),
                ChildPages = new List<PageDescriptor>(),
                DefaultBase = dbPage.DefaultBase,
                DefaultPaneViewType = dbPage.DefaultPaneViewType
            };
        }

        private void ValidatePage(PageDescriptor page, int? existingPageId = null)
        {
            if (page.Id != 0 && !existingPageId.HasValue)
            {
                ThrowValidationException($"Id field must not have value");
            }
            if (page.Id == 0 && existingPageId.HasValue)
            {
                ThrowValidationException($"Id field must have value");
            }


            ValidateRequiredString(page.Name, nameof(page.Name));

            if (PageNameAlreadyExists(page.Name, existingPageId))
            {
                ThrowValidationException($"Page already exists with name: {page.Name}");
            }

            ValidateRequiredString(page.DisplayName, nameof(page.DisplayName));
            if (!_allowedPageTypes.Contains(page.PageType))
            {
                ThrowValidationException("PageType value not supported");
            }

            if (page.ParentId.HasValue && !GetPages().Any(p => p.Id == page.ParentId.Value))
            {
                ThrowValidationException("Invalid Parent Page");
            }

            if (page.ChildPages?.Count > 0)
            {
                ThrowValidationException("Operations on pages with ChildPages are not supported. Each page has to be managed individually using ParentId to denote hierarchy.");
            }
            if (page.Panes?.Length > 0)
            {
                for (int paneIndex = 0; paneIndex < page.Panes.Length; paneIndex++)
                {
                    var pane = page.Panes[paneIndex];
                    pane.PageName = page.Name;
                    ValidatePane(pane, paneIndex + 1, page.Name);
                }
            }
        }

        public bool PageNameAlreadyExists(string pageName, int? existingPageId = null)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.Pages.Any(p => p.ProductShortCode == _productContext.ShortCode &&
                p.SubProductId == _productContext.SubProductId &&
                p.Name == pageName &&
                p.Id != existingPageId);
        }

        private void ValidatePane(PaneDescriptor pane, int paneIndexInPage, string pageName)
        {
            if (string.IsNullOrWhiteSpace(pane.Id))
            {
                // If no pane id is provided - initialise it to a sensible default id
                pane.Id = $"{pageName}_{paneIndexInPage}";
            }
            ValidateRequiredString(pane.PaneType, nameof(pane.PaneType));
            if (pane.Height <= 0)
            {
                ThrowValidationException("Height value must be a positive integer, most probably 500 or 900");
            }
            if (pane.Roles != null && pane.Roles.Length > 0)
            {
                ThrowValidationException("Roles field is not supported for Pane objects");
            }
            pane.Parts?.ToList().ForEach(part => ValidatePart(part, pane.Id));
        }

        private void ValidatePart(PartDescriptor part, string paneId)
        {
            if (string.IsNullOrWhiteSpace(part.PaneId))
            {
                part.PaneId = paneId;
            }
            else if (part.PaneId != paneId)
            {
                ThrowValidationException("Field Part.PaneId must be either null or equal to the id of the containing pane");
            }
            ValidateRequiredString(part.PartType, nameof(part.PartType));

            if (part.Subset != null && part.Subset.Length > 0)
            {
                ThrowValidationException("Subset field is not supported for Part objects");
            }
            if (part.Roles != null && part.Roles.Length > 0)
            {
                ThrowValidationException("Roles field is not supported for Part objects");
            }

            using var dbContext = _dbContextFactory.CreateDbContext();
            var metricNamesInDB = dbContext.MetricConfigurations.Where(mc => mc.ProductShortCode == _productContext.ShortCode && mc.SubProductId == _productContext.SubProductId).Select(mc => mc.Name);
            var wrongMetricNames = part.AutoMetrics?.Where(am => !metricNamesInDB.Contains(am));
            if (wrongMetricNames != null && wrongMetricNames.Any())
            {
                ThrowValidationException(
                    $"AutoMetrics with that name {string.Join(", ", wrongMetricNames)} for page name was not found in database");
            }


            if (!string.IsNullOrEmpty(part.Spec1))
            {
                var spec1Values = part.Spec1.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (part.PartType == PartType.PageLink)
                {
                    var pageDisplayNamesInDB = dbContext.Pages.Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId).Select(p => p.DisplayName);
                    var wrongPageNames = spec1Values.Where(spec1PageName => !pageDisplayNamesInDB.Contains(spec1PageName));
                    if (wrongPageNames.Any())
                    {
                        ThrowValidationException(
                            $"Part with PartType: {part.PartType} and spec1: {wrongPageNames.First()} - no page with displayName equal to this spec1 value was found in database");
                    }
                }
                else if (part.PartType != "Text")
                {
                    wrongMetricNames = spec1Values.Where(value => !metricNamesInDB.Contains(value));
                    if (wrongMetricNames.Any())
                    {
                        ThrowValidationException(
                            $"Part with PartType: {part.PartType} and spec1: {wrongMetricNames.First()} - no metric with name equal to this spec1 value was found in database");
                    }
                }
            }
        }

        private void ValidateRequiredString(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                ThrowValidationException($"Field {fieldName} is required");
            }
        }

        private static void ThrowValidationException(string message)
        {
            throw new BadRequestException(message);
        }
    }
}
