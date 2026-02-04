using BrandVue.EntityFramework;

namespace BrandVue.Models
{
    public class MultiEntityProfileModel : ISubsetIdProvider
    {
        public string SubsetId { get; }
        public Period Period { get; }
        public EntityInstanceRequest DataRequest { get; }
        public int ActiveEntityId { get; }
        public string[] MeasureNames { get; }
        public int[] OverriddenBaseVariableIds { get; }
        public bool IncludeMarketAverage { get; }

        public MultiEntityProfileModel(string subsetId, Period period, EntityInstanceRequest dataRequest,
            int activeEntityId, string[] measureNames, int[] overriddenBaseVariableIds, bool includeMarketAverage)
        {
            SubsetId = subsetId;
            Period = period;
            DataRequest = dataRequest;
            MeasureNames = measureNames;
            ActiveEntityId = activeEntityId;
            OverriddenBaseVariableIds = overriddenBaseVariableIds;
            IncludeMarketAverage = includeMarketAverage;
        }
    }
}