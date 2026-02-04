using BrandVue.SourceData.QuotaCells;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Test.BrandVue.SourceData.Weightings
{
    public class QuotaCellReferenceWeightingsExposePrivates
    {
        private readonly QuotaCellReferenceWeightings _quota_Cell_Reference_Weightings;

        public QuotaCellReferenceWeightingsExposePrivates(QuotaCellReferenceWeightings quotaCellReferenceWeightings)
        {
            _quota_Cell_Reference_Weightings = quotaCellReferenceWeightings;
        }
        public double TotalWeighting => _quota_Cell_Reference_Weightings.TotalWeighting;
        public IDictionary<string, WeightingValue> _weightingsByQuotaCellString
        {
            get
            {
                var myType = typeof(QuotaCellReferenceWeightings);
                FieldInfo[] fields = myType.GetFields(
                         BindingFlags.NonPublic |
                         BindingFlags.Instance);
                return (IDictionary<string, WeightingValue>)(fields.Single(x => x.FieldType.Name == typeof(IDictionary<string, WeightingValue>).Name)).GetValue(_quota_Cell_Reference_Weightings);
            }
        }
    }
}
