using System.Globalization;
using System.IO;
using System.Text;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Dashboard;
using CsvHelper;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Entity;

public class EntitySetSqlMigrator
{
    private readonly IEntitySetConfigurationRepository _entitySetConfigurationRepository;
    private readonly IProductContext _productContext;
    private ILogger<EntitySetSqlMigrator> _logger;

    private const int BufferSize = 1024 * 1024;
    private const string NameHeader = "Name";
    private const string EntityHeader = "EntityType";
    private const string SubsetHeader = "Subset";
    private const string InstancesHeader = "Instances";
    private const string KeyInstancesHeader = "KeyInstances";
    private const int NotFoundEntityInstanceId = -1;
    private const string OrganisationHeader = "Organisation";
    private const string MainInstanceHeader = "MainInstance";
    private const string FallbackHeader = "Fallback";
    private const string SectorSetHeader = "SectorSet";
    
    private const string AutomaticMigrationUserId = "automatic-migration";

    
    public EntitySetSqlMigrator(IEntitySetConfigurationRepository entitySetConfigurationRepository, IProductContext productContext, ILoggerFactory loggerFactory)
    {
        _entitySetConfigurationRepository = entitySetConfigurationRepository;
        _productContext = productContext;
        _logger = loggerFactory.CreateLogger<EntitySetSqlMigrator>();
    }

    public bool MigrateEntitySetsToSql(string baseMetadataPath)
    {
        bool fileExists = false;
        var filepath = Path.Combine(baseMetadataPath, "EntitySets.csv");

        var existingSets = _entitySetConfigurationRepository.GetEntitySetConfigurations();
        
        if (File.Exists(filepath) && !existingSets.Any())
        {
            fileExists = true;
            ProcessFile(filepath);
        }

        return fileExists;
    }

    private void ProcessFile(string entitySetCsvFilepath)
    {
        using (var fileStream = new FileStream(
                   entitySetCsvFilepath,
                   FileMode.Open,
                   FileAccess.Read,
                   FileShare.ReadWrite))
        using (var bs = new BufferedStream(fileStream, BufferSize))
        using (var reader = new StreamReader(bs, Encoding.UTF8, true, BufferSize))
        using (var parser = new CsvParser(reader, CultureInfo.CurrentCulture))
        {
            var headers = parser.Read();
            if (headers == null)
            {
                throw new ArgumentOutOfRangeException($"End of file whilst reading header {entitySetCsvFilepath}");
            }

            while (true)
            {
                var rowValues = parser.Read();
                if (rowValues == null)
                {
                    break;
                }

                WriteRowToDatabase(headers, rowValues);
            }
        }
    }

    private void WriteRowToDatabase(string[] headers, string[] rowValues)
    {
        if (rowValues.Length == 0 || rowValues.All(string.IsNullOrWhiteSpace))
        {
            return;
        }
        
        var name = FieldExtractor.ExtractString(NameHeader, headers, rowValues);
        var entityType = FieldExtractor.ExtractString(EntityHeader, headers, rowValues);
        var subsetId = FieldExtractor.ExtractString(SubsetHeader, headers, rowValues);
        var organisation = FieldExtractor.ExtractString(OrganisationHeader, headers, rowValues, true);

        string subsetToUse = !string.IsNullOrWhiteSpace(subsetId) ? subsetId : null;
        string organisationToUse = !string.IsNullOrWhiteSpace(organisation) ? organisation : null;
        
        try
        {
            var mainInstanceId = FieldExtractor.ExtractInteger(MainInstanceHeader, headers, rowValues, defaultValue: NotFoundEntityInstanceId);
            var isFallback = FieldExtractor.ExtractBoolean(FallbackHeader, headers, rowValues);
            var isSectorSet = FieldExtractor.ExtractBoolean(SectorSetHeader, headers, rowValues);

            var keyInstancesString = FieldExtractor.ExtractString(KeyInstancesHeader, headers, rowValues);
            var keyInstancesStringArray = FieldExtractor.ExtractStringArray(KeyInstancesHeader, headers, rowValues) ?? new string[0];
            var keyInstanceIds = keyInstancesStringArray.SelectMany(GetIdsFromRange).ToList();
                
            var allInstancesString = FieldExtractor.ExtractString(InstancesHeader, headers, rowValues);
            var allInstancesStringArray = FieldExtractor.ExtractStringArray(InstancesHeader, headers, rowValues) ?? new string[0];
            var allInstanceIds = allInstancesStringArray.SelectMany(GetIdsFromRange).ToList();

            if (mainInstanceId == NotFoundEntityInstanceId)
            {
                mainInstanceId = keyInstanceIds.First();
            }

            var entitySetConfiguration = new EntitySetConfiguration
            {
                ProductShortCode = _productContext.ShortCode,
                SubProductId = null,
                Name = name,
                EntityType = entityType,
                Subset = subsetToUse,
                Instances = keyInstancesString,
                MainInstance = mainInstanceId,
                IsFallback = isFallback,
                IsSectorSet = isSectorSet,
                IsDefault = false,
                IsDisabled = false,
                Organisation = organisationToUse,
                LastUpdatedUserId = AutomaticMigrationUserId,
                ChildAverageMappings = new List<EntitySetAverageMappingConfiguration>()
            };

            var createdParentSet = _entitySetConfigurationRepository.Create(entitySetConfiguration);

            if (RequiresNewSetForAverage(keyInstanceIds, allInstanceIds))
            {
                var averageMainInstanceId = mainInstanceId;
                if (averageMainInstanceId == NotFoundEntityInstanceId)
                {
                    averageMainInstanceId = allInstanceIds.First();
                }
                
                var configurationForAverageSet = new EntitySetConfiguration
                {
                    ProductShortCode = _productContext.ShortCode,
                    SubProductId = null,
                    Name = $"{name} (average)",
                    EntityType = entityType,
                    Subset = subsetToUse,
                    Instances = allInstancesString,
                    MainInstance = averageMainInstanceId,
                    IsFallback = isFallback,
                    IsSectorSet = isSectorSet,
                    IsDefault = false,
                    IsDisabled = false,
                    Organisation = organisationToUse,
                    LastUpdatedUserId = AutomaticMigrationUserId,
                    ChildAverageMappings = new List<EntitySetAverageMappingConfiguration>(),
                };

                var createdAverageSet = _entitySetConfigurationRepository.Create(configurationForAverageSet);

                var averageMapping = new EntitySetAverageMappingConfiguration
                {
                    ParentEntitySetId = createdParentSet.Id,
                    ChildEntitySetId = createdAverageSet.Id,
                    ExcludeMainInstance = false
                };

                createdParentSet.ChildAverageMappings = new List<EntitySetAverageMappingConfiguration> {averageMapping};
            }
            else
            {
                var averageMapping = new EntitySetAverageMappingConfiguration
                {
                    ParentEntitySetId = createdParentSet.Id,
                    ChildEntitySetId = createdParentSet.Id,
                    ExcludeMainInstance = false
                };
                
                createdParentSet.ChildAverageMappings = new List<EntitySetAverageMappingConfiguration> {averageMapping};
            }

            _entitySetConfigurationRepository.Update(createdParentSet);
            
            _logger.LogInformation($"Success: Migrated entity set ${name} for type: {entityType}, product: {_productContext.ShortCode}, subset: {subsetToUse}, organisation: {organisationToUse}");
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error: Failed to migrate entity set {name} for type: {entityType}, product: {_productContext.ShortCode}, subset: {subsetToUse}, organisation: {organisationToUse}");
        }
    }
    
    private static bool RequiresNewSetForAverage(IEnumerable<int> keyInstances, IEnumerable<int> allInstances)
    {
        var keyInstanceSet = keyInstances.ToHashSet();
        var allInstancesSet = allInstances.ToHashSet();
        return !keyInstanceSet.SetEquals(allInstancesSet);
    }
    
    private IEnumerable<int> GetIdsFromRange(string idRangeString)
    {
        if (!idRangeString.Contains(":"))
        {
            return new[] {int.Parse(idRangeString)};
        }

        var idRange = idRangeString.Split(':').Select(int.Parse).ToArray();
        int min = idRange[0];
        int max = idRange[1];

        return Enumerable.Range(min, max - min + 1);
    }
}
