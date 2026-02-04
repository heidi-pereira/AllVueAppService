using BrandVue.Models;

namespace BrandVue.Services.CrosstabExporterUtilities
{
    public class CrosstabHeader
    {
        public string Id { get; }
        public string Name { get; }
        public char SignificanceIdentifier { get; }
        public CrosstabHeader[] SubHeaders { get; }
        public int Depth { get; }
        public int ColumnSpan { get; }

        private CrosstabHeader(string id, string name, char significanceIdentifier, CrosstabHeader[] subHeaders)
        {
            Id = id;
            Name = name;
            SignificanceIdentifier = significanceIdentifier;
            SubHeaders = subHeaders;
            Depth = CalculateDepth();
            ColumnSpan = CalculateColumnSpan();
        }

        public CrosstabHeader(CrosstabCategory category) : this(category.Id, category.DisplayName ?? category.Name, category.SignificanceIdentifier, category.SubCategories.Select(c => new CrosstabHeader(c)).ToArray()) { }

        public CrosstabHeader ExtendToDepth(int depth)
        {
            if (Depth == depth)
            {
                return this;
            }
            var extended = new CrosstabHeader(Id, null, default, new[] { this });
            return extended.ExtendToDepth(depth);
        }

        public CrosstabHeader[] GetColumnsAtDepth(int depth)
        {
            if (Depth == depth)
            {
                return new[] { this };
            }
            return SubHeaders.SelectMany(h => h.GetColumnsAtDepth(depth)).ToArray();
        }

        private int CalculateDepth()
        {
            if (SubHeaders.Length == 0)
            {
                return 0;
            }

            return 1 + SubHeaders.Select(s => s.Depth).Max();
        }

        private int CalculateColumnSpan()
        {
            if (SubHeaders.Length == 0)
            {
                return 1;
            }
            return SubHeaders.Select(s => s.ColumnSpan).Sum();
        }
    }
}
