namespace BrandVue.EntityFramework.MetaData.BaseSizes
{
    public class BaseExpressionDefinition
    {
        public BaseDefinitionType BaseType { get; set; } = BaseDefinitionType.SawThisQuestion;
        public string BaseMeasureName { get; set; }
        public int? BaseVariableId { get; set; } = null;
    }

    public enum BaseDefinitionType : byte
    {
        AllRespondents,
        SawThisQuestion,
        SawThisChoice,
    }
}
