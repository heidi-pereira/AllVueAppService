namespace BrandVue.SourceData
{
    public interface IAddableRepository<TStored, TIdentity> where TStored : class
    {
        /// <remarks>
        /// Not used during the normal load process, allows manual adding if the id is not yet in use
        /// Returns true if added
        /// </remarks>
        bool TryAdd(TIdentity objectId, TStored obj);
    }
}