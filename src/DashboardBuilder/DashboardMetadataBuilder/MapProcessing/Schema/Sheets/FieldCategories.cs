using System;
using System.ComponentModel;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing.Typed;
using JetBrains.Annotations;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    [Sheet("Categories", false)]
    public class FieldCategories : SheetRow
    {
        public string FieldName { get; internal set; }
        public string Categories { get; internal set; }
        public string Subsets { get; internal set; }
        [DefaultValue(null)]
        public string Question { get; internal set; }
        [DefaultValue(null)]
        public string Type { get; internal set; }
        [DefaultValue(null)]
        public string LookupSheet { get; internal set; }
        [DefaultValue(null)]
        public string LookupType { get; internal set; }
        [UsedImplicitly]
        public FieldCategories() {}

        public FieldCategories(string fieldName, string categories, string subsets = "", string question= "", string type = "", string lookupSheet = "", string lookupType = "")
        {
            FieldName = fieldName;
            Categories = categories;
            Subsets = subsets;
            Question = question;
            Type = type;
            LookupSheet = lookupSheet;
            LookupType = lookupType;
        }

        public FieldCategories Rename(string newName)
        {
            return new FieldCategories(newName, Categories, Subsets, Question, Type, LookupSheet, LookupType);
        }

        public override string ToString()
        {
            return $"{nameof(FieldName)}: {FieldName}, {nameof(Categories)}: {Categories}, {nameof(Subsets)}: {Subsets}, {nameof(Question)}: {Question}, {nameof(Type)}: {Type}, {nameof(LookupSheet)}: {LookupSheet}, {nameof(LookupType)}: {LookupType}";
        }
    }

    public static class FieldCategoriesExtension
    {
        public static bool EnabledForSubset(this FieldCategories field, string subset)
        {
            if (string.IsNullOrEmpty(field.Subsets))
            {
                return true;
            }
            var subsets = field.Subsets.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
            return subsets.Contains(subset);
        }

    }
}