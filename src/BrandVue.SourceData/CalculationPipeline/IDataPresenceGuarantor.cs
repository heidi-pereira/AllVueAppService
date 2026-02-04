using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.CalculationPipeline
{
    public interface IDataPresenceGuarantor
    {
        Task EnsureDataLoadedIntoMemory(IRespondentRepository respondentRepository, Subset subset,
            IEnumerable<Measure> measures,
            CalculationPeriod calculationPeriod, AverageDescriptor average, IFilter filter, Break[] breaks,
            CancellationToken cancellationToken);

        Task EnsureDataIsLoaded(IRespondentRepository respondentRepository, Subset subset, Measure measure,
            CalculationPeriod calculationPeriod, AverageDescriptor average, IFilter filter,
            IReadOnlyCollection<IDataTarget> targetInstances, Break[] breaks, CancellationToken cancellationToken);

        IEnumerable<ProfileResponseEntity> LoadEmptyProfiles(Subset subset,
            ResponseFieldDescriptor[] responseFieldDescriptors);
        Task EnsureDataIsLoaded<TOut>(IRespondentRepository respondentRepository, Subset subset,
            IVariable<TOut> variable, IReadOnlyCollection<IDataTarget> targetInstances, DateTimeOffset start,
            DateTimeOffset end, CancellationToken cancellationToken);
    }
}