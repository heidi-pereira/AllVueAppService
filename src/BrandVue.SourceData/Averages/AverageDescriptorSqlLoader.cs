using BrandVue.EntityFramework.MetaData.Averages;

namespace BrandVue.SourceData.Averages
{
    public class AverageDescriptorSqlLoader : IAverageDescriptorSqlLoader
    {
        private readonly AverageDescriptorRepository _averageDescriptorRepository;
        private readonly IProductContext _productContext;
        private readonly IAverageConfigurationRepository _averageConfigurationRepository;
        private readonly ISubsetRepository _subsetRepository;
        private readonly AppSettings _settings;

        public AverageDescriptorSqlLoader(
            AverageDescriptorRepository averageDescriptorRepository,
            IProductContext productContext,
            IAverageConfigurationRepository averageConfigurationRepository,
            ISubsetRepository subsetRepository,
            AppSettings settings)
        {
            _averageDescriptorRepository = averageDescriptorRepository;
            _productContext = productContext;
            _averageConfigurationRepository = averageConfigurationRepository;
            _subsetRepository = subsetRepository;
            _settings = settings;
        }

        public void Load(AverageDescriptorMapFileLoader temporaryMapFileLoader, string fullyQualifiedPathToCsvDataFile)
        {
            var loadedAverages = _averageConfigurationRepository.GetAll()
                .Select(AverageDescriptorFrom)
                .ToList();
            if (loadedAverages.All(a => a.Disabled))
            {
                DefaultAverageRepositoryData.CopyFallbackAveragesToRealRepo(_averageDescriptorRepository, _productContext);
            }
            else
            {
                DefaultAverageRepositoryData.AddCustomPeriodAverages(_averageDescriptorRepository, _productContext.GenerateFromSurveyIds);
            }

            foreach (var average in loadedAverages)
            {
                _averageDescriptorRepository.Add(average);
            }
        }

        public AverageDescriptor AverageDescriptorFrom(AverageConfiguration configuration)
        {
            var from = new AverageDescriptor();
            PopulateAverageDescriptorFrom(from, configuration);
            return from;
        }

        public void PopulateAverageDescriptorFrom(AverageDescriptor toDescriptor,
            AverageConfiguration fromConfiguration)
        {
            var subsetIds = fromConfiguration.SubsetIds.Length == 0 ? null : fromConfiguration.SubsetIds;
            toDescriptor.AverageId = fromConfiguration.AverageId;
            toDescriptor.DisplayName = fromConfiguration.DisplayName;
            toDescriptor.Order = fromConfiguration.Order;
            toDescriptor.Group = fromConfiguration.Group;
            toDescriptor.TotalisationPeriodUnit = fromConfiguration.TotalisationPeriodUnit;
            toDescriptor.NumberOfPeriodsInAverage = fromConfiguration.NumberOfPeriodsInAverage;
            toDescriptor.WeightingMethod = fromConfiguration.WeightingMethod;
            toDescriptor.WeightAcross = fromConfiguration.WeightAcross;
            toDescriptor.AverageStrategy = fromConfiguration.AverageStrategy;
            toDescriptor.MakeUpTo = fromConfiguration.MakeUpTo;
            toDescriptor.WeightingPeriodUnit = fromConfiguration.WeightingPeriodUnit;
            toDescriptor.IncludeResponseIds = fromConfiguration.IncludeResponseIds;
            toDescriptor.IsDefault = fromConfiguration.IsDefault;
            toDescriptor.AllowPartial = fromConfiguration.AllowPartial;
            toDescriptor.Disabled = fromConfiguration.Disabled;
            toDescriptor.AuthCompanyShortCode = fromConfiguration.AuthCompanyShortCode;
            toDescriptor.Subset = subsetIds?
                .Where(_subsetRepository.HasSubset)
                .Select(_subsetRepository.Get)
                .ToArray();
        }

        public void AddOrUpdate(AverageConfiguration average)
        {
            var existingConfig = _averageConfigurationRepository.Get(average.Id);
            if (existingConfig is not null && existingConfig.AverageId != average.AverageId)
            {
                Remove(existingConfig);
            }

            if (_averageDescriptorRepository.TryGet(average.AverageId, out var existing))
            {
                _averageConfigurationRepository.Update(average);
                PopulateAverageDescriptorFrom(existing, average);
            }
            else
            {
                _averageConfigurationRepository.Create(average);
                _averageDescriptorRepository.Add(AverageDescriptorFrom(average));
            }
        }

        public void Remove(AverageConfiguration average)
        {
            _averageConfigurationRepository.Delete(average.Id);
            _averageDescriptorRepository.Remove(average.AverageId);
        }

        public static AverageConfiguration AverageConfigurationFrom(AverageDescriptor descriptor, IProductContext productContext)
        {
            return new AverageConfiguration
            {
                ProductShortCode = productContext.ShortCode,
                SubProductId = productContext.SubProductId,
                AverageId = descriptor.AverageId,
                DisplayName = descriptor.DisplayName,
                Order = descriptor.Order,
                Group = descriptor.Group,
                TotalisationPeriodUnit = descriptor.TotalisationPeriodUnit,
                NumberOfPeriodsInAverage = descriptor.NumberOfPeriodsInAverage,
                WeightingMethod = descriptor.WeightingMethod,
                WeightAcross = descriptor.WeightAcross,
                AverageStrategy = descriptor.AverageStrategy,
                MakeUpTo = descriptor.MakeUpTo,
                WeightingPeriodUnit = descriptor.WeightingPeriodUnit,
                IncludeResponseIds = descriptor.IncludeResponseIds,
                IsDefault = descriptor.IsDefault,
                AllowPartial = descriptor.AllowPartial,
                Disabled = descriptor.Disabled,
                SubsetIds = descriptor.Subset?.Select(s => s.Id).ToArray() ?? Array.Empty<string>(),
                AuthCompanyShortCode = descriptor.AuthCompanyShortCode
            };
        }
    }
}
