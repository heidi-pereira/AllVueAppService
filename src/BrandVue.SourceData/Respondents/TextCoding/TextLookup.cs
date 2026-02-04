namespace BrandVue.SourceData.Respondents.TextCoding
{
    public class TextLookup
    {
        private readonly TextLookupType _lookupType;
        public string Name { get; }
        public IReadOnlyCollection<TextLookupData> Data { get; }

        public TextLookup(string name, IReadOnlyCollection<TextLookupData> data, TextLookupType lookupType)
        {
            _lookupType = lookupType;
            Name = name;
            Data = data;
        }

        public string BuildSqlJoinCondition(string responseText, string lookupText) =>
            _lookupType switch
            {
                TextLookupType.Equals => $"TRIM({responseText}) = {lookupText}",
                TextLookupType.StartsWith => $"TRIM({responseText}) LIKE ({lookupText} + '%')",
                TextLookupType.EndsWith => $"TRIM({responseText}) LIKE ('%' + {lookupText})",
                TextLookupType.Contains => $"TRIM({responseText}) LIKE ('%' + {lookupText} + '%')",
                _ => throw new ArgumentOutOfRangeException(nameof(_lookupType), _lookupType, null)
            };
    }
}