namespace BrandVue.SourceData.Models.Filters
{
    public class FilterInfoList : FilterInfo, IEquatable<FilterInfoList>
    {
        public int[] IncludeList { get; set; }

        public FilterInfoList(string questionId, string[] questionClassIds): base(questionId, questionClassIds)
        {
            IncludedValuesType = IncludedValuesTypeEnum.List;
        }

        public bool Equals(FilterInfoList other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return IncludeList.SequenceEqual(other.IncludeList);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FilterInfoList) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (IncludeList != null ? IncludeList.GetHashCode() : 0);
            }
        }

        public override string ToString()
        {
            return $"{nameof(IncludeList)}: {string.Join("|", IncludeList)}";
        }
    }
}