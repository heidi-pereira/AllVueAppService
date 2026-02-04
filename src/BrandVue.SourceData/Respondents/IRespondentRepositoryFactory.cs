using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.Respondents
{
    public record QuotaCellAllocationReason(string Dimension, int? AnswerValue, string Reason);

    public interface IRespondentRepositoryFactory
    {
        Task<IRespondentRepository> CreateRespondentRepository(Subset subset, DateTimeOffset? signOffDate,
            CancellationToken cancellationToken);

        public List<QuotaCellAllocationReason> QuotaCellAllocationReason(Subset subset, IProfileResponseEntity profileResponseEntity);

    }
}