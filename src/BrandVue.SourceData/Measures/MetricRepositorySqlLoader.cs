using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vue.Common.Auth;

namespace BrandVue.SourceData.Measures
{
    public class MetricRepositorySqlLoader
    {
        private readonly IProductContext _productContext;
        private readonly IMetricFactory _metricFactory;
        private readonly IUserDataPermissionsOrchestrator _userDataPermissionsOrchestrator;
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly ILogger<IMeasureRepository> _logger;

        public MetricRepositorySqlLoader(
            IDbContextFactory<MetaDataContext> dbContextFactory,
            IProductContext productContext,
            IMetricFactory metricFactory,
            IUserDataPermissionsOrchestrator userDataPermissionsOrchestrator,
            ILoggerFactory loggerFactory)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
            _metricFactory = metricFactory;
            _logger = loggerFactory.CreateLogger<IMeasureRepository>();
            _productContext = productContext;
            _userDataPermissionsOrchestrator = userDataPermissionsOrchestrator;
        }

        public MetricRepository CreateMeasureRepository()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var metricsInContext = MetricsInContext(dbContext).AsNoTracking();
            var measureRepository = new MetricRepository(_userDataPermissionsOrchestrator);
            foreach (var metricConfiguration in metricsInContext)
            {
                //checking if can create measure by metric if not disable it.
                if (_metricFactory.TryCreateMetric(metricConfiguration, out var measure, out var errorMessage))
                {
                    if (!measureRepository.TryAdd(metricConfiguration.Name, measure))
                    {
                        _logger.LogWarning($"Duplicate metric configuration name found: {metricConfiguration.Name} {LoggingTags.Measure} {LoggingTags.Config}");
                    }
                }
                else
                {
                    _logger.LogWarning($"Following error happened while trying to create measure for {metricConfiguration.Name}: {errorMessage}  {LoggingTags.Measure} {LoggingTags.Config}");
                }
            }

            return measureRepository;
        }

        private IQueryable<MetricConfiguration> MetricsInContext(MetaDataContext context) =>
            context.MetricConfigurations.ForProductContext(_productContext);
    }
}