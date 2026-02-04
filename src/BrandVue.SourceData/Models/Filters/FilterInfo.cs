using System.Runtime.Serialization;
using BrandVue.SourceData.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BrandVue.SourceData.Models.Filters
{
    [JsonConverter(typeof(JsonIncludeValuesConverter))]
    [KnownType(typeof(FilterInfoList))]
    [KnownType(typeof(FilterInfoRange))]
    [KnownType(typeof(FilterInfoUnknown))]
    [KnownType(typeof(FilterInfoNotNull))]
    public abstract class FilterInfo : IEquatable<FilterInfo>
    {
        public IncludedValuesTypeEnum IncludedValuesType { get; protected set; }

        public string QuestionId { get; }

        public string[] QuestionClassIds { get; }

        protected FilterInfo(string questionId, string[] questionClassIds)
        {
            QuestionId = questionId;
            QuestionClassIds = questionClassIds;
        }

        private class JsonIncludeValuesConverter : JsonCreationConverter<FilterInfo>
        {
            protected override FilterInfo Create(Type objectType,
                JObject jObject)
            {
                string includedValuesType = jObject.Value<string>("includedValuesType");
                string questionId = jObject.Value<string>("questionId");
                var questionClassIds = jObject.Value<JArray>("questionClassIds").Values<string>().ToArray();

                if (includedValuesType.Equals(IncludedValuesTypeEnum.Unknown.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new FilterInfoUnknown(questionId, questionClassIds);
                }

                if (includedValuesType.Equals(IncludedValuesTypeEnum.List.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new FilterInfoList(questionId, questionClassIds);
                }

                if (includedValuesType.Equals(IncludedValuesTypeEnum.Range.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new FilterInfoRange(questionId, questionClassIds);
                }

                if (includedValuesType.Equals(IncludedValuesTypeEnum.NotNull.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return new FilterInfoNotNull(questionId, questionClassIds);
                }

                throw new ArgumentException($"Unknown included values type of {jObject.Value<string>(nameof(IncludedValuesType))}");

            }
        }

        public bool Equals(FilterInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IncludedValuesType == other.IncludedValuesType && string.Equals(QuestionId, other.QuestionId) && QuestionClassIds.SequenceEqual(other.QuestionClassIds);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FilterInfo) obj);
        }

        public override int GetHashCode()
        {
            return QuestionId != null ? QuestionId.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return $"{nameof(IncludedValuesType)}: {IncludedValuesType}, {nameof(QuestionId)}: {QuestionId}, {nameof(QuestionClassIds)}: {string.Join(", ", QuestionClassIds)}";
        }
    }
}