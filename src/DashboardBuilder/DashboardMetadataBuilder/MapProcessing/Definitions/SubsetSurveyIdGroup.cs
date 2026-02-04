
namespace DashboardMetadataBuilder.MapProcessing.Definitions
{
    public readonly struct MapSubset
    {

        public MapSubset(string subsetId)
        {
            SubsetId = subsetId;
        }

        public string SubsetId { get; }

        public static MapSubset SingleDefaultSubset { get; } = new MapSubset(nameof(SingleDefaultSubset));
    }
}