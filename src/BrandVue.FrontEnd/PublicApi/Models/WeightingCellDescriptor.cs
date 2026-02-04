namespace BrandVue.PublicApi.Models
{
    /// <summary>
    /// The lookup of identifiers to weighted cell parts
    /// </summary>
    /// <remarks>
    /// </remarks>
    public record WeightingCellDescriptor
    {

        /// <summary>
        /// The id of the weighting cell
        /// </summary>
        public int WeightingCellId { get; }

        /// <summary>
        /// The descriptions of the parts of the weighted cell
        /// </summary>
        public IReadOnlyDictionary<string, string> CellPartDescriptions { get; }

        /// <summary>
        /// Indicates if the cell is weighted
        /// </summary>
        public bool IsWeighted { get; }

        public WeightingCellDescriptor(int weightingCellId, IReadOnlyDictionary<string, string> cellPartDescriptions, bool isWeighted)
        {
            WeightingCellId = weightingCellId;
            CellPartDescriptions = cellPartDescriptions;
            IsWeighted = isWeighted;
        }
    }
}