using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using JsonKnownTypes;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    /// <summary>
    /// This maps to what a user specified to represent a variable
    /// </summary>
    [Newtonsoft.Json.JsonConverter(typeof(JsonKnownTypesConverter<VariableDefinition>)), JsonDiscriminator(Name = "discriminator")]
    [KnownType(typeof(EvaluatableVariableDefinition))]
    [KnownType(typeof(FieldExpressionVariableDefinition))]
    [KnownType(typeof(BaseFieldExpressionVariableDefinition))]
    [KnownType(typeof(GroupedVariableDefinition))]
    [KnownType(typeof(BaseGroupedVariableDefinition))]
    [KnownType(typeof(QuestionVariableDefinition))]
    [KnownType(typeof(SingleGroupVariableDefinition))]

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
    [JsonDerivedType(typeof(EvaluatableVariableDefinition), "EvaluatableVariableDefinition")]
    [JsonDerivedType(typeof(FieldExpressionVariableDefinition), "FieldExpressionVariableDefinition")]
    [JsonDerivedType(typeof(GroupedVariableDefinition), "GroupedVariableDefinition")]
    [JsonDerivedType(typeof(QuestionVariableDefinition), "QuestionVariableDefinition")]
    [JsonDerivedType(typeof(SingleGroupVariableDefinition), "SingleGroupVariableDefinition")]
    [JsonDerivedType(typeof(BaseFieldExpressionVariableDefinition), "BaseFieldExpressionVariableDefinition")]
    [JsonDerivedType(typeof(BaseGroupedVariableDefinition), "BaseGroupedVariableDefinition")]

    public abstract class VariableDefinition
    {
    }
}
