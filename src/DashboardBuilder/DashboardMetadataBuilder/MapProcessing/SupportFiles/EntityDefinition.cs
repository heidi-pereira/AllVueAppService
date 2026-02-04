namespace DashboardMetadataBuilder.MapProcessing.SupportFiles
{
    public class EntityDefinition
    {
        public string EntityType { get; }
        public string ColumnName { get; }
        public string EntityIdentifier { get; }

        public EntityDefinition(string entityType, string columnName, string entityIdentifier)
        {
            EntityType = entityType;
            ColumnName = columnName;
            EntityIdentifier = entityIdentifier;
        }
    }
}
