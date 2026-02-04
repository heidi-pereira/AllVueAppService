namespace BrandVue.SourceData.Measures
{
    public static class CalculationTypeParser
    {
        private const string YesNo = "yn";
        private const string Average = "avg";
        private const string NetPromoterScore = "nps";
        private const string Text = "text";
        private const string EoTotalSpendPerTimeOfDay = "EoTotalSpendPerTimeOfDay";
        private const string EoTotalSpendPerLocation = "EoTotalSpendPerLocation";

        public static CalculationType Parse(object source)
        {
            return Parse(source?.ToString().Trim());
        }

        public static CalculationType Parse(string source)
        {
            switch (source)
            {
                case Average:
                    return CalculationType.Average;

                case NetPromoterScore:
                    return CalculationType.NetPromoterScore;

                case YesNo:
                    return CalculationType.YesNo;

                case Text:
                    return CalculationType.Text;
                
                case EoTotalSpendPerTimeOfDay:
                    return CalculationType.EoTotalSpendPerTimeOfDay;
                
                case EoTotalSpendPerLocation:
                    return CalculationType.EoTotalSpendPerLocation;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(source),
                        source,
                        $"Unsupported calculation type {source}.");
            }
        }

        public static string AsString(CalculationType calculationType)
        {
            return calculationType switch
            {
                CalculationType.YesNo => YesNo,
                CalculationType.Average => Average,
                CalculationType.NetPromoterScore => NetPromoterScore,
                CalculationType.Text => Text,
                _ => throw new ArgumentOutOfRangeException(nameof(calculationType), calculationType, $"Unsupported calculation type {calculationType}.")
            };
        }
    }
}
