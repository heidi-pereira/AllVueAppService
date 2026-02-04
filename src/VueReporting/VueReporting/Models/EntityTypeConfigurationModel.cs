using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VueReporting.Models
{
    public class EntityTypeConfigurationModel
    {
        public EntityType EntityType { get; set; }
        public IReadOnlyCollection<EntityInstance> AllInstances { get; set; }
        public IReadOnlyCollection<EntitySet> EntitySets { get; set; }
        public string DefaultEntitySetName { get; set; }
    }
}
