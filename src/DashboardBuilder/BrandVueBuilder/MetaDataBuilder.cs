using System;
using System.Collections.Generic;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;
using DashboardMetadataBuilder.MapProcessing.SupportFiles;
using Microsoft.Extensions.Logging;

namespace BrandVueBuilder
{
    internal class MetaDataBuilder
    {
        private readonly IBrandVueProductSettings _brandVueProductSettings;
        private readonly ILogger _logger;
        private readonly TableDefinitionFactory _tableDefinitionFactory;
        private readonly FieldCategories[] _fieldCategories;

        public MetaDataBuilder(BrandViewMetaBuilderAppSettings metaSettings,
            IBrandVueProductSettings brandVueProductSettings, ILoggerFactory loggerFactory,
            TableDefinitionFactory tableDefinitionFactory, FieldCategories[] fieldCategories)
        {
            MetaSettings = metaSettings;
            _brandVueProductSettings = brandVueProductSettings;
            _logger = loggerFactory.CreateLogger(nameof(MetaDataBuilder));
            _tableDefinitionFactory = tableDefinitionFactory;
            _fieldCategories = fieldCategories;
        }

        private BrandViewMetaBuilderAppSettings MetaSettings { get; }

        public void Build()
        {
            _logger.LogInformation("{Product}: Start Metadata build", _brandVueProductSettings.ShortCode);
            MetadataExtractor metadataExtractor = new MetadataExtractor();
            MetadataFileProcessor.ProcessAssets(MetaSettings.SourceFolder,MetaSettings.BaseFolder, MetaSettings.OutputPathMetadata);

            AppendLegacyMetaDataToMapFile();

            metadataExtractor.SaveAllCsvsWithMostlyLowercaseColumnNames(_brandVueProductSettings.Map, MetaSettings.OutputPathConfig);

            OutputFieldsDefinition();
            _logger.LogInformation("{Product}: Completed Metadata build", _brandVueProductSettings.ShortCode);
        }

        private FieldCategories GetFieldCategoryOrNull(string fieldName, string subset) => 
            _fieldCategories.FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.InvariantCultureIgnoreCase) && x.EnabledForSubset(subset));

        private void OutputFieldsDefinition()
        {
            List<JsonSubsetFieldDefinitions> entityDescriptions = new List<JsonSubsetFieldDefinitions>();
            foreach (var subsetId in _brandVueProductSettings.SubsetIds)
            {
                var fieldDefinitionForSubset = new JsonSubsetFieldDefinitions(subsetId, subsetId);
                entityDescriptions.Add(fieldDefinitionForSubset);

                foreach (var tableDefinition in _tableDefinitionFactory.CreateTableDefinitions(subsetId))
                {
                    var entities = new List<EntityDefinition>(
                        tableDefinition.EntityCombination.Select(x =>
                            new EntityDefinition(x.Type, x.IdColumn, x.Identifier)));
                    foreach (var field in tableDefinition.FieldMetadata)
                    {
                        var fieldCategory = GetFieldCategoryOrNull(field.Name, subsetId);
                        fieldDefinitionForSubset.FieldDefinitions.Add(
                            new JsonFieldDefinition(field.Name, field.Name,
                                tableDefinition.TableName, entities.ToArray(), fieldCategory?.Categories, field.VarCode,
                                field.GetFilterColumns(),
                                field.Question ?? fieldCategory?.Question,
                                field.ScaleFactor, field.PreScaleLowPassFilterValue,
                                valueEntityIdentifier:field.ValueEntityIdentifier, field.DataValueColumn,
                                fieldCategory?.Type, fieldCategory?.LookupSheet, fieldCategory?.LookupType, field.RoundingType));
                    }

                    foreach (var field in tableDefinition.DirectFields)
                    {
                        var fieldCategory = GetFieldCategoryOrNull(field.Name, subsetId);
                        fieldDefinitionForSubset.FieldDefinitions.Add(
                            new JsonFieldDefinition(field.Name, field.Name,
                                tableDefinition.TableName, 
                                EntityDefinitionsForDirectFields(entities, field), 
                                fieldCategory?.Categories, field.Field.varCode,
                                Array.Empty<JsonFilterColumn>(),
                                field.Field.Question ?? fieldCategory?.Question, dataValueColumn: "text",
                                type: fieldCategory?.Type, lookupSheet: fieldCategory?.LookupSheet, 
                                lookupType: fieldCategory?.LookupType
                            ));
                    }
                }
            }
            new FieldDefinitionSerializer(entityDescriptions).SerializeToDisk(MetaSettings.OutputPathConfig);
        }

        private static EntityDefinition[] EntityDefinitionsForDirectFields(List<EntityDefinition> entities, DirectFieldDefinition field)
        {
            var entitiesArray = new List<EntityDefinition>();
            foreach (var entity in entities)
            {
                if (String.Equals(entity.EntityType, field.OptionalEntityType,StringComparison.CurrentCultureIgnoreCase))
                {
                    entitiesArray.Add(new EntityDefinition(entity.EntityType, field.OptionalColumnNameOfEntity,
                        entity.EntityIdentifier));
                }
                else
                {
                    entitiesArray.Add(entity);
                }
            }
            return entitiesArray.ToArray();
        }

        private void AppendLegacyMetaDataToMapFile()
        {
            var fieldConverter = new LegacyFieldConverter(_brandVueProductSettings.Map);
            fieldConverter.ConvertFieldsToBrandAndProfileFields();
        }
    }
}