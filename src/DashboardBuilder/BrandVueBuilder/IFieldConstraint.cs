using DashboardMetadataBuilder.MapProcessing.SupportFiles;

namespace BrandVueBuilder
{
    internal interface IFieldConstraint
    {
        JsonFilterColumn GenerateFilterColumnOrNull();
    }
}