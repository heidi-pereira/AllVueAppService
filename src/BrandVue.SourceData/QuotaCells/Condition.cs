using System.Diagnostics;

namespace BrandVue.SourceData.QuotaCells
{
    [DebuggerDisplay("{_lowerBound}-{_upperBound}")]
    public class Condition
    {
        private readonly int _lowerBound;
        private readonly int _upperBound;

        private Condition(string[] split)
        {
            _lowerBound = int.Parse(split.First());
            _upperBound = int.Parse(split.Last());
        }

        /// <param name="condition">Comma separated conditions of the form "x-y" or "x" where x and y are integers
        /// e.g. "1-5,6,7-9"</param>
        public static IEnumerable<Condition> Parse(string condition)
        {
            return condition.Split(',').Select(c => new Condition(c.Split('-')));
        }

        public bool IsMatch(int val)
        {
            return val >= _lowerBound && val <= _upperBound;
        }
    }
}