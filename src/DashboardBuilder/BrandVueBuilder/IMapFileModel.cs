using System.Collections.Generic;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;

namespace BrandVueBuilder
{
    internal interface IMapFileModel
    {
        IReadOnlyCollection<Entity> Entities { get; }
        IReadOnlyCollection<TextLookup> Lookups { get; }
        IReadOnlyCollection<Fields> FieldsForSubset(string subsetId);
    }
}