using System.Numerics;
using IronPython.Runtime.Exceptions;

namespace BrandVue.SourceData.Calculation.Expressions;

internal static class MemoryNumericExtensions
{
    public static TNum Min<TNum>(this Memory<TNum> nums, TNum? defaultValueOrNullForThrow = null) where TNum : struct, IComparisonOperators<TNum, TNum, bool>, IMinMaxValue<TNum>
    {
        if (nums.Length == 0) return defaultValueOrNullForThrow ?? throw new SyntaxWarningException("Cannot get min of zero numbers. Please specify a default for this case e.g. min(something) becomes min(something, default=None)");
        var min = TNum.MaxValue;
        for (int i = 0; i < nums.Length; i++)
        {
            var current = nums.Span[i];
            if (current < min) min = current;
        }

        return min;
    }

    public static TNum Max<TNum>(this Memory<TNum> nums, TNum? defaultValueOrNullForThrow = null) where TNum : struct, IComparisonOperators<TNum, TNum, bool>, IMinMaxValue<TNum>
    {
        if (nums.Length == 0) return defaultValueOrNullForThrow ?? throw new SyntaxWarningException("Cannot get max of zero numbers. Please specify a default for this case e.g. max(something) becomes max(something, default=None)");
        var max = TNum.MinValue;
        for (int i = 0; i < nums.Length; i++)
        {
            var current = nums.Span[i];
            if (current > max) max = current;
        }

        return max;
    }
    public static Numeric Sum(this Memory<Numeric> nums)
    {
        Numeric sum = 0;
        for (int i = 0; i < nums.Length; i++)
        {
            sum += nums.Span[i];
        }

        return sum;
    }
    public static Numeric Any(this Memory<Numeric> nums)
    {
        for (int i = 0; i < nums.Length; i++)
        {
            var current = nums.Span[i];
            if (current.IsTruthy) return true;
        }

        return false;
    }
    public static Numeric Count(this Memory<Numeric> nums, Numeric toCount)
    {
        int count = 0;
        for (int i = 0; i < nums.Length; i++)
        {
            var current = nums.Span[i];
            if (current == toCount) count++;
        }

        return count;
    }
    public static bool Contains<T>(this Memory<T> nums, T toFind) where T : IEqualityOperators<T, T, bool>
    {
        for (int i = 0; i < nums.Length; i++)
        {
            var current = nums.Span[i];
            if (current == toFind) return true;
        }

        return false;
    }
}