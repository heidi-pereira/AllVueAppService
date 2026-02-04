namespace BrandVue.PublicApi.Models
{
    /// <summary>
    /// The lookup of identifiers to demographic cell parts
    /// </summary>
    /// <remarks>
    /// **This is now deprecated. Use [weighting cell descriptor](#tocsweightingcelldescriptor) instead.**
    /// </remarks>
    [Obsolete("Use" +nameof(WeightingCellDescriptor))]
    public class DemographicCellDescriptor : IEquatable<DemographicCellDescriptor>
    {
        /// <summary>
        /// The id of the demographic cell
        /// </summary>
        public int DemographicCellId { get; }

        /// <summary>
        /// The region of the descriptor
        /// </summary>
        public string Region { get; }

        /// <summary>
        /// The gender of the descriptor
        /// </summary>
        public string Gender { get; }

        /// <summary>
        /// The age group of the descriptor
        /// </summary>
        public string AgeGroup { get; }

        /// <summary>
        /// The socioeconomic group of the descriptor
        /// </summary>
        public string SocioEconomicGroupIndicator { get; }

        public DemographicCellDescriptor(int demographicCellId, string region, string gender, string ageGroup,
            string socioEconomicGroupIndicator)
        {
            DemographicCellId = demographicCellId;
            Region = region ?? throw new ArgumentNullException(nameof(region));
            Gender = gender ?? throw new ArgumentNullException(nameof(gender));
            AgeGroup = ageGroup ?? throw new ArgumentNullException(nameof(ageGroup));
            SocioEconomicGroupIndicator = socioEconomicGroupIndicator ?? throw new ArgumentNullException(nameof(socioEconomicGroupIndicator));
        }

        public bool Equals(DemographicCellDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return DemographicCellId == other.DemographicCellId && string.Equals(Region, other.Region) && string.Equals(Gender, other.Gender) && string.Equals(AgeGroup, other.AgeGroup) && string.Equals(SocioEconomicGroupIndicator, other.SocioEconomicGroupIndicator);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DemographicCellDescriptor) obj);
        }
        
        public override int GetHashCode()
        {
            return DemographicCellId;
        }

        public override string ToString()
        {
            return $"{nameof(DemographicCellId)}: {DemographicCellId}, {nameof(Region)}: {Region}, {nameof(Gender)}: {Gender}, {nameof(AgeGroup)}: {AgeGroup}, {nameof(SocioEconomicGroupIndicator)}: {SocioEconomicGroupIndicator}";
        }
    }
}