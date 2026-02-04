using Aspose.Cells;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(nameof(CommentSource), false, 3)]
    public class CommentSource : SheetRow
    {
        [Column(0)]
        public int RespId { get; private set; }
    }
}