using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(nameof(BrandMap), false)]
    public class BrandMap : SheetRow
    {
        [Column(0)]
        public string Id { get; private set; }
        [Column(1)]
        public string Value { get; private set; }
    }
}