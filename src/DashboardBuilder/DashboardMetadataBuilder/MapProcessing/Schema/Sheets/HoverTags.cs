using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(nameof(HoverTags), false, 0)]
    public class HoverTags : SheetRow
    {
        [Column(0)]
        public string TagName { get; private set; }
        [Column(1)]
        public string Table { get; private set; }
    }
}