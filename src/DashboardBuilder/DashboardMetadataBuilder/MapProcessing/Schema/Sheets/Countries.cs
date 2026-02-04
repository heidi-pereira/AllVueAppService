using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(nameof(Countries), false)]
    public class Countries : SheetRow
    {
        [Column(0)]
        public int Id { get; private set; }
        [Column(1)]
        public string Value { get; private set; }
    }
}