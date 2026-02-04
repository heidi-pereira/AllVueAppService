using System;
using System.ComponentModel;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(nameof(BrandFields), false)]
    public class BrandFields : SheetRow
    {
        public string Field { get; set; }
        public string Type { get; internal set; }
        public string Name { get; internal set; }
        private string HasBrandSuffix { get; set; }
        private string UsageId { get; set; }
        [DefaultValue(null)] public string Categories { get; private set; }
        [DefaultValue(null)] public string FieldName { get; private set; }

        public static BrandFields LegacyConstuctor(string field, string categories)
        {
            return new BrandFields { Field = field, Name = field, Categories = categories, HasBrandSuffix = "n"};
        }

        public bool VarcodeHasBrandSuffix => HasBrandSuffix == "y";

        public int? FieldUsageIdOrNull
        {
            get
            {
                int? usageIdOrNull = null;
                if (!String.IsNullOrWhiteSpace(UsageId))
                {
                    usageIdOrNull = Convert.ToInt32(UsageId);
                    if (usageIdOrNull == -992)
                    {
                        throw new ArgumentOutOfRangeException(nameof(UsageId), UsageId,
                            $"CH1 set to -992 is meaningless, please delete the value from the cell on row with name '{Name}'");
                    }
                }

                return usageIdOrNull;
            }
        }

        public BrandFieldType BrandFieldType
        {
            get
            {
                if (Enum.TryParse(Type, true, out BrandFieldType brandFieldType)) return brandFieldType;
                throw new ArgumentOutOfRangeException(nameof(Type), Type,
                    $"Valid types for a brand field are {String.Join(",", Enum.GetNames(typeof(BrandFieldType)))}");
            }
        }

        [DefaultValue(null)]
        public string LookupSheet { get; internal set; }

        [DefaultValue(TextLookupType.StartsWith)]
        public TextLookupType LookupType { get; internal set; }
    }
}