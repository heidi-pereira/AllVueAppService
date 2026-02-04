namespace BrandVue.SourceData.Measures
{
    public static class FieldOperationParser
    {
        private const string Plus = "plus";
        private const string Minus = "minus";
        private const string Or = "or";
        private const string Filter = "filter";

        public static FieldOperation Parse(object source)
        {
            return Parse(source?.ToString().Trim());
        }

        public static FieldOperation Parse(string source)
        {
            switch (source)
            {
                case null:
                case "":
                    return FieldOperation.None;

                case Filter:
                    return FieldOperation.Filter;

                case Plus:
                    return FieldOperation.Plus;

                case Minus:
                    return FieldOperation.Minus;

                case Or:
                    return FieldOperation.Or;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(source),
                        source,
                        $"Unsupported field operation {source}.");
            }    
        }
    }
}
