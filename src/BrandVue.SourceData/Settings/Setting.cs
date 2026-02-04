using BrandVue.SourceData.CommonMetadata;
using Newtonsoft.Json;

namespace BrandVue.SourceData.Settings
{
    public class Setting : BaseIdentifiableWithUntypedFields, IDisableable, IEnvironmentConfigurable
    {
        [JsonProperty("name")]
        public string Key { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("environment")]
        public string[] Environment { get; set; }
        public bool Disabled { get; set; }
    }
}