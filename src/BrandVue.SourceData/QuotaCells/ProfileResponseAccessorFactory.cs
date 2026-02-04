using System.Collections.Concurrent;

namespace BrandVue.SourceData.QuotaCells
{
    internal class ProfileResponseAccessorFactory : IProfileResponseAccessorFactory
    {
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly ConcurrentDictionary<Subset, Lazy<ProfileResponseAccessor>> _accessors = new();

        internal ProfileResponseAccessorFactory(IRespondentRepositorySource respondentRepositorySource) =>
            _respondentRepositorySource = respondentRepositorySource;

        /// <remarks>The add function can run in parallel, so use Lazy which locks around value initialization</remarks>
        public IProfileResponseAccessor GetOrCreate(Subset subset) =>
            _accessors.GetOrAdd(subset, _ => new (() => LoadAllProfiles(subset))).Value; 

        /// <summary>Can be very slow (30s+ for brandvues) since it has to load profile data for every respondent to use in weightings</summary>
        private ProfileResponseAccessor LoadAllProfiles(Subset subset)
        {
            var allProfiles = _respondentRepositorySource.GetForSubset(subset);
            return new(allProfiles, subset);
        }
    }
}