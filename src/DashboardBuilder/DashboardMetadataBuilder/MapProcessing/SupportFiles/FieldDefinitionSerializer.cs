using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DashboardMetadataBuilder.MapProcessing.SupportFiles
{
    public class FieldDefinitionSerializer
    {
        private readonly List<JsonSubsetFieldDefinitions> _fieldDefinitions;

        public FieldDefinitionSerializer(List<JsonSubsetFieldDefinitions> fieldDefinitions)
        {
            _fieldDefinitions = fieldDefinitions;
        }

        public void SerializeToDisk(string path)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new JavaScriptDateTimeConverter());
            serializer.NullValueHandling = NullValueHandling.Ignore;
            using (StreamWriter sw = new StreamWriter(Path.Combine(path, "Fields.json")))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, _fieldDefinitions);
                }
            }
        }
    }
}