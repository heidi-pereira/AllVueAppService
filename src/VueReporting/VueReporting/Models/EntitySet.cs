using System.Collections.Generic;
using Newtonsoft.Json;

namespace VueReporting.Models
{
    public class EntitySet
    {
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }
        [JsonProperty(Required = Required.Always)]
        public long[] InstanceIds { get; set; }
        public string Organisation { get; set; }
        public long? MainInstanceId { get; set; }
        public string MainInstanceName { get; set; }
    }


    [JsonObject(ItemRequired = Required.Always)]
    public class EntityInstance
    {
        public string Name { get; set; }
        public virtual long Id { get; set; }
    }
}
