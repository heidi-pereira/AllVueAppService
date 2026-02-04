using System;
using System.Text.RegularExpressions;

namespace MIG.SurveyPlatform.MapGeneration.Model
{
    internal class FieldDefinition : IFieldDefinition
    {
        public FieldContext Context { get; }

        public FieldDefinition(FieldContext context)
        {
            Context = context;
        }

        public string Field { get; set; }
        public string Type { get; set; }

        public string Name => FilenameFriendly(Context.GetHumanName("_"));

        public bool HasSubsetNumericSuffix { get; set; }

        public int? UsageId { get; set; }
        public string Categories { get; set; }
        public string Question { get; set; }
        public string ParentChoiceSet { get; set; }

        public string ProfileField { get; set; } = "";
        public string ProfileValues { get; set; } = "";
        public string BrandIdTag { get; set; } = "";
        public bool HasBrandSuffix { get; set; }
        public string FieldName { get; set; }
        public bool IsBrandField { get; set; }

        private static string FilenameFriendly(string str)
        {
            var withReplacements = String.Join("_", str.Split(new []{' ', ':','-'}, StringSplitOptions.RemoveEmptyEntries))
                .Replace("/", "or").Replace("&", "and");
            return Regex.Replace(withReplacements, "[^a-zA-Z0-9_]+", "", RegexOptions.Compiled);
        }
    }
}