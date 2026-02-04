using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    public static class FieldsExtension
    {
        public static bool IsIncludedForSubset(this Fields row, string subsetId)
        {
            if (string.IsNullOrEmpty(row.Subset))
                return true;
            return row.Subset.Split('|').Any(x => string.Compare(x, subsetId, StringComparison.InvariantCultureIgnoreCase) == 0);
        }
    }
    [Sheet(nameof(Fields), false)]
    public class Fields : SheetRow
    {
        public string Name { get; internal set; }
        public string varCode { get; internal set; }
        public string CH1 { get; internal set; }
        public string CH2 { get; internal set; }
        public string optValue { get; internal set; }
        public string Text{ get; internal set; }
        [DefaultValue(null)] public string PreScaleLowPassFilterValue { get; internal set; }
        [DefaultValue(null)] public string ScaleFactor { get; internal set; }
        [DefaultValue(null)] public string RoundingType { get; internal set; }
        [DefaultValue(null)] public string Question { get; internal set; }
        [DefaultValue(null)] public string Subset { get; internal set; }

        public Fields() {}

        public Fields(string name, string varCode, string ch1 = "", string ch2 = "", string optValue = "", string text = "", string scaleFactor = "", string preScaleLowPassFilterValue = "", string question = "", string roundingType = "")
        {
            Name = name;
            this.varCode = varCode;
            CH1 = ch1;
            CH2 = ch2;
            this.optValue = optValue;
            Text = text;
            ScaleFactor = scaleFactor;
            PreScaleLowPassFilterValue = preScaleLowPassFilterValue;
            Question = question;
            RoundingType = roundingType;
        }

        public Fields Rename(string newName)
        {
            return new Fields(newName, varCode, CH1, CH2, optValue, Text, ScaleFactor, PreScaleLowPassFilterValue, Question, RoundingType);
        }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(varCode)}: {varCode}, {nameof(CH1)}: {CH1}, {nameof(CH2)}: {CH2}, {nameof(optValue)}: {optValue}, {nameof(Text)}: {Text}, {nameof(Question)}: {Question} , {nameof(PreScaleLowPassFilterValue)}: {PreScaleLowPassFilterValue}, {nameof(ScaleFactor)}: {ScaleFactor}, {nameof(RoundingType)}: {RoundingType}";
        }

        private sealed class NameEqualityComparer : IEqualityComparer<Fields>
        {
            public bool Equals(Fields x, Fields y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Name == y.Name;
            }

            public int GetHashCode(Fields obj)
            {
                return (obj.Name != null ? obj.Name.GetHashCode() : 0);
            }
        }

        public static IEqualityComparer<Fields> NameComparer { get; } = new NameEqualityComparer();
    }
}