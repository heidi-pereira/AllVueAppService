
namespace BrandVue.PublicApi.Models
{

    /// <summary>
    /// The lookup of identifiers to weightings
    /// </summary>
    /// <remarks>
    /// </remarks>
    public record Weight 
    {
        public Weight(int weightingCellId, double multiplier)
        {
            WeightingCellId = weightingCellId;
            Multiplier = multiplier;
        }

        /// <summary>
        /// The id of the weighting cell
        /// </summary>
        public int WeightingCellId { get; }

        /// <summary>
        /// The multiplier to apply to responses to get the correct weighted averages
        /// </summary>
        public double Multiplier { get; }

        public override string ToString()
        {
            return $"{nameof(WeightingCellId)}: {WeightingCellId}, {nameof(Multiplier)}: {Multiplier}";
        }

    }

}