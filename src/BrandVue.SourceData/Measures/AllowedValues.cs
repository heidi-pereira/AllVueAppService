namespace BrandVue.SourceData.Measures
{
    public class AllowedValues
    {

        public int? Minimum { get; set; }

        public int? Maximum { get; set; }

        public int[] Values { get; set; }

        public bool IsList =>
            Values != null && Values.Length > 0;

        public bool IsRange =>
            Minimum.HasValue && Maximum.HasValue;

        public bool Contains(int? value)
        {
            // We used to never pay attention to ranges for base values in the typescript code due to an insufficient check on onestat.ts:47
            return value.HasValue
                   && (InList(value.Value)
                       || InRange(value.Value));
        }

        private bool InList(int value)
        {
            //  Experimented with binary search here but turns out to be slower for
            //  arrays containing only a small number of values.
            return IsList
                   && Values.Contains(value);
        }

        private bool InRange(int value)
        {
            return IsRange
                   && Minimum.Value <= value
                   && value <= Maximum.Value;
        }
    }
}