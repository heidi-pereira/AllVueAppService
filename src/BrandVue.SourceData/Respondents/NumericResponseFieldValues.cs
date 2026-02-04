namespace BrandVue.SourceData.Respondents
{
    /// <summary>
    /// This exists because the input data is massively redundant.
    /// Many brand responses contain *exactly* the same field values
    /// and thus we'd like to store these only once, a bit like
    /// interning strings.
    /// 
    /// This type is really just a specialised dictionary that
    /// implements an equality commparison that allows us to do
    /// the equivalent of the string interning process and store
    /// only one copy of the same data.
    /// </summary>
    public class NumericResponseFieldValues : Dictionary<string, int>
    {
        private int? _hashCode;

        public NumericResponseFieldValues() : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public override bool Equals(object obj)
        {
            var other = obj as NumericResponseFieldValues;
            if (other == null || other.Count != Count)
            {
                return false;
            }

            foreach (var kvp in this)
            {
                if (!other.TryGetValue(kvp.Key, out int otherValue))
                {
                    return false;
                }

                if (kvp.Value != otherValue)
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            if (_hashCode == null)
            {
                var temp = 0;
                foreach (var kvp in this)
                {
                    temp ^= kvp.Key.GetHashCode();
                    temp ^= kvp.Value;
                }

                _hashCode = temp;
            }
            return _hashCode.Value;
        }
    }
}
