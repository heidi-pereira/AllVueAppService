using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;

namespace BrandVueBuilder
{
    /// <summary>
    /// Text fields that BrandVue reads direct from SurveyPortalMorarTemp
    /// </summary>
    internal class DirectFieldDefinition
    {
        public string Name { get; }
        public string OptionalEntityType { get; }
        public string OptionalColumnNameOfEntity { get; }
        public Fields Field { get; }

        public DirectFieldDefinition(string name, string optionalEntityType, string optionalColumnNameOfEntity, Fields field)
        {
            Name = name;
            OptionalEntityType = optionalEntityType;
            OptionalColumnNameOfEntity = optionalColumnNameOfEntity;
            Field = field;
        }

    }
}