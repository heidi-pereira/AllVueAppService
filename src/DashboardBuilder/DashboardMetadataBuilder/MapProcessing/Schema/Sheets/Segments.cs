using System.Collections.Generic;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    /// <summary>
    /// TODO Change this to true when MakeSegmentData is called for all builds (and replace TypedWorksheet&lt;Segments&gt;.TryGet) with the constructor call
    /// </summary>
    [Sheet(nameof(Segments), false)]
    public class Segments : SheetRow
    {
        public int SegmentId { get; private set; }
        public string SourceType { get; private set; }
        public string Source { get; private set; }

        /// <summary>
        /// You should assume the column name is nameof(Dimensions.Id) if it isn't set here
        /// </summary>
        public string DimensionsColumnName { get; private set; }
        /// <summary>
        /// Only used for segments that map to a single source venue id, e.g. card based
        /// </summary>
        public string SourceVenueId { get; private set; }
        public string RespondentInfoFromQuestion { get; private set; }

        public static IEnumerable<string> GetDimensionsIdColumnNames(IReadOnlyCollection<Segments> segments)
        {
            return segments.Select(s => s.DimensionsColumnName).Concat(new[]{ nameof(Dimensions.Id) });
        }
    }
}