using System.Collections.Generic;
using DashboardMetadataBuilder.MapProcessing.SupportFiles;

namespace BrandVueBuilder
{
    internal interface IFieldMetadata
    {
        string Name { get; }
        string ValueEntityIdentifier { get; }

        string Question { get; }
        string ScaleFactor { get; }
        string RoundingType { get; }
        string PreScaleLowPassFilterValue { get; }
        string VarCode { get; }
        string DataValueColumn { get; }
        IReadOnlyCollection<JsonFilterColumn> GetFilterColumns();
    }
}