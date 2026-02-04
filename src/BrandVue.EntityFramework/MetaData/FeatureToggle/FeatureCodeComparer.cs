namespace BrandVue.EntityFramework.MetaData.FeatureToggle
{
    public class FeatureCodeComparer : IEqualityComparer<Feature>
    {
        public bool Equals(Feature x, Feature y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return x.FeatureCode == y.FeatureCode;
        }

        public int GetHashCode(Feature obj)
        {
            return obj.FeatureCode.GetHashCode();
        }
    }
}