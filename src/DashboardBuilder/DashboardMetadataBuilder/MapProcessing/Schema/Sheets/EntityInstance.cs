using System.ComponentModel;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    public class EntityInstance : SheetRow
    {
        public int Id { get; internal set; }
        public string Name { get; internal set; }
        [DefaultValue("")]
        public string Aliases { get; internal set; }
    }
}
