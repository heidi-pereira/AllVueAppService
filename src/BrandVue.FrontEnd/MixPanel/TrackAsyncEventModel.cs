#nullable enable
namespace BrandVue.MixPanel
{
    public record TrackAsyncEventModel(
        VueEvents EventName,
        string DistinctId,
        string IpAddress,
        Dictionary<string, object>? AdditionalProps = null);
}