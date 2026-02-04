using NJsonSchema.Annotations;

namespace BrandVue.EntityFramework.MetaData.Breaks
{
    public class CrossMeasure
    {
        public string MeasureName { get; set; }
        //an empty array should include all instances to preserve existing behaviour
        public CrossMeasureFilterInstance[] FilterInstances { get; set; } = Array.Empty<CrossMeasureFilterInstance>();
        public CrossMeasure[] ChildMeasures { get; set; } = Array.Empty<CrossMeasure>();
        public bool MultipleChoiceByValue { get; set; } = false;
        [CanBeNull]
        public string SignificanceFilterInstanceComparandName { get; set; }
    }

    public class CrossMeasureFilterInstance
    {
        //only one of these will be present, depending on whether the measure uses FilterValueMapping or not
        public string FilterValueMappingName { get; set; } = "";
        public int InstanceId { get; set; } = -1;
    }
}
