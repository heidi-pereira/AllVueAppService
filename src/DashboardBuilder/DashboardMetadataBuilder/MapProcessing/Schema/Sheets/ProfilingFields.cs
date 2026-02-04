using System;
using System.ComponentModel;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing.Definitions;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet(nameof(ProfilingFields), false)]
    public class ProfilingFields : SheetRow
    {
        public string Field { get; set; }
        public string Type { get; internal set; }
        public string Name { get; internal set; }
        [DefaultValue("n")] public string HasSubsetNumericSuffix { get; private set; }
        [DefaultValue(null)] private string UsageId { get; set; }
        [DefaultValue(null)] public string Categories { get; internal set; }
        [DefaultValue(null)] public string Subset { get; internal set; }
        [DefaultValue(null)] public string ScaleFactor { get; internal set; }
        [DefaultValue(null)] public string PreScaleLowPassFilterValue { get; private set; }
        [DefaultValue(null)] public string Question { get; private set; }
        public ProfilingFields() {}

        public ProfilingFields(string field, string type, string name, string usageId = "", string categories = "", string subset = "", string scaleFactor = "", string lookupSheet = "", string question="")
        {
            Field = field;
            Type = type;
            Name = name;
            UsageId = usageId;
            Categories = categories;
            Subset = subset;
            ScaleFactor = scaleFactor;
            LookupSheet = lookupSheet;
            Question = question;
        }

        public static ProfilingFields LegacyConstuctor(string field, string categories, string subset, string question, string scaleFactor, string preScaleLowPassFilterValue)
        {
            return new ProfilingFields {Field = field, Name = field, Categories = categories, Subset = subset, ScaleFactor = scaleFactor, PreScaleLowPassFilterValue = preScaleLowPassFilterValue, Question = question};
        }
        public string GetFieldVarcodeWithSubsetSuffix(string subsetNumericSuffix) =>
            Field + GetSubsetSuffix(subsetNumericSuffix);

        public string GetSubsetSuffix(string subsetNumericSuffix)
        {
            return (HasSubsetNumericSuffix == "y" ? subsetNumericSuffix : "");
        }

        private string [] AssociatedSubsets => string.IsNullOrEmpty(Subset) ? new string[] { }: Subset.Split(new[] {"|"}, StringSplitOptions.RemoveEmptyEntries);

        public bool IsFieldAvailable(MapSubset subset)
        {
            var availSubsets = AssociatedSubsets;
            return availSubsets.Length == 0 || availSubsets.Any(x=> string.Equals(x, subset.SubsetId, StringComparison.CurrentCultureIgnoreCase));
        }
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

        [DefaultValue(null)]
        public string LookupSheet { get; internal set; }

        [DefaultValue(TextLookupType.StartsWith)]
        public TextLookupType LookupType { get; set; }
    }
}