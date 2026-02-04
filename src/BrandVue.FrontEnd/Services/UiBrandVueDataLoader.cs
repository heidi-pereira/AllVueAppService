using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BrandVue.EntityFramework.MetaData.Page;

namespace BrandVue.Services
{
    public class UiBrandVueDataLoader : IUiBrandVueDataLoader
    {
        private readonly ILogger<UiBrandVueDataLoader> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IBrandVueDataLoaderSettings _settings;
        private readonly IDbContextFactory<MetaDataContext> _metaDataContextFactory;
        private readonly IProductContext _productContext;
        private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;

        public UiBrandVueDataLoader(ILoggerFactory loggerFactory,
            IBrandVueDataLoaderSettings settings,
            IDbContextFactory<MetaDataContext> metaDataContextFactory,
            IProductContext productContext,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            BrandVueDataLoader brandVueDataLoader)
        {
            _logger = loggerFactory.CreateLogger<UiBrandVueDataLoader>();
            _loggerFactory = loggerFactory;
            _settings = settings;
            _metaDataContextFactory = metaDataContextFactory;
            _productContext = productContext;
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
            BrandVueDataLoader = brandVueDataLoader;
        }

        public IPagesRepository PageRepository { get; private set; }
        public IPanesRepository PaneRepository { get; private set; }
        public IPartsRepository PartRepository { get; private set; }
        public IBrandVueDataLoader BrandVueDataLoader { get; }

        public void LoadBrandVueMetadata()
        {
            BrandVueDataLoader.LoadBrandVueMetadata();
            LoadDashboardUserInterfaceConfiguration();
        }

        private void LoadDashboardUserInterfaceConfiguration()
        {
            if (_settings.LoadConfigFromSql)
            {
                PartRepository = new PartsRepositorySql(_productContext, _metaDataContextFactory);

                PaneRepository = new PanesRepositorySql(_productContext, _metaDataContextFactory, PartRepository, BrandVueDataLoader.SubsetRepository);

                PageRepository = new PagesRepositorySql(
                    _productContext,
                    _metaDataContextFactory,
                    PaneRepository);

                ConfigureMultiEntitySplitByForReportsCardCharts((PartsRepositorySql) PartRepository);

                return;
            }

            PageRepository = new PagesRepositoryMapFile();
            var pageLoader = new PageInformationLoader(
                BrandVueDataLoader.SubsetRepository,
                (PagesRepositoryMapFile)PageRepository,
                _commonMetadataFieldApplicator,
                _loggerFactory.CreateLogger<PageInformationLoader>());
            pageLoader.LoadIfExists(_settings.PageMetadataFilepath);

            PaneRepository = new PanesRepositoryMapFile();
            var paneLoader = new PaneInformationLoader(
                BrandVueDataLoader.SubsetRepository,
                (PanesRepositoryMapFile) PaneRepository,
                _commonMetadataFieldApplicator,
                _loggerFactory.CreateLogger<PaneInformationLoader>());
            paneLoader.LoadIfExists(_settings.PaneMetadataFilepath);

            PartRepository = new PartsRepositoryMapFile();
            var partLoader = new PartInformationLoader(
                BrandVueDataLoader.SubsetRepository,
                (PartsRepositoryMapFile) PartRepository,
                _commonMetadataFieldApplicator,
                _loggerFactory.CreateLogger<PartInformationLoader>());
            partLoader.LoadIfExists(_settings.PartMetadataFilepath);
        }

        //todo: 26/5/22 - JH/JR - Delete this when old reports are updated
        private void ConfigureMultiEntitySplitByForReportsCardCharts(PartsRepositorySql partRepository)
        {
            var reportPartTypes = new[] { PartType.ReportsCardText, PartType.ReportsCardChart, PartType.ReportsCardStackedMulti, PartType.ReportsCardMultiEntityMultipleChoice, PartType.ReportsTable, PartType.ReportsCardLine, PartType.ReportsCardDoughnut };
            var reportParts = partRepository.GetParts().Where(p => reportPartTypes.Any(type => type.Equals(p.PartType, StringComparison.OrdinalIgnoreCase)));
            var partsToUpdate = reportParts.Where(p => p.MultipleEntitySplitByAndFilterBy == null);
            var updatedParts = new List<PartDescriptor>();
            foreach (var part in partsToUpdate)
            {
                try
                {
                    var measure = BrandVueDataLoader.MeasureRepository.Get(part.Spec1);
                    var entityTypes = measure.EntityCombination.ToArray();

                    string splitByEntityTypeName = null;
                    EntityTypeAndInstance[] filterByEntityTypes = Array.Empty<EntityTypeAndInstance>();
                    if (entityTypes.Length == 0)
                    {
                        //default config above
                    }
                    else if (entityTypes.Length == 1)
                    {
                        splitByEntityTypeName = entityTypes.Single().Identifier;
                    }
                    else
                    {
                        if (part.DefaultSplitBy == null)
                        {
                            throw new Exception("Invalid defaultSplitBy parameter");
                        }

                        var filterByType = entityTypes.First(m => m.Identifier != part.DefaultSplitBy);

                        int? filterId = null;
                        if (!string.IsNullOrEmpty(part.Spec3))
                        {
                            if (int.TryParse(part.Spec3, out var parsedFilterId))
                            {
                                filterId = parsedFilterId;
                            }
                            else
                            {
                                throw new Exception("Invalid Spec3 parameter");
                            }
                        }

                        splitByEntityTypeName = part.DefaultSplitBy;
                        filterByEntityTypes = new[] {
                            new EntityTypeAndInstance
                        {
                            Type = filterByType.Identifier,
                            Instance = filterId
                        }};
                    }

                    var multipleEntitySplitByAndFilterBy = new MultipleEntitySplitByAndFilterBy()
                    {
                        SplitByEntityType = splitByEntityTypeName,
                        FilterByEntityTypes = filterByEntityTypes
                    };
                    part.MultipleEntitySplitByAndFilterBy = multipleEntitySplitByAndFilterBy;
                    updatedParts.Add(part);
                }
                catch (Exception)
                {
                    _logger.LogError("Unable to extract reportscardchart into MultiEntitySplitBy column", part);
                }
            }

            partRepository.UpdateMultipleEntitySplitByAndMainForPart(updatedParts);
        }
    }
}
