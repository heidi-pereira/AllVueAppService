using System.Linq;
using Aspose.Cells;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(SheetName, false)]
    public class Dimensions : SheetRow
    {
        public const string SheetName = nameof(Dimensions);
        public int Id { get; private set; }
    }
}