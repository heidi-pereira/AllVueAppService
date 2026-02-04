using System.Collections.Generic;
using System.Linq;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Serialization.Model
{
    internal class SubsetSerializationInfo : ISerializationInfo<Subset>
    {
        public string SheetName { get; } = "Subsets";

        public string[] ColumnHeadings { get; } = new[]
        {
            nameof(Subset.Id), nameof(Subset.DisplayName), nameof(Subset.DisplayNameShort), nameof(Subset.Iso2LetterCountryCode), nameof(Subset.Description), nameof(Subset.Order), nameof(Subset.NumericSuffix), nameof(Subset.Disabled), nameof(Subset.Environment), nameof(Subset.ExternalUrl)
        };

        public string[] RowData(Subset s)
        {
            return new[]
            {
                s.Id, s.DisplayName, s.DisplayNameShort, s.Iso2LetterCountryCode, s.Description, s.Order.ToString(), s.NumericSuffix, s.Disabled, s.Environment, s.ExternalUrl,
            };
        }

        public IEnumerable<Subset> OrderForOutput(IEnumerable<Subset> subset)
        {
            return subset.OrderBy(s => s.Order);
        }
    }
}