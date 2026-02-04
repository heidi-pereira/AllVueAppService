using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.Middleware;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Subsets;
using AverageDescriptor = BrandVue.PublicApi.Models.AverageDescriptor;

namespace BrandVue.PublicApi.Services
{
    public class ApiAverageProvider : IApiAverageProvider
    {
        private readonly IAverageDescriptorRepository _averageDescriptorRepository;
        private readonly RequestScope _requestScope;

        public ApiAverageProvider(IAverageDescriptorRepository averageDescriptorRepository, RequestScope requestScope)
        {
            _averageDescriptorRepository = averageDescriptorRepository;
            _requestScope = requestScope;
        }

        /// <summary>
        /// Get all average descriptors that are not disabled and belong in the requested subset
        /// </summary>
        public IEnumerable<AverageDescriptor> GetAllAvailableAverageDescriptors(Subset subset) =>
            GetAllAvailableSourceDataAverages(subset)
                .Select(a => new AverageDescriptor(a));

        /// <summary>
        /// Get all average descriptors that are not disabled and belong in the requested subset. These will also be filtered by supported averages for weightings calculation
        /// </summary>
        public IEnumerable<AverageDescriptor> GetSupportedAverageDescriptorsForWeightings(Subset subset) =>
            GetAllAvailableSourceDataAverages(subset).Where(IsWeightableAverage)
                .Select(a => new AverageDescriptor(a));

        private static bool IsWeightableAverage(SourceData.Averages.AverageDescriptor sourceAverage)
        {
            return sourceAverage.TotalisationPeriodUnit switch
            {
                TotalisationPeriodUnit.Day => true,
                TotalisationPeriodUnit.Month => sourceAverage.NumberOfPeriodsInAverage == 1,
                TotalisationPeriodUnit.All => true,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        /// <summary>
        /// This is same logic that BrandVue UI uses to filter averages at the top level
        /// </summary>
        private IEnumerable<SourceData.Averages.AverageDescriptor> GetAllAvailableSourceDataAverages(Subset subset) =>
            _averageDescriptorRepository.GetAllForClient(_requestScope.Organization)
                .Where(a => !a.Disabled && (a.Subset == null || a.Subset.Any(s => s.Id == subset.Id)));
    }
}