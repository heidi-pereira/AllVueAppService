using JsonKnownTypes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace BrandVue.EntityFramework.Answers.Model
{
    [Newtonsoft.Json.JsonConverter(typeof(JsonKnownTypesConverter<QuestionTypeSpecificData>)), JsonDiscriminator(Name = "discriminator")]
    [KnownType(typeof(HeatMapData))]

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminator")]
    [JsonDerivedType(typeof(HeatMapData), "HeatMapData")]

    public abstract class QuestionTypeSpecificData
    {
    }

    public class HeatMapData : QuestionTypeSpecificData
    {
        public int AddClickRadiusInPixels { get; set; }
        public int MaxClicks { get; set; }
    }
}
