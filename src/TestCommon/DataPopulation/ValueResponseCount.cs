using System;

namespace TestCommon.DataPopulation
{
    /// <summary>
    /// Numbers of responses with a given value
    /// </summary>
    public class ValueResponseCount
    {
        public int Value { get; }
        public uint Count { get; }

        public ValueResponseCount(int value, uint count)
        {
            Value = value;
            Count = count;
        }

        public static ValueResponseCount[] PercentageTrueVals(double percentageTrueVals, uint totalResponses,
            int falseVal, int trueVal)
        {
            if (percentageTrueVals > 1) throw new ArgumentOutOfRangeException(nameof(percentageTrueVals), percentageTrueVals, null);

            uint trueValsCount = (uint) (totalResponses * percentageTrueVals);
            return new[]
            {
                new ValueResponseCount(trueVal, trueValsCount),
                new ValueResponseCount(falseVal, totalResponses - trueValsCount)
            };
        }
    }
}