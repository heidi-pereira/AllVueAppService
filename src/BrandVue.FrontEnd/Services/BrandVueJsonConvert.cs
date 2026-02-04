using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace BrandVue.Services
{
    public static class BrandVueJsonConvert
    {
        public static readonly JsonSerializerSettings Settings = InitializeSettings(new JsonSerializerSettings());

        public static JsonSerializerSettings InitializeSettings(JsonSerializerSettings jsonSerializerSettings)
        {
            jsonSerializerSettings.ContractResolver = new CamelCaseResolverIgnoreDictionaryKeys();
            // For attributes which are set to null, do not serialise their name into the output to reduce payload size (sometimes by quite a lot)
            // nswag generated typescript seems happy enough to default to null anyhow.
            jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            jsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            jsonSerializerSettings.Converters = new List<JsonConverter> {new StringEnumConverter()};
            jsonSerializerSettings.MaxDepth= 40;

            return jsonSerializerSettings;
        }

        private class CamelCaseResolverIgnoreDictionaryKeys : CamelCasePropertyNamesContractResolver
        {
            protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
            {
                JsonDictionaryContract contract = base.CreateDictionaryContract(objectType);
                contract.DictionaryKeyResolver = propertyName => propertyName;
                return contract;
            }
        }
    }
}
