using BrandVue.SourceData;
using Newtonsoft.Json;

namespace BrandVue.PublicApi.Models
{
    public class SurveysetInfo : IEquatable<SurveysetInfo>
    {
        [JsonConstructor] //Only use in tests which make an http call
        public SurveysetInfo(DateTime earliestResponseDate, DateTime latestResponseDate) : this(earliestResponseDate.ToUtcDateOffset(), latestResponseDate.ToUtcDateOffset())
        {
        }

        public SurveysetInfo(DateTimeOffset earliestResponseDate, DateTimeOffset latestResponseDate)
        {
            EarliestResponseDate = earliestResponseDate;
            LatestResponseDate = latestResponseDate;
        }

        /// <summary>
        /// Date of the earliest survey response available.
        /// </summary>
        public DateTimeOffset EarliestResponseDate { get; }
        /// <summary>
        /// Date of the latest survey response available.
        /// </summary>
        public DateTimeOffset LatestResponseDate { get; }

        public bool Equals(SurveysetInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return EarliestResponseDate.Equals(other.EarliestResponseDate) && LatestResponseDate.Equals(other.LatestResponseDate);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SurveysetInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EarliestResponseDate.GetHashCode() * 397) ^ LatestResponseDate.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"{nameof(EarliestResponseDate)}: {EarliestResponseDate}, {nameof(LatestResponseDate)}: {LatestResponseDate}";
        }
    }
}