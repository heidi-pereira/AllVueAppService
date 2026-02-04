using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.CalculationPipeline
{
    public static class SignificanceCalculator
    {
        public static double CalculateTScore(Measure measure, WeightedDailyResult currentResult, WeightedDailyResult previousResult)
        {
            double tValue;

            switch (measure.CalculationType)
            {
                case CalculationType.YesNo:
                    if (currentResult.WeightedResult * currentResult.UnweightedSampleSize == 0 ||
                        previousResult.WeightedResult * previousResult.UnweightedSampleSize == 0)
                    {
                        return 0;
                    }
                    var populationMean =
                        ((currentResult.WeightedResult * currentResult.UnweightedSampleSize) +
                         (previousResult.WeightedResult * previousResult.UnweightedSampleSize)) /
                        (currentResult.UnweightedSampleSize + previousResult.UnweightedSampleSize);

                    var standardError = Math.Sqrt(
                        (populationMean * (1 - populationMean)) *
                        (1d / currentResult.UnweightedSampleSize + 1d / previousResult.UnweightedSampleSize)
                    );
                    tValue = (currentResult.WeightedResult - previousResult.WeightedResult) / standardError;
                    break;

                case CalculationType.Average:
                    if (currentResult.UnweightedSampleSize == 0 || previousResult.UnweightedSampleSize == 0)
                    {
                        return 0;
                    }
                    if (currentResult.StandardDeviation == null || previousResult.StandardDeviation == null)
                    {
                        return 0;
                    }
                    var currentVariance = Math.Pow(currentResult.StandardDeviation.Value, 2);
                    var oldVariance = Math.Pow(previousResult.StandardDeviation.Value, 2);

                    tValue = (currentResult.WeightedResult - previousResult.WeightedResult) /
                             Math.Sqrt(currentVariance / currentResult.UnweightedSampleSize +
                                       oldVariance / previousResult.UnweightedSampleSize);
                    break;

                case CalculationType.NetPromoterScore:
                    if (currentResult.UnweightedSampleSize == 0 || previousResult.UnweightedSampleSize == 0)
                    {
                        return 0;
                    }
                    tValue = (currentResult.WeightedResult - previousResult.WeightedResult);
                    break;

                default:
                    throw new NotSupportedException(
                        "Invalid calculation type: " +
                        Enum.GetName(typeof(CalculationType), measure.CalculationType)
                    );
            }

            return tValue;
        }

        public static Significance CalculateSignificance(double tScore, SigConfidenceLevel confidenceLevel)
        {
            double zScore = confidenceLevel switch
            {
                SigConfidenceLevel.NinetyNine => 2.576,
                SigConfidenceLevel.NinetyEight => 2.326,
                SigConfidenceLevel.NinetyFive => 1.96,
                SigConfidenceLevel.Ninety => 1.645,
                _ => 1.96
            };

            if (tScore > zScore)
                return Significance.Up;
            if (tScore < -zScore)
                return Significance.Down;
            return Significance.None;
        }
    }
}
