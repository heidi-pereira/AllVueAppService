using System.ComponentModel;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(nameof(Settings))]
    public class Settings : SheetRow
    {
        public string Setting { get; private set; }
        public string Value { get; private set; }
        [DefaultValue(null)]
        public string Environment { get; set; }
    }
}