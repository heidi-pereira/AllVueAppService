namespace BrandVue.SourceData.Weightings
{
    /// <summary>
    /// The lookup of identifiers to weightings
    /// </summary>
    /// <remarks>
    /// **This is now deprecated. Use [weighting cell weight](#tocsweightingcellweight) instead.**
    /// </remarks>
    [Obsolete("Please use the weights endpoint")]
    public class DemographicCellWeighting
    {
        public DemographicCellWeighting(int demographicCellId, double multiplier)
        {
            DemographicCellId = demographicCellId;
            Multiplier = multiplier;
        }

        /// <summary>
        /// The demographic cell id of the weighting value
        /// </summary>
        public int DemographicCellId { get; }
        /// <summary>
        /// The multiplier to apply to responses to get the correct weighted averages
        /// </summary>
        public double Multiplier { get; }

        protected bool Equals(DemographicCellWeighting other)
        {
            return DemographicCellId == other.DemographicCellId && Multiplier.Equals(other.Multiplier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DemographicCellWeighting) obj);
        }

        public override int GetHashCode()
        {
            return DemographicCellId;
        }

        public override string ToString()
        {
            return $"{nameof(DemographicCellId)}: {DemographicCellId}, {nameof(Multiplier)}: {Multiplier}";
        }
    }
}