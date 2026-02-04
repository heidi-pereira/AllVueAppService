namespace BrandVue.EntityFramework.MetaData.Averages
{
    public enum TotalisationPeriodUnit : byte
    {
        /// <summary>
        /// Rolling average based on a sliding window
        /// </summary>
        Day,
        /// <summary>
        /// Fixed average aligned with calendar year at totalisation period intervals
        /// </summary>
        Month,
        /// <summary>
        /// Fixed average capturing all data in a given subset
        /// </summary>
        All
    }
}
