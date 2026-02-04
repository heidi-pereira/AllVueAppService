namespace BrandVue.SourceData.Models.Filters
{
    public class FilterInfoRange : FilterInfo, IEquatable<FilterInfoRange>
    {
        public int Min { get; set; }
        public int Max { get; set; }

        public FilterInfoRange(string questionId, string[] questionClassIds) : base(questionId, questionClassIds)
        {
            IncludedValuesType = IncludedValuesTypeEnum.Range;
        }

        public bool Equals(FilterInfoRange other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Min == other.Min && Max == other.Max;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FilterInfoRange) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ Min;
                hashCode = (hashCode * 397) ^ Max;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Min)}: {Min}, {nameof(Max)}: {Max}";
        }
    }
}