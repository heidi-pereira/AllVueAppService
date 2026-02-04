using JetBrains.Annotations;

namespace BrandVue.Models
{
    public class MeasureFilterRequestModel
    {
        public MeasureFilterRequestModel(string measureName, Dictionary<string, int[]> entityInstances, bool invert,
            bool treatPrimaryValuesAsRange, [CanBeNull] int[] values = null)
        {
            MeasureName = measureName;
            Invert = invert;
            EntityInstances = entityInstances;
            // When there are no values set it means "any". So just include the whole range
            Values = values ?? [int.MinValue, int.MaxValue];
            TreatPrimaryValuesAsRange = treatPrimaryValuesAsRange || values == null;
        }
        public string MeasureName { get; }
        public Dictionary<string, int[]> EntityInstances { get; }
        public int[] Values { get; }
        public bool Invert { get; }
        public bool TreatPrimaryValuesAsRange { get; }
    }
}