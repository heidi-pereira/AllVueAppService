using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using JsonKnownTypes;
using NJsonSchema.Converters;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    // Newtonsoft deserialization
    [Newtonsoft.Json.JsonConverter(typeof(JsonKnownTypesConverter<VariableComponent>)), JsonDiscriminator(Name = "discriminator")]
    [KnownType(typeof(InclusiveRangeVariableComponent))]
    [KnownType(typeof(InstanceListVariableComponent))]
    [KnownType(typeof(CompositeVariableComponent))]
    [KnownType(typeof(DateRangeVariableComponent))]
    [KnownType(typeof(SurveyIdVariableComponent))]

    //Using an abstract class instead of an interface so nswag generates the subtypes correctly
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
    [JsonDerivedType(typeof(InclusiveRangeVariableComponent), "InclusiveRangeVariableComponent")]
    [JsonDerivedType(typeof(InstanceListVariableComponent), "InstanceListVariableComponent")]
    [JsonDerivedType(typeof(CompositeVariableComponent), "CompositeVariableComponent")]
    [JsonDerivedType(typeof(DateRangeVariableComponent), "DateRangeVariableComponent")]
    [JsonDerivedType(typeof(SurveyIdVariableComponent), "SurveyIdVariableComponent")]

    //For nswag until this gets merged: https://github.com/RicoSuter/NJsonSchema/pull/1675
    [JsonInheritanceConverter(typeof(VariableComponent), "discriminator")]

    public abstract class VariableComponent
    {
        public abstract bool IsValid(out string errorMessage);
    }

    public static class VariableComponentExtensions
    {
        public static IEnumerable<VariableComponent> GetDescendants(this VariableComponent node, bool leafOnly = false)
        {
            if (node is CompositeVariableComponent compositeVariableComponent)
            {
                if (!leafOnly)
                {
                    yield return compositeVariableComponent;
                }

                foreach (var c in compositeVariableComponent.CompositeVariableComponents.SelectMany(c =>
                             GetDescendants(c, leafOnly)))
                {
                    yield return c;
                }
            }
            else
            {
                yield return node;
            }
        }
    }

}
