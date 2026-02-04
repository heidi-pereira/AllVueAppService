
using CsvHelper.Configuration.Attributes;

namespace BrandVue.SourceData.Subsets
{
    public class SurveyRecord
    {
        [Name("survey id")]
        public int SurveyId { get; set; }

        [Name("name")]
        public string Name { get; set; }

        [Name("subset")]
        public string SubsetId { get; set; }

        [Name("includedsegments")]
        public string IncludedSegments { get; set; }
    }
}