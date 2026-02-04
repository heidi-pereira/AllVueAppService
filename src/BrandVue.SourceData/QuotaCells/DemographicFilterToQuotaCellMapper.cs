using BrandVue.SourceData.Averages;

namespace BrandVue.SourceData.QuotaCells
{
    public class DemographicFilterToQuotaCellMapper : IDemographicFilterToQuotaCellMapper
    {
        private readonly IRespondentRepositorySource _respondentRepositorySource;

        public DemographicFilterToQuotaCellMapper(
            IRespondentRepositorySource respondentRepositorySource)
        {
            _respondentRepositorySource = respondentRepositorySource;
        }

        public IGroupedQuotaCells MapWeightedQuotaCellsFor(Subset datasetSelector, DemographicFilter filter)
        {
            var respondentRepository = _respondentRepositorySource.GetForSubset(datasetSelector);
            return filter.Apply(datasetSelector.Id, respondentRepository.WeightedCellsGroup);
        }

        public IGroupedQuotaCells MapQuotaCellsFor(Subset datasetSelector, DemographicFilter filter, AverageDescriptor averageDescriptor)
        {
            var respondentRepository = _respondentRepositorySource.GetForSubset(datasetSelector);
            var quotaCellsGroup = respondentRepository.GetGroupedQuotaCells(averageDescriptor);
            return filter.Apply(datasetSelector.Id, quotaCellsGroup);
        }
    }
}
