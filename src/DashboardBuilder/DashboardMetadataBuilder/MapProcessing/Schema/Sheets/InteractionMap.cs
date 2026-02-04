using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    /// <summary>
    /// Used for Renault
    /// </summary>
    [Sheet("Map", false, 2)]
    public class InteractionMap : SheetRow
    {
        [Column(0)]
        public int InteractionId { get; private set; }
        [Column(2)]
        public string Importance { get; private set; }
        [Column(3)]
        public string Delivery { get; private set; }
        [Column(4)]
        public string GoodComment { get; private set; }
        [Column(5)]
        public string PoorComment { get; private set; }
        [Column(6)]
        public int FieldId { get; private set; }
        [Column(7)]
        public string Description { get; private set; }
    }
}