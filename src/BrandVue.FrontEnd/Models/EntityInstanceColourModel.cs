namespace BrandVue.Models
{
    public class EntityInstanceColourModel
    {
        public long InstanceId { get; }
        public string Colour { get; }

        public EntityInstanceColourModel(long instanceId, string colour)
        {
            InstanceId = instanceId;
            Colour = colour;
        }
    }

    public class NamedInstanceColourModel : EntityInstanceColourModel
    {
        public string InstanceName { get; }

        public NamedInstanceColourModel(long instanceId, string instanceName, string colour) : base(instanceId, colour)
        {
            InstanceName = instanceName;
        }
    }
}