using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Respondents.TextCoding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Exception = System.Exception;

namespace BrandVue.SourceData.Respondents
{
    public class ResponseFieldManager : ILoadableResponseFieldManager
    {
        private readonly ILogger<ResponseFieldManager> _logger;
        private readonly IResponseEntityTypeRepository _responseEntityType;
        private readonly ConcurrentDictionary<string, ResponseFieldDescriptor> _fieldDescriptors = new(StringComparer.OrdinalIgnoreCase);
        private readonly UniqueSequentialIdProvider _uniqueSequentialIdProvider;
        private int _inMemoryIndex;

        [DataContract(Name = "EntityDefinition")]
        [DebuggerDisplay("{EntityType} {ColumnName} {EntityIdentifier}")]
        public class JsonEntityDefinition
        {
            [DataMember]
            public string EntityType { get; }
            [DataMember]
            public string ColumnName { get; }
            [DataMember]
            public string EntityIdentifier { get; }

            public JsonEntityDefinition(string entityType, string columnName, string entityIdentifier)
            {
                EntityType = entityType;
                ColumnName = columnName;
                EntityIdentifier = entityIdentifier;
            }
        }

        [DataContract(Name = "EntityDefinition")]
        [DebuggerDisplay("{ColumnName} {Value}")]
        public class JsonFilterColumn
        {
            [DataMember]
            public string ColumnName { get; }
            [DataMember]
            public int Value { get; }

            public JsonFilterColumn(string columnName, int value)
            {
                ColumnName = columnName;
                Value = value;
            }
        }
        [DataContract(Name = "FieldDefinition")]
        [DebuggerDisplay("{Name} {ColumnName} {EntityDefinitions} {FilterColumns}")]
        public class JsonFieldDefinition
        {
            [DataMember]
            public string Name { get; }
            [DataMember]
            public string ColumnName { get; }

            [DataMember(Name = "EntityDefinitions")]
            public List<JsonEntityDefinition> EntityDefinitions { get; }

            [DataMember(Name = "FilterColumns")]
            public List<JsonFilterColumn> FilterColumns { get; }
            [DataMember]
            public string TableName { get; }
            [DataMember]
            public string VarCode { get; }
            public string Categories { get; }
            public string Question { get; }
            [DataMember]
            public string ScaleFactor { get; }

            /// <summary>
            /// Must be "Round", "Ceiling" or "Floor"
            /// </summary>
            [DataMember]
            public string RoundingType { get; }
            [DataMember]
            public string DataValueColumn { get; }
            public string ValueEntityIdentifier { get; }
            public string Type { get; }
            public string LookupSheet { get; }
            public string LookupType { get; }

            public JsonFieldDefinition(string name, string tableName, string columnName, string categories,
                string question, string scaleFactor, string varCode, string dataValueColumn,
                string valueEntityIdentifier, string type, string lookupSheet, string lookupType, string roundingType)
            {
                Name = name;
                TableName = tableName;
                ColumnName = columnName;
                EntityDefinitions = new List<JsonEntityDefinition>();
                FilterColumns = new List<JsonFilterColumn>();
                Categories = categories;
                Question = question;
                ScaleFactor = scaleFactor;
                RoundingType = roundingType;
                VarCode = varCode;
                DataValueColumn = dataValueColumn;
                ValueEntityIdentifier = valueEntityIdentifier;
                Type = type;
                LookupSheet = lookupSheet;
                LookupType = lookupType;
            }
        }

        [DataContract(Name = "FieldDefinitionForSubset")]
        [DebuggerDisplay("{SubsetId} {SchemaName} {FieldDefinitions}")]
        public class JsonFieldDefinitionForSubset
        {
            [DataMember]
            public string SubsetId { get; }
            [DataMember]
            public string SchemaName { get; }
            [DataMember(Name = "FieldDefinitions")]
            public List<JsonFieldDefinition> FieldDefinitions { get; }

            public JsonFieldDefinitionForSubset(string subsetId, string schemaName = "dbo")
            {
                SubsetId = subsetId;
                SchemaName = schemaName;
                FieldDefinitions = new List<JsonFieldDefinition>();
            }
        }

        internal ResponseFieldManager(IResponseEntityTypeRepository responseEntityTypeRepository) : this(
            NullLogger<ResponseFieldManager>.Instance, responseEntityTypeRepository)
        {

        }

        public ResponseFieldManager(ILogger<ResponseFieldManager> logger,
            IResponseEntityTypeRepository responseEntityType)
        {
            _logger = logger;
            _responseEntityType = responseEntityType;
            _uniqueSequentialIdProvider = new UniqueSequentialIdProvider();
        }

        public void Load(string fullyQualifiedPathToJsonFile, string baseMetaPath)
        {
            var models = GetModels(fullyQualifiedPathToJsonFile, baseMetaPath).ToArray();
            Load(models);
        }

        public void Load(params (string SubsetId, FieldDefinitionModel Model)[] models)
        {
            var _ = LazyLoad(models).ToArray();
        }

        public IEnumerable<ResponseFieldDescriptor> LazyLoad(params (string SubsetId, FieldDefinitionModel Model)[] models)
        {
            foreach (var m in models) GetOrCreate(m.Model);
            //Add all fields, then add definitions, so that inter-field references in base expression works
            foreach (var m in models) yield return AddFieldDefinition(m.SubsetId, m.Model);
        }

        private IEnumerable<(string SubsetId, FieldDefinitionModel Model)> GetModels(string fullyQualifiedPathToJsonFile,
            string baseMetaPath)
        {
            string json = File.ReadAllText(fullyQualifiedPathToJsonFile);
            var definitions = JsonConvert.DeserializeObject<List<JsonFieldDefinitionForSubset>>(json);
            var simpleCsvReader = new SimpleCsvReader(_logger);
            foreach (var definition in definitions)
            {
                foreach (var fieldDefinition in definition.FieldDefinitions)
                {
                    double? scaleFactor = double.TryParse(fieldDefinition.ScaleFactor, out var parsedScaleFactor) ? parsedScaleFactor : null;
                    var roundingType = Enum.TryParse(fieldDefinition.RoundingType, out SqlRoundingType parsedRoundingFactor) ? parsedRoundingFactor : SqlRoundingType.Round;
                    var dataValueColumn = Enum.TryParse<EntityInstanceColumnLocation>(fieldDefinition.DataValueColumn, true, out var result) 
                        ? result 
                        : EntityInstanceColumnLocation.Unknown;

                    var textLookup = StringComparer.OrdinalIgnoreCase.Equals(fieldDefinition.Type, "Lookup")
                        ? GetTextLookup(baseMetaPath, fieldDefinition, simpleCsvReader, definition) 
                        : null;
           
                    bool isFieldDefinitionOpenText = StringComparer.OrdinalIgnoreCase.Equals(fieldDefinition.Type, "OpenText");

                    var entityModels = new List<EntityFieldDefinitionModel>();
                    foreach (var entity in fieldDefinition.EntityDefinitions)
                    {
                        var entityType = _responseEntityType.FirstOrDefault(x =>
                            string.Equals(x.Identifier, entity.EntityType, StringComparison.InvariantCultureIgnoreCase))
                            ?? throw new InvalidDataException($"Field {fieldDefinition.Name} references undefined entity type `{entity.EntityType}`");
                        entityModels.Add(new EntityFieldDefinitionModel(entity.ColumnName, entityType,
                            entity.EntityIdentifier));
                    }
                    var fieldDefinitionModel = new FieldDefinitionModel(fieldDefinition.Name, definition.SchemaName,
                        fieldDefinition.TableName, fieldDefinition.ColumnName, fieldDefinition.Question,
                        scaleFactor, fieldDefinition.VarCode, dataValueColumn, fieldDefinition.ValueEntityIdentifier, 
                        isFieldDefinitionOpenText, textLookup, entityModels, roundingType);

                    foreach (var filterColumn in fieldDefinition.FilterColumns)
                    {
                        fieldDefinitionModel.AddFilter((new DbLocation(filterColumn.ColumnName), filterColumn.Value));
                    }

                    yield return (definition.SubsetId, Model: fieldDefinitionModel);
                }
            }
        }

        private static TextLookup GetTextLookup(string baseMetaPath, JsonFieldDefinition fieldDefinition,
            SimpleCsvReader simpleCsvReader, JsonFieldDefinitionForSubset definition)
        {
            string lookupFile = Path.Combine(baseMetaPath, $"{fieldDefinition.LookupSheet}.csv");
            if (!File.Exists(lookupFile))
                throw new ArgumentException($"{fieldDefinition.Name} for survey segment {definition.SubsetId} is missing lookup file: {lookupFile}");

            var lookupType = Enum.Parse<TextLookupType>(fieldDefinition.LookupType);
            var textLookupData = simpleCsvReader.ReadCsv<TextLookupData>(lookupFile)
                .SelectMany(d => d.LookupText.Split('|').Select(t => new TextLookupData {MapToId = d.MapToId, LookupText = t}))
                .ToArray();
            return new TextLookup(fieldDefinition.LookupSheet, textLookupData, lookupType);

        }

        private ResponseFieldDescriptor AddFieldDefinition(string subsetId, FieldDefinitionModel fieldDefinitionModel)
        {
            var field = Get(fieldDefinitionModel.Name);
            field.AddDataAccessModelForSubset(subsetId, fieldDefinitionModel);
            return field;
        }

        public List<ResponseFieldDescriptor> GetOrAddFieldsForEntityType(IEnumerable<EntityType> entityTypes, string subsetId)
        {
            return _fieldDescriptors.Where(x => x.Value.EntityCombination.IsEquivalent(entityTypes) && x.Value.IsAvailableForSubset(subsetId))
                .Select(x=>x.Value).ToList();
        }

        public ICollection<ResponseFieldDescriptor> GetAllFields() =>
            _fieldDescriptors.Values;

        public ResponseFieldDescriptor Get(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentNullException(
                    nameof(fieldName),
                    $@"Cannot create a field descriptor for null or empty {nameof(fieldName)}.");
            }
            var fieldIdentifier = NameGenerator.EnsureValidPythonIdentifier(fieldName);
            var field = _fieldDescriptors.GetOrAdd(fieldIdentifier,
                (name) => throw new Exception($"Not allowed to create {fieldIdentifier} here!"));
            return field;
        }

        public bool TryGet(string fieldName, out ResponseFieldDescriptor field)
        {
            var fieldIdentifier = NameGenerator.EnsureValidPythonIdentifier(fieldName);
            return _fieldDescriptors.TryGetValue(fieldIdentifier, out field);
        }

        private void GetOrCreate(FieldDefinitionModel model)
        {
            var fieldName = model.Name;
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentNullException(
                    nameof(fieldName),
                    $"Cannot create a field descriptor for null or empty {nameof(fieldName)}.");
            }

            var fieldIdentifier = NameGenerator.EnsureValidPythonIdentifier(fieldName);
            var _ = _fieldDescriptors.GetOrAdd(
                fieldIdentifier,
                _ => ResponseFieldDescriptor.CreateFromOrderedEntities(fieldIdentifier, model.OrderedEntityCombination,
                    ++_inMemoryIndex, model.ValueEntityIdentifier, model.QuestionModel?.ItemNumber ?? 0,
                    _uniqueSequentialIdProvider));
        }
    }
}
