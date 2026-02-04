using System.Threading;

namespace BrandVue.SourceData.Respondents
{
    public class RespondentRepositorySource : IRespondentRepositorySource
    {
        private readonly IRespondentRepository[] _repositories;
        private readonly IRespondentRepositoryFactory _respondentRepositoryFactory;
        private readonly DateTimeOffset? _signOffDate;
        private readonly object _lockObject = new object();

        public RespondentRepositorySource(ISubsetRepository subsetRepository, IRespondentRepositoryFactory respondentRepositoryFactory, DateTimeOffset? signOffDate = null)
        {
            _repositories = new IRespondentRepository[subsetRepository.Count];
            _respondentRepositoryFactory = respondentRepositoryFactory;
            _signOffDate = signOffDate;
        }

        public List<QuotaCellAllocationReason> QuotaCellAllocationReason(Subset subset, IProfileResponseEntity profileResponseEntity, CancellationToken cancellationToken) => _respondentRepositoryFactory.QuotaCellAllocationReason(subset, profileResponseEntity);

        public IRespondentRepository GetForSubset(Subset subset)
        {
            var repository = _repositories[subset.Index];
            if (repository == null)
            {
                lock (_lockObject)
                {
                    repository = _repositories[subset.Index];
                    if (repository == null)
                    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits - This is in a lock so only happens on one thread while the profiles are initially loaded.
                        // It shouldn't cause any actual issues other than increasing thread use: https://blog.stephencleary.com/2017/03/aspnetcore-synchronization-context.html#:~:text=You%20Can%20Block%20on%20Async%20Code%20%2D%20But%20You%20Shouldn%E2%80%99t
                        repository = _respondentRepositoryFactory.CreateRespondentRepository(subset, _signOffDate, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                        _repositories[subset.Index] = repository;
                    }
                }
            }
            
            return repository;
        }
    }
}
