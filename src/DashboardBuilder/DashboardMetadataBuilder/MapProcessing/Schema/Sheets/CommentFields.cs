using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(nameof(CommentFields), false)]
    public class CommentFields : SheetRow
    {
        public string Field { get; private set; }
        public int DashboardFieldId { get; private set; }
    }
}