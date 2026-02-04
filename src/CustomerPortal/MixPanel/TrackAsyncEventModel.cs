#nullable enable
using System.Collections.Generic;

namespace CustomerPortal.MixPanel
{
    public class TrackAsyncEventModel
    {
        public TrackAsyncEventModel(VueEvents eventName,
        string distinctId,
        string ipAddress,
        Dictionary<string, object>? additionalProps = null)
        {
            EventName = eventName;
            DistinctId = distinctId;
            IpAddress = ipAddress;
            AdditionalProps = additionalProps;
        }
        public VueEvents EventName { get; }
        public string DistinctId { get; }
        public string IpAddress { get; }
        public Dictionary<string, object>? AdditionalProps { get; }
    }
}