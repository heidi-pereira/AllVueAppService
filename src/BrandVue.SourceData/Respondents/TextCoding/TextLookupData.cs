using CsvHelper.Configuration.Attributes;

namespace BrandVue.SourceData.Respondents.TextCoding
{
    public class TextLookupData
    {
        [Name("id")]
        public int MapToId { get; set; }
        [Name("lookup")]
        public string LookupText { get; set; }
    }
}