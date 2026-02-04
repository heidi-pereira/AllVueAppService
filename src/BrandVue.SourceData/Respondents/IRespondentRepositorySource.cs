using System.Threading;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Respondents
{
    public interface IRespondentRepositorySource
    {
        IRespondentRepository GetForSubset(Subset subset);
        public List<QuotaCellAllocationReason> QuotaCellAllocationReason(Subset subset, IProfileResponseEntity profileResponseEntity, CancellationToken cancellationToken);
    }
}