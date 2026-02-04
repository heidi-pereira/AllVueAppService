using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using Microsoft.EntityFrameworkCore;
namespace BrandVue.SourceData.Dashboard
{
    /// <remarks>
    /// After interacting with this repository, the EntitySetConfiguration returned should have its mapping child configurations fully loaded
    /// Except: <see cref="GetWithoutMappings"/> and <seealso cref="Delete"/>
    /// </remarks>
    public class EntitySetConfigurationRepositorySql : IEntitySetConfigurationRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;

        public EntitySetConfigurationRepositorySql(IDbContextFactory<MetaDataContext> dbContextFactory, IProductContext productContext)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
        }

        public EntitySetConfiguration GetWithoutMappings(int id)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.EntitySetConfigurations
                .Where(p => p.ProductShortCode == _productContext.ShortCode &&
                            p.SubProductId == _productContext.SubProductId)
                .SingleOrDefault(p => p.Id == id);
        }

        public EntitySetConfiguration Get(string entitySetName, string entityTypeIdentifier, string subsetId, string organisation)
        {
            return GetEntitySetConfigurations()
                .FirstOrDefault(esc => esc.Name == entitySetName
                                       && esc.TypeSubsetAndOrganisationEquals( entityTypeIdentifier, subsetId, organisation));
        }

        public EntitySetConfiguration Get(int id)
        {
            return GetEntitySetConfigurations()
                .FirstOrDefault(esc => esc.Id == id);
        }
        
        public IReadOnlyCollection<EntitySetConfiguration> GetEntitySetConfigurations()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return GetEntitySetConfigurations(dbContext).ToList();
        }

        private IEnumerable<EntitySetConfiguration> GetEntitySetConfigurations(MetaDataContext dbContext)
        {
            return dbContext.EntitySetConfigurations
                .Include(es => es.ChildAverageMappings)
                .ThenInclude(cam => cam.ChildEntitySetConfiguration)
                .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId);
        }
        

        public EntitySetConfiguration Create(EntitySetConfiguration entitySetConfiguration)
        {
            ValidateEntitySetConfiguration(entitySetConfiguration, true);

            using var dbContext = _dbContextFactory.CreateDbContext();

            dbContext.EntitySetConfigurations.Add(entitySetConfiguration);
            LoadAverageMappingProperties(entitySetConfiguration, dbContext);
            dbContext.SaveChanges();

            return entitySetConfiguration;
        }

        public EntitySetConfiguration Update(EntitySetConfiguration entitySetConfiguration)
        {
            ValidateEntitySetConfiguration(entitySetConfiguration, false);

            using var dbContext = _dbContextFactory.CreateDbContext();

            // First, fetch the existing entity with its mappings from the database
            var existingEntitySetConfig = GetEntitySetConfigurations(dbContext).FirstOrDefault(x => x.Id == entitySetConfiguration.Id);

            if (existingEntitySetConfig == null)
                throw new ArgumentException($"EntitySetConfiguration with ID {entitySetConfiguration.Id} not found.");

            // Update the main properties
            dbContext.Entry(existingEntitySetConfig).CurrentValues.SetValues(entitySetConfiguration);

            UpdateAverageMappings(entitySetConfiguration, existingEntitySetConfig, dbContext);

            dbContext.SaveChanges();

            return existingEntitySetConfig;
        }

        private static void UpdateAverageMappings(EntitySetConfiguration entitySetConfiguration,
            EntitySetConfiguration existingEntitySetConfig, MetaDataContext dbContext)
        {
            // Remove all existing mappings
            foreach (var mapping in existingEntitySetConfig.ChildAverageMappings.ToList())
            {
                dbContext.EntitySetAverageMappingConfigurations.Remove(mapping);
            }

            // Clear the collection to prevent tracking issues
            existingEntitySetConfig.ChildAverageMappings.Clear();

            // Add the new mappings
            foreach (var newMapping in entitySetConfiguration.ChildAverageMappings)
            {
                // Get the child entity from the context if it exists, otherwise attach it
                var childEntitySetConfig = dbContext.EntitySetConfigurations
                                      .Local
                                      .FirstOrDefault(e => e.Id == newMapping.ChildEntitySetId)
                                  ?? dbContext.EntitySetConfigurations.Find(newMapping.ChildEntitySetId);

                var mappingToAdd = new EntitySetAverageMappingConfiguration
                {
                    ParentEntitySetId = existingEntitySetConfig.Id,
                    ChildEntitySetId = newMapping.ChildEntitySetId,
                    ParentEntitySetConfiguration = existingEntitySetConfig,
                    ChildEntitySetConfiguration = childEntitySetConfig
                };

                existingEntitySetConfig.ChildAverageMappings.Add(mappingToAdd);
            }
        }

        public void Delete(EntitySetConfiguration entitySetConfiguration)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var attachedEntitySet = dbContext.EntitySetConfigurations.FirstOrDefault(x => x.Id == entitySetConfiguration.Id);
            if (attachedEntitySet == null)
                throw new ArgumentException("Can't find the entity set to delete.");

            // Because the foreign key can't have cascade delete (due to the ChildEntitySetId having cascade delete)
            // we need to remove the child average mappings first.
            var entitySetAverageMappingConfigurations = dbContext.EntitySetAverageMappingConfigurations.Where(x => x.ParentEntitySetId == attachedEntitySet.Id).ToArray();
            dbContext.EntitySetAverageMappingConfigurations.RemoveRange(entitySetAverageMappingConfigurations);

            dbContext.EntitySetConfigurations.Remove(attachedEntitySet);

            dbContext.SaveChanges();
        }

        private static void LoadAverageMappingProperties(EntitySetConfiguration entitySetConfiguration,
            MetaDataContext dbContext)
        {
            foreach (var mapping in entitySetConfiguration.ChildAverageMappings)
            {
                dbContext.Entry(mapping).Reference(m => m.ChildEntitySetConfiguration).Load();
            }
        }

        private void ValidateEntitySetConfiguration(EntitySetConfiguration entitySetConfiguration, bool forInsert)
        {
            var validationError = GetValidationErrorOrNull(entitySetConfiguration, forInsert);
            if (validationError != null) ThrowValidationException(validationError);
        }

        private string GetValidationErrorOrNull(EntitySetConfiguration entitySetConfiguration, bool forInsert)
        {
            if (forInsert ^ entitySetConfiguration.Id == 0)
            {
                return $"Id field {(forInsert ? "must not" : "must")} have value";
            }

            if (forInsert && AlreadyExists(entitySetConfiguration))
            {
                return $"Entity Set already exists with name: {entitySetConfiguration.Name}";
            }

            return null;
        }

        private bool AlreadyExists(EntitySetConfiguration entitySetConfiguration)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            
            return dbContext.EntitySetConfigurations.Any(es =>
                es.Name == entitySetConfiguration.Name &&
                es.Subset == entitySetConfiguration.Subset &&
                es.Organisation == entitySetConfiguration.Organisation &&
                es.EntityType == entitySetConfiguration.EntityType &&
                es.ProductShortCode == _productContext.ShortCode && 
                es.SubProductId == _productContext.SubProductId
            );
        }
        
        private static void ThrowValidationException(string message) => throw new BadRequestException(message);
    }
}
