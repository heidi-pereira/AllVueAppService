using System.Diagnostics;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class CrosstabResults : AbstractCommonResultsInformation
    {
        public IEnumerable<CrosstabCategory> Categories { get; set; }
        public IEnumerable<InstanceResult> InstanceResults { get; set; }
        public int HiddenColumns { get; set; }
    }

    public class CrosstabCategory
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public char SignificanceIdentifier { get; set; }
        public bool IsTotalCategory { get; set; }
        public IReadOnlyCollection<CrosstabCategory> SubCategories { get; set; } = Array.Empty<CrosstabCategory>();
    }

    public class InstanceResult
    {
        public EntityInstance EntityInstance { get; set; }
        public IReadOnlyDictionary<string, CellResult> Values { get; set; }
    }

    [DebuggerDisplay("{Result} ({SampleForCount})")]
    public class CellResult
    {
        public double Result { get; set; }
        public double? Count { get; set; }
        public double SampleForCount { get; set; }
        public SampleSizeMetadata SampleSizeMetaData { get; set; }

        public Significance? Significance { get; set; }
        public IEnumerable<char> SignificantColumns { get; set; }
        public int? IndexScore { get; set; }
        public bool HasValidResult => Result != 0 || SampleForCount > 0;
        public uint UnweightedSampleForCount { get; set; }
    }
}
