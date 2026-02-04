using System.Globalization;
using BrandVue.SourceData.Import;

namespace BrandVue.SourceData.Settings
{
    public class InstanceSettings : IInstanceSettings
    {
        public InstanceSettings(DateTimeOffset ? lastSignOffDate, bool generateFromAnswersTable)
        {
            LastSignOffDate = lastSignOffDate;
            GenerateFromAnswersTable = generateFromAnswersTable;
        }

        public InstanceSettings(IProductContext productContext, DateTimeOffset? overrideLastDate = null):
            this(overrideLastDate, productContext.IsAllVue || productContext.GenerateFromAnswersTable)
        {
            if (productContext.IsAllVue)
            {
                ForceBrandTypeAsDefault = false;
            }
        }

        public DateTimeOffset? LastSignOffDate { get; }
        public bool GenerateFromAnswersTable { get; }
        /// <summary>
        /// In production, we want to stop forcing the brand type as default, but until we manage that, the tests set this so that entities get predictable names rather than one suddenly being "brand" for no reason
        /// </summary>
        public bool ForceBrandTypeAsDefault { get; init; } = true;
    }
}