using System.Collections.Immutable;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.Respondents
{
    internal static class WeightingModelsExtensions
    {
        internal static IEnumerable<IEnumerable<int>> CategoriesForDimension(Dimension t)
        {
            return t.CellKeyToTarget.Select(rc =>
            {
                var entityIds = rc.Key.Split(QuotaCell.PartSeparator).Select(int.Parse);
                return entityIds;
            });
        }
    }
}
