using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData
{
    public abstract class ReasonablyResilientBaseIdentifiableLoader<TWidgeroo>
        : ReasonablyResilientBaseLoader<TWidgeroo, int> where TWidgeroo : class
    {
        private const string Property_Id = "id";

        protected ReasonablyResilientBaseIdentifiableLoader(
            BaseRepository<TWidgeroo, int> baseRepository,
            Type loaderSubclass, ILogger logger)
            : base(baseRepository, loaderSubclass, logger)
        {
        }

        protected override string IdentityPropertyName => Property_Id;

        protected override int GetIdentity(
            string[] currentRecord,
            int identityFieldIndex)
        {
            return int.Parse(currentRecord[identityFieldIndex]);
        }
    }
}
