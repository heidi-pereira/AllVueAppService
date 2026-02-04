using System.Diagnostics;
using System.Threading;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.Services;
using BrandVue.SourceData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Vue.AuthMiddleware;
using Vue.Common.App_Start;
using Vue.Common.AuthApi;
using Vue.Common.FeatureFlags;

namespace BrandVue
{
    /// <summary>
    /// All types containing survey/dashboard specific data are retrieved via here.
    /// For Vues such as eatingout, there will just be a single loader, for surveyvue there will be one per subproduct.
    /// There's a subproduct for each numeric surveyid by default, and other named subproducts can be added.
    /// </summary>
    class PerRequestSubProductLoaderDecorator : IBrandVueDataLoader, IUiBrandVueDataLoader, IEagerlyLoadable<IBrandVueDataLoader>, ISubProductSecurityRestrictionsProvider, IInvalidatableLoaderCache, ISubProductBrowserCacheKeyTrackerProvider
    {
        private readonly IMemoryCache _subProductLoaderCache;
        private readonly IMemoryCache _securityRestrictionsProviderCache;
        private readonly IMemoryCache _subProductBrowserCacheKeyTrackerCache;
        private readonly AppSettings _appSettings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRequestScopeAccessor _requestScopeAccessor;
        private readonly IDbContextFactory<MetaDataContext> _metaDataContextFactory;
        private readonly IAnswerDbContextFactory _answersDbContextFactory;
        private readonly IAuthApiClient _authApiClient;
        private readonly IPermissionService _permissionService;
        private readonly IWeightingPlanRepository _weightingPlanRepository;
        private readonly ProductContextProvider _productContextProvider;
        private readonly IChoiceSetReader _choiceSetReader;

        public PerRequestSubProductLoaderDecorator(AppSettings appSettings,
            ILoggerFactory loggerFactory,
            IDbContextFactory<MetaDataContext> metaDataContextFactory,
            IAnswerDbContextFactory answerDbContextFactory,
            IRequestScopeAccessor requestScopeAccessor,
            IAuthApiClient authApiClient,
            IPermissionService permissionService,
            IWeightingPlanRepository weightingPlanRepository,
            IRequestAwareFactory<UiBrandVueDataLoader> uiBrandVueDataLoaderFactory,
            IChoiceSetReader choiceSetReader)
        {
            _appSettings = appSettings;
            _loggerFactory = loggerFactory;
            _requestScopeAccessor = requestScopeAccessor;
            _subProductLoaderCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 20 });
            _securityRestrictionsProviderCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 20 });
            _subProductBrowserCacheKeyTrackerCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 500 });
            _metaDataContextFactory = metaDataContextFactory;
            _answersDbContextFactory = answerDbContextFactory;
            _authApiClient = authApiClient;
            _permissionService = permissionService;
            _productContextProvider = new ProductContextProvider(_appSettings, new AnswerDbContextFactory(_appSettings.ConnectionString), loggerFactory.CreateLogger<ProductContextProvider>());
            _weightingPlanRepository = weightingPlanRepository;
            _uiBrandVueDataLoaderFactory = uiBrandVueDataLoaderFactory;
            _choiceSetReader = choiceSetReader;
        }

        public void InvalidateCacheEntry(string productName, string subProductId)
        {
            var cacheKey = GetCacheKey(productName, subProductId);
            _subProductLoaderCache.Remove(cacheKey);
            _subProductBrowserCacheKeyTrackerCache.Remove(cacheKey);
        }

        public void InvalidateQuestions(IList<int[]> surveyIdsForEachSubset)
        {
            foreach (int[] surveyIds in surveyIdsForEachSubset)
            {
                _choiceSetReader.InvalidateCache(surveyIds);
            }
        }

        public void EagerlyLoad()
        {
            // Eagerly load things in real environments rather than make the first user wait
            // If a dev intended to start the app, they'll launch a browser, but we don't want various dev tools (nswag, EF Core) to run this.
            if (_appSettings.AppDeploymentEnvironment != AppSettings.LiveEnvironmentName) return;

            var subProductsToEagerlyLoad = _productContextProvider.GetSubProductsToEagerlyLoad(_appSettings.ProductToLoadDataFor);
            var logger = _loggerFactory.CreateLogger("EagerlyLoad");
            _ = Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => // Put this in the background so app can start serving pages that may have already been requested
            {
                foreach (var subProduct in subProductsToEagerlyLoad)
                {
                    try
                    {
                        var loader = GetOrLoad(subProduct).BrandVueDataLoader;
                        // Feel free to change this locally if you want to load all subsets much faster at the expense of doing anything else for a few minutes
                        loader.SubsetRepository.AsParallel().WithDegreeOfParallelism(1).ForAll(subset =>
                        {
                            EagerlyLoad(subProduct, subset, loader, logger);
                        });
                    }
                    catch (Exception)
                    {
                        //Calls to EagerlyLoad shouldn't throw. If the CreateRespondentRepository method throws then the logging will be handled there.
                        //The exception will be rethrown for subsequent calls to Lazy<UiBrandVueDataLoader> for future requests for that specific loader
                    }
                }
            }, TaskScheduler.Default);
        }

        private void EagerlyLoad(string subProduct, Subset subset, IBrandVueDataLoader loader, ILogger logger)
        {
            using var _ = logger.BeginScope($"Product: {_appSettings.ProductToLoadDataFor}, Subproduct: {subProduct}, Subset {subset}");
            var sw = new Stopwatch();
            sw.Start();
            loader.ProfileResponseAccessorFactory.GetOrCreate(subset);
            sw.Stop();
            logger.LogInformation($"Loaded subset in {sw.ElapsedMilliseconds}ms");
        }

        private IBrandVueDataLoader RequestedBrandVueDataLoader => RequestedUiBrandVueDataLoader.BrandVueDataLoader;
        private UiBrandVueDataLoader RequestedUiBrandVueDataLoader => GetOrLoad(_requestScopeAccessor.RequestScope.SubProduct);

        private readonly object _cacheLock = new();
        private readonly IRequestAwareFactory<UiBrandVueDataLoader> _uiBrandVueDataLoaderFactory;

        private UiBrandVueDataLoader GetOrLoad(string subProductId)
        {
            string cacheKey = GetCacheKey(_appSettings.ProductToLoadDataFor, subProductId);

            var uiBrandVueDataLoader = GetOrCreate();

            if (!uiBrandVueDataLoader.BrandVueDataLoader.LazyDataLoader.DataLimiter.RequiresReload) return uiBrandVueDataLoader;

            // There's a race condition here where another thread could have just run GetOrCreate between these two lines, and then this thread removes it.
            // It would cause two threads to do the reload, but probably isn't worth the complexity of locking to avoid
            _subProductLoaderCache.Remove(cacheKey);
            return GetOrCreate();

            UiBrandVueDataLoader GetOrCreate()
            {
                Lazy<UiBrandVueDataLoader> lazy;

                if (!_subProductLoaderCache.TryGetValue(cacheKey, out lazy))
                {
                    lock (_cacheLock)
                    {
                        lazy = _subProductLoaderCache.GetOrCreate(cacheKey, entry =>
                            CreateLazyLoader(entry,
                                _productContextProvider.ProvideProductContext(_appSettings.ProductToLoadDataFor,
                                    subProductId))
                        );
                    }
                }

                try
                {
                    return lazy.Value;
                }
                catch (Exception)
                {
                    //If there's an exception on load, we want to remove the loader from the cache & try again.
                    //Otherwise the exception gets cached by the Lazy loader
                    _subProductLoaderCache.Remove(cacheKey);
                    throw;
                }
            }
        }

        private ISubProductBrowserCacheKeyTracker GetOrCreateCacheKeyTracker(string subProductId)
        {
            string cacheKey = GetCacheKey(_appSettings.ProductToLoadDataFor, subProductId);
            return _subProductBrowserCacheKeyTrackerCache.GetOrCreate(cacheKey, entry =>
            {
                entry.Size = 1;
                return new SubProductBrowserCacheKeyTracker();
            });
        }

        private ISubProductSecurityRestrictionsProvider GetOrCreateSecurityRestrictionsProvider(string subProductId)
        {
            string cacheKey = GetCacheKey(_appSettings.ProductToLoadDataFor, subProductId);

            var lazy = _securityRestrictionsProviderCache.GetOrCreate(cacheKey, entry =>
                CreateLazySecurityRestrictionsProvider(entry, _productContextProvider.ProvideProductContext(_appSettings.ProductToLoadDataFor, subProductId))
            );

            try
            {
                return lazy.Value;
            }
            catch (Exception)
            {
                //If there's an exception on load, we want to remove the provider from the cache & try again.
                //Otherwise the exception gets cached by the Lazy loader
                _securityRestrictionsProviderCache.Remove(cacheKey);
                throw;
            }
        }

        private string GetCacheKey(string productName, string subProductId) => subProductId ?? productName;

        private Lazy<UiBrandVueDataLoader> CreateLazyLoader(ICacheEntry entry, IProductContext productContext)
        {
            entry.Size = 1;//Could use number of respondents if we need to be more granular
            if (!productContext.KeepInMemory)
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);
                entry.Priority = CacheItemPriority.Low;
            }
            else
            {
                entry.Priority = CacheItemPriority.NeverRemove;
            }
            return new Lazy<UiBrandVueDataLoader>(() => CreateLoader(entry, productContext));
        }

        private Lazy<ISubProductSecurityRestrictionsProvider> CreateLazySecurityRestrictionsProvider(ICacheEntry entry,
            IProductContext productContext)
        {
            entry.Size = 1;
            if (!productContext.KeepInMemory)
            {
                entry.SlidingExpiration = TimeSpan.FromHours(1);
                entry.Priority = CacheItemPriority.Low;
            }
            else
            {
                entry.Priority = CacheItemPriority.NeverRemove;
            }

            return new Lazy<ISubProductSecurityRestrictionsProvider>(() =>
                new SubProductSecurityRestrictionsProvider(_appSettings, productContext, _authApiClient,
                    _permissionService, _loggerFactory.CreateLogger<SubProductSecurityRestrictionsProvider>()));
        }

        private UiBrandVueDataLoader CreateLoader(ICacheEntry entry, IProductContext productContext)
        {
            var loader = _uiBrandVueDataLoaderFactory.Create();
            Load(loader, $"Survey {entry.Key}", _loggerFactory);
            GetOrCreateCacheKeyTracker(productContext.SubProductId).Update();
            return loader;
        }

        private static void Load(UiBrandVueDataLoader loader, string loaderLogDescriptor, ILoggerFactory loggerFactory)
        {
            try
            {
                //Ensure metadata loaded
                loader.LoadBrandVueMetadata();
                loader.BrandVueDataLoader.LoadBrandVueData();
            }
            catch (AggregateException e)
            {
                var logger = loggerFactory.CreateLogger<IoCConfig>();
                if (e.InnerExceptions != null && e.InnerExceptions.Count == 1)
                {
                    logger.LogCritical(e.InnerExceptions[0], "Data for '{DataLoaderKey}' not loaded, site will be unavailable",
                        loaderLogDescriptor);
                }
                else
                {
                    logger.LogCritical(e, "Data for '{DataLoaderKey}' not loaded, site will be unavailable", loaderLogDescriptor);
                }

                throw;
            }
            catch (Exception e)
            {
                var logger = loggerFactory.CreateLogger<IoCConfig>();
                logger.LogCritical(e, "Data for '{DataLoaderKey}' not loaded, site will be unavailable", loaderLogDescriptor);
                throw;
            }
        }

        public ISubProductBrowserCacheKeyTracker SubProductBrowserCacheKeyTracker => GetOrCreateCacheKeyTracker(_requestScopeAccessor.RequestScope.SubProduct);

        public IResponseFieldManager ResponseFieldManager => RequestedBrandVueDataLoader.ResponseFieldManager;

        public IQuotaCellDescriptionProvider QuotaCellDescriptionProvider => RequestedBrandVueDataLoader.QuotaCellDescriptionProvider;

        public IQuotaCellReferenceWeightingRepository QuotaCellReferenceWeightingRepository => RequestedBrandVueDataLoader.QuotaCellReferenceWeightingRepository;

        public ISubsetRepository SubsetRepository => RequestedBrandVueDataLoader.SubsetRepository;

        public IAverageConfigurationRepository AverageConfigurationRepository => RequestedBrandVueDataLoader.AverageConfigurationRepository;

        public IAverageDescriptorRepository AverageDescriptorRepository => RequestedBrandVueDataLoader.AverageDescriptorRepository;

        public IFilterRepository FilterRepository => RequestedBrandVueDataLoader.FilterRepository;

        public ILoadableEntityInstanceRepository EntityInstanceRepository => RequestedBrandVueDataLoader.EntityInstanceRepository;

        public IMetricCalculationOrchestrator Calculator => RequestedBrandVueDataLoader.Calculator;

        public IMeasureRepository MeasureRepository => RequestedBrandVueDataLoader.MeasureRepository;

        public IRespondentRepositorySource RespondentRepositorySource => RequestedBrandVueDataLoader.RespondentRepositorySource;

        public IInstanceSettings InstanceSettings => RequestedBrandVueDataLoader.InstanceSettings;

        public ILazyDataLoader LazyDataLoader => RequestedBrandVueDataLoader.LazyDataLoader;

        public ILoadableEntitySetRepository EntitySetRepository => RequestedBrandVueDataLoader.EntitySetRepository;

        public ILoadableEntityTypeRepository EntityTypeRepository => RequestedBrandVueDataLoader.EntityTypeRepository;

        public IMetricFactory MetricFactory => RequestedBrandVueDataLoader.MetricFactory;

        public IMetricConfigurationRepository MetricConfigurationRepository => RequestedBrandVueDataLoader.MetricConfigurationRepository;

        public IFieldExpressionParser FieldExpressionParser => RequestedBrandVueDataLoader.FieldExpressionParser;

        public IVariableConfigurationRepository VariableConfigurationRepository => RequestedBrandVueDataLoader.VariableConfigurationRepository;

        public IProfileResultsCalculator ProfileResultsCalculator => RequestedBrandVueDataLoader.ProfileResultsCalculator;

        public ISampleSizeProvider SampleSizeProvider => RequestedBrandVueDataLoader.SampleSizeProvider;

        public IProfileResponseAccessorFactory ProfileResponseAccessorFactory => RequestedBrandVueDataLoader.ProfileResponseAccessorFactory;
        public IEntitySetConfigurationRepository EntitySetConfigurationRepository => RequestedBrandVueDataLoader.EntitySetConfigurationRepository;

        public IRespondentDataLoader RespondentDataLoader => RequestedBrandVueDataLoader.RespondentDataLoader;

        public void LoadBrandVueData() => RequestedBrandVueDataLoader.LoadBrandVueData();

        public void LoadBrandVueMetadata() => RequestedBrandVueDataLoader.LoadBrandVueMetadata();

        public IPagesRepository PageRepository => RequestedUiBrandVueDataLoader.PageRepository;

        public IPanesRepository PaneRepository => RequestedUiBrandVueDataLoader.PaneRepository;

        public IPartsRepository PartRepository => RequestedUiBrandVueDataLoader.PartRepository;
        public async Task<ISubProductSecurityRestrictions> GetSecurityRestrictions(CancellationToken cancellationToken) => await GetOrCreateSecurityRestrictionsProvider(_requestScopeAccessor.RequestScope.SubProduct).GetSecurityRestrictions(cancellationToken);

        public IQuestionTypeLookupRepository QuestionTypeLookupRepository => RequestedBrandVueDataLoader.QuestionTypeLookupRepository;
        public IResponseRepository TextResponseRepository => RequestedBrandVueDataLoader.TextResponseRepository;
        public IResponseWeightingRepository ResponseWeightingRepository => RequestedBrandVueDataLoader.ResponseWeightingRepository;
        public IFeatureToggleService FeatureToggleService => RequestedBrandVueDataLoader.FeatureToggleService;
        public ITextCountCalculatorFactory TextCountCalculatorFactory => RequestedBrandVueDataLoader.TextCountCalculatorFactory;
    }
}
