using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.LazyLoading;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;
using BrandVue.EntityFramework.Exceptions;

namespace BrandVue.SourceData.Dashboard
{
    public class EntityTypeRepositorySql : IEntityTypeConfigurationRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IResponseEntityTypeRepository _responseEntityTypeRepository;
        private readonly IProductContext _productContext;

        public EntityTypeRepositorySql(IProductContext productContext, IDbContextFactory<MetaDataContext> dbContextFactory, IResponseEntityTypeRepository responseEntityTypeRepository)
        {
            _dbContextFactory = dbContextFactory;
            _responseEntityTypeRepository = responseEntityTypeRepository;
            _productContext = productContext;
        }

        public IReadOnlyCollection<EntityTypeConfiguration> GetEntityTypes()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return EntityTypeConfigurations(dbContext)
                .ToList();
        }

        private IQueryable<EntityTypeConfiguration> EntityTypeConfigurations(MetaDataContext dbContext)
        {
            return ForProductContext(dbContext.EntityTypeConfigurations);
        }

        private IQueryable<EntityTypeConfiguration> ForProductContext(IQueryable<EntityTypeConfiguration> dbContextEntityTypeConfigurations)
        {
            return dbContextEntityTypeConfigurations
                .Where(p => p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId);
        }

        public EntityTypeConfiguration Save(string entityTypeIdentifier, string displayNameSingular, string displayNamePlural, IReadOnlyCollection<string> surveyChoiceSetNames, EntityTypeCreatedFrom? createdFrom)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var toUpdateOrAdd = Save(entityTypeIdentifier, displayNameSingular, displayNamePlural, surveyChoiceSetNames, createdFrom, dbContext);
            dbContext.SaveChanges();
            return toUpdateOrAdd;
        }

        public EntityTypeConfiguration Save(string entityTypeIdentifier, string displayNameSingular,
            string displayNamePlural,
            IReadOnlyCollection<string> surveyChoiceSetNames, EntityTypeCreatedFrom? createdFrom,
            MetaDataContext dbContext, bool allowDuplicateDisplayNames)
        {
            return Save(entityTypeIdentifier, displayNameSingular, displayNamePlural, surveyChoiceSetNames, createdFrom, dbContext, allowDuplicateDisplayNames, true);
        }

        private EntityTypeConfiguration Save(string entityTypeIdentifier, string displayNameSingular, string displayNamePlural,
            IReadOnlyCollection<string> surveyChoiceSetNames, EntityTypeCreatedFrom? createdFrom, MetaDataContext dbContext, bool allowDuplicateDisplayNames = false, bool checkLocal = false)
        {
            var toUpdateOrAdd = ForProductContext(dbContext.EntityTypeConfigurations).SingleOrDefault(c => c.Identifier == entityTypeIdentifier);
            if (checkLocal && toUpdateOrAdd is null)
            {
                toUpdateOrAdd = ForProductContext(dbContext.EntityTypeConfigurations.Local.AsQueryable()).SingleOrDefault(c => c.Identifier == entityTypeIdentifier);
            }

            if (toUpdateOrAdd != null)
            {
                toUpdateOrAdd.DisplayNameSingular = displayNameSingular;
                toUpdateOrAdd.DisplayNamePlural = displayNamePlural;
                toUpdateOrAdd.SurveyChoiceSetNames = surveyChoiceSetNames;
            }
            else
            {
                toUpdateOrAdd = new EntityTypeConfiguration
                {
                    ProductShortCode = _productContext.ShortCode,
                    SubProductId = _productContext.SubProductId,
                    Identifier = entityTypeIdentifier,
                    DisplayNameSingular = displayNameSingular,
                    DisplayNamePlural = displayNamePlural,
                    SurveyChoiceSetNames = surveyChoiceSetNames,
                    CreatedFrom = createdFrom
                };
                dbContext.EntityTypeConfigurations.Add(toUpdateOrAdd);
            }

            if (!allowDuplicateDisplayNames) ValidateModel(toUpdateOrAdd);
            return toUpdateOrAdd;
        }

        private void ValidateModel(EntityTypeConfiguration entityTypeConfiguration)
        {
            var singularNameAlreadyInMemory = _responseEntityTypeRepository
                .Where(e => e.Identifier != entityTypeConfiguration.Identifier)
                .Select(e => e.DisplayNameSingular)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)
                .Contains(entityTypeConfiguration.DisplayNameSingular);

            if (singularNameAlreadyInMemory)
                throw new BadRequestException("DisplayNameSingular already in use");

            var pluralNameAlreadyInMemory = _responseEntityTypeRepository
                .Where(e => e.Identifier != entityTypeConfiguration.Identifier)
                .Select(e => e.DisplayNamePlural)
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase)
                .Contains(entityTypeConfiguration.DisplayNamePlural);

            if (pluralNameAlreadyInMemory)
                throw new BadRequestException("DisplayNamePlural already in use");
            
            using var dbContext = _dbContextFactory.CreateDbContext();
            var entityConfigurationsNotEditing = EntityTypeConfigurations(dbContext).Where(e => e.Identifier != entityTypeConfiguration.Identifier);

            AssertPropertyValueNotInDb(entityTypeConfiguration, entityConfigurationsNotEditing, p => p.DisplayNameSingular);
            AssertPropertyValueNotInDb(entityTypeConfiguration, entityConfigurationsNotEditing, p => p.DisplayNamePlural);
        }

        private void AssertPropertyValueNotInDb(EntityTypeConfiguration entityTypeConfiguration, IQueryable<EntityTypeConfiguration> existingDbEntityTypeConfigurations , Func<EntityTypeConfiguration, IComparable> getProp)
        {
            if (string.IsNullOrEmpty(getProp(entityTypeConfiguration).ToString()))
                throw new BadRequestException($"{getProp(entityTypeConfiguration)} cannot be null or empty.");

            bool existsInDb = existingDbEntityTypeConfigurations.AsEnumerable().Any(e => getProp(e).Equals(getProp(entityTypeConfiguration)));
            if (existsInDb)
                throw new BadRequestException($"{getProp(entityTypeConfiguration)} already in use.");
        }
    }
}
