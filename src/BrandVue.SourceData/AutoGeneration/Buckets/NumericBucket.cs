using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.SourceData.AutoGeneration.Buckets
{
    public class NumericBucket : IntegerInclusiveBucket
    {
        public string GetBucketDescriptor(string format)
        {
            if (MaximumInclusive == null)
            {
                return $"{MinimumInclusive}+";
            }
            if (MinimumInclusive == MaximumInclusive)
            {
                return MinimumInclusive.ToString();
            }
            return $"{MinimumInclusive} - {MaximumInclusive}";
        }

        public VariableRangeComparisonOperator GetBucketOperator()
        {
            if (MaximumInclusive == null)
            {
                return VariableRangeComparisonOperator.GreaterThan;
            }
            if (MinimumInclusive == MaximumInclusive)
            {
                return VariableRangeComparisonOperator.Exactly;
            }
            return VariableRangeComparisonOperator.Between;
        }
    }
}