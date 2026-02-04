using Newtonsoft.Json;

namespace BrandVue.SourceData
{
    public abstract class BaseIdentifiable : IIdentifiable
    {
        protected BaseIdentifiable() {}

        [JsonProperty("id")]
        public virtual int Id { get; set; }
    }
}
