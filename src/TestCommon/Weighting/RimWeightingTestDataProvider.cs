using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.Weightings.Rim;
using NUnit.Framework;

namespace TestCommon.Weighting
{
    public static class RimWeightingTestDataProvider
    {
        public static IEnumerable<TestCaseData> GetTestCaseData()
        {
            yield return new TestCaseData(SimpleHappyPath.CreateTestData()).SetName(nameof(SimpleHappyPath));
            yield return new TestCaseData(AnswerWithZeroSampleSize.CreateTestData()).SetName(nameof(AnswerWithZeroSampleSize));
            yield return new TestCaseData(SlowManyDimensionalPath.CreateTestData()).SetName(nameof(SlowManyDimensionalPath));
        }

        public static IEnumerable<RimTestData> GetRawTestData()
        {
            yield return SimpleHappyPath.CreateTestData();
            yield return AnswerWithZeroSampleSize.CreateTestData();
            yield return SlowManyDimensionalPath.CreateTestData();
        }

        public static IEqualityComparer<RimWeightingCalculationResult> ResultComparer => new RimWeightingCalculationResultEqualityComparer();

        internal class RimWeightingCalculationResultEqualityComparer : IEqualityComparer<RimWeightingCalculationResult>
        {
            public bool Equals(RimWeightingCalculationResult x, RimWeightingCalculationResult y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;

                if (Math.Abs(x.MinWeight - y.MinWeight) > RimWeightingCalculator.PointTolerance) return false;
                if (Math.Abs(x.MaxWeight - y.MaxWeight) > RimWeightingCalculator.PointTolerance) return false;
                if (Math.Abs(x.EfficiencyScore - y.EfficiencyScore) > RimWeightingCalculator.PointTolerance) return false;
                if (x.Converged != y.Converged) return false;
                if (x.IterationsRequired != y.IterationsRequired) return false;

                foreach (var xQuotaDetail in x.QuotaDetails)
                {
                    var yQuotaDetail =
                        y.QuotaDetails.SingleOrDefault(qd => qd.QuotaCellKey == xQuotaDetail.QuotaCellKey);
                    if (yQuotaDetail is null) return false;
                    if (Math.Abs(xQuotaDetail.SampleSize - yQuotaDetail.SampleSize) > RimWeightingCalculator.PointTolerance) return false;
                    if (Math.Abs(xQuotaDetail.ScaleFactor - yQuotaDetail.ScaleFactor) > RimWeightingCalculator.PointTolerance) return false;
                    if (Math.Abs(xQuotaDetail.Target - yQuotaDetail.Target) > RimWeightingCalculator.PointTolerance) return false;
                }

                return true;
            }

            public int GetHashCode(RimWeightingCalculationResult obj) => 0; //We don't care for this in the tests
        }
    }
}
