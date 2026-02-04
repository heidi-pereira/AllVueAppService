using System.Collections.Generic;

namespace DashboardMetadataBuilder.MapProcessing.SupportFiles
{
    public class JsonSubsetFieldDefinitions
    {
        public string SubsetId { get; }
        public string SchemaName { get; }
        public List<JsonFieldDefinition> FieldDefinitions { get; }

        public JsonSubsetFieldDefinitions(string subsetId, string schemaName = "dbo")
        {
            SubsetId = subsetId;
            SchemaName = schemaName;
            FieldDefinitions = new List<JsonFieldDefinition>();
        }
    }
}