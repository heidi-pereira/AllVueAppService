using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(nameof(SubsetsIdOnly), false)]
    public class SubsetsIdOnly : SheetRow
    {
        public string Id { get; private set; }
    }
}