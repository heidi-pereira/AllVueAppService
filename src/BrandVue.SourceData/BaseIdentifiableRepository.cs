namespace BrandVue.SourceData
{
    public abstract class BaseIdentifiableRepository<TStored>
        : EnumerableBaseRepository<TStored, int>
        where TStored : BaseIdentifiable
    {
        protected override void SetIdentity(TStored target, int identity)
        {
            target.Id = identity;
        }
    }
}
