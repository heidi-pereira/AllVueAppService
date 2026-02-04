using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(nameof(MealScores), false)]
    public class MealScores : SheetRow
    {
        public string Eaten { get; private set; }
        public string Score { get; private set; }
        [Column(Name = "Value for Money")]
        public string ValueForMoney { get; private set; }
    }
}