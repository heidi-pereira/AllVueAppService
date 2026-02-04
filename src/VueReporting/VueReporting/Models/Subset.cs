using Newtonsoft.Json;

namespace VueReporting.Models
{
    [JsonObject(ItemRequired = Required.Always)]
    public class Subset
    {
        public string Id { get; set; }
    }
}