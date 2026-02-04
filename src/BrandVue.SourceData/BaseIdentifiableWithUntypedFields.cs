namespace BrandVue.SourceData
{
    public abstract class BaseIdentifiableWithUntypedFields : BaseIdentifiable
    {
        protected BaseIdentifiableWithUntypedFields() : base()
        {
            Fields = new Dictionary<string, object>();
        }

        public IDictionary<string, object> Fields { get; private set; }
    }
}
