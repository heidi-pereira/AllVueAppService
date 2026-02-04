using BrandVue.SourceData.Measures;

namespace BrandVue.Models
{
    public record CompetitionBreak
    {
        public CompositeFilterModel FilterModel { get; set; }
        public BreakResults BreakResults { get; set; }
        public Measure PrimaryMeasure { get; set; }
        public string SignificanceComparand { get; set; }
    }
}
