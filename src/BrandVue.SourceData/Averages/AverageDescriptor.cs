using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.CommonMetadata;
using Newtonsoft.Json;

namespace BrandVue.SourceData.Averages
{
    /// <remarks>
    /// Warning: "Descriptor" is used by all the PublicApi types, but this is not one of those as you can see from the namespace.
    /// </remarks>
    public class AverageDescriptor : BaseMetadataEntity
    {
        public string AverageId { get; set; }
        public string DisplayName { get; set; }
        public int Order { get; set; }
        public string [] Group { get; set; }
        public TotalisationPeriodUnit TotalisationPeriodUnit { get; set; }
        public int NumberOfPeriodsInAverage { get; set; }
        public WeightingMethod WeightingMethod { get; set; }
        public WeightAcross WeightAcross { get; set; }
        public AverageStrategy AverageStrategy { get; set; }
        public MakeUpTo MakeUpTo { get; set; }
        public WeightingPeriodUnit WeightingPeriodUnit { get; set; }
        public bool IncludeResponseIds { get; set; }
        public int InternalIndex { get; set; }
        public bool IsDefault { get; set; }
        public bool AllowPartial { get; set; }
        public string AuthCompanyShortCode { get; set; }

        public AverageDescriptor ShallowCopy()
        {
            return (AverageDescriptor) MemberwiseClone();
        }

        public bool IsCustom() => AverageId == AverageIds.CustomPeriod  || AverageId == AverageIds.CustomPeriodNotWeighted;

        /// <summary>
        /// True if users should not be able to select this average in dashboard average dropdowns.
        /// </summary>
        public bool IsHiddenFromUsers { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }
}
