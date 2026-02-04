using System.Runtime.Serialization;
using BrandVue.SourceData.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BrandVue.PublicApi.Models
{
    [JsonConverter(typeof(JsonQuestionAnswerConverter))]
    [KnownType(typeof(QuestionValueAnswer))]
    [KnownType(typeof(QuestionTextAnswer))]
    [KnownType(typeof(QuestionMultipleChoiceAnswer))]
    [KnownType(typeof(QuestionUnknownAnswerType))]
    public abstract class QuestionAnswer : IEquatable<QuestionAnswer>
    {
        public AnswerTypeEnum AnswerType { get; protected set; }

        private class JsonQuestionAnswerConverter : JsonCreationConverter<QuestionAnswer>
        {
            protected override QuestionAnswer Create(Type objectType,
                JObject jObject)
            {
                string answerType = jObject.Value<string>("answerType");

                if (answerType.Equals(AnswerTypeEnum.Value.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new QuestionValueAnswer();
                }

                if (answerType.Equals(AnswerTypeEnum.Text.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new QuestionTextAnswer();
                }

                if (answerType.Equals(AnswerTypeEnum.Category.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new QuestionMultipleChoiceAnswer();
                }

                if (answerType.Equals(AnswerTypeEnum.Unknown.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new QuestionUnknownAnswerType();
                }

                throw new ArgumentException($"Unknown answer type of {jObject.Value<string>(nameof(AnswerType))}");
            }
        }

        public bool Equals(QuestionAnswer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(AnswerType, other.AnswerType);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((QuestionAnswer) obj);
        }

        public override int GetHashCode()
        {
            return (AnswerType.GetHashCode());
        }
    }
}