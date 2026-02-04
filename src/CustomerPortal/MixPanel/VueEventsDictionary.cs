using System.Collections.Generic;

namespace CustomerPortal.MixPanel
{
    public static class VueEventsDictionary
    {
        public static Dictionary<VueEvents, VueEventProps> _eventsProperties = new Dictionary<VueEvents, VueEventProps>()
        {
            { VueEvents.UploadedDocument, new VueEventProps("Documents Page", "", "") },
            { VueEvents.DeletedDocument, new VueEventProps("Documents Page", "", "") },
            { VueEvents.DownloadedDocument, new VueEventProps("Documents Page", "", "") },
        };
    }
}