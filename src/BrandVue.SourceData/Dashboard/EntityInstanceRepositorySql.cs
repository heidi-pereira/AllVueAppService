using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlTypes;
using BrandVue.EntityFramework.Exceptions;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BrandVue.SourceData.Dashboard
{
    public class EntityInstanceRepositorySql : IEntityInstanceConfigurationRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IEntityRepository _entityRepository;
        private readonly IProductContext _productContext;

        public EntityInstanceRepositorySql(IProductContext productContext, IDbContextFactory<MetaDataContext> dbContextFactory, IEntityRepository entityRepository)
        {
            _dbContextFactory = dbContextFactory;
            _entityRepository = entityRepository;
            _productContext = productContext;
        }

        public string VerifyLengthAndTruncate(string toTruncate, int maxLength)
        {
            if(toTruncate.Length < maxLength)
            {
                return toTruncate;
            }
            return toTruncate.Substring(0, maxLength);
        }

        public void Save(Subset selectedSubset, string entityTypeIdentifier, int surveyChoiceId,
            string displayName, bool enabled, DateTimeOffset? startDate, string imageUrl, bool validate = true)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            Save(selectedSubset, entityTypeIdentifier, surveyChoiceId, displayName, enabled, startDate, imageUrl, dbContext, validate, false);
            dbContext.SaveChanges();
        }

        public void Save(Subset selectedSubset, string entityTypeIdentifier, int surveyChoiceId,
            string displayName, bool enabled, DateTimeOffset? startDate, string imageUrl, MetaDataContext dbContext, bool validate)
        {
            Save(selectedSubset, entityTypeIdentifier, surveyChoiceId, displayName, enabled, startDate, imageUrl, dbContext, validate, true);
        }

        private void Save(Subset selectedSubset, string entityTypeIdentifier, int surveyChoiceId,
            string displayName, bool enabled, DateTimeOffset? startDate, string imageURL, MetaDataContext dbContext, bool validate, bool checkLocal)
        {
            displayName = VerifyLengthAndTruncate(displayName, SqlTypeConstants.DefaultVarcharLength);
            if (validate && enabled)
            {
                Validate(entityTypeIdentifier, displayName, selectedSubset);
            }

            var existing = ForTypeAndSurveyChoiceId(ForProductContext(dbContext.EntityInstanceConfigurations), entityTypeIdentifier, surveyChoiceId);
            if (checkLocal && existing is null)
            {
                existing = ForTypeAndSurveyChoiceId(ForProductContext(dbContext.EntityInstanceConfigurations.Local.AsQueryable()),
                    entityTypeIdentifier, surveyChoiceId);
            }

            Save(selectedSubset, entityTypeIdentifier, surveyChoiceId, displayName, enabled, startDate, imageURL, dbContext, existing);
        }

        public EntityInstanceConfiguration Save(Subset selectedSubset, string entityTypeIdentifier, int surveyChoiceId, string displayName,
            bool enabled, DateTimeOffset? startDate, string imageUrl, MetaDataContext dbContext, EntityInstanceConfiguration existing)
        {
            if (existing != null)
            {
                if (existing.DisplayNameOverrideBySubset == null)
                {
                    existing.DisplayNameOverrideBySubset = new Dictionary<string, string>
                        { { selectedSubset.Id, displayName } };
                }
                else
                {
                    existing.DisplayNameOverrideBySubset[selectedSubset.Id] = displayName;
                }

                existing.EnabledBySubset[selectedSubset.Id] = enabled;
                if (startDate.HasValue)
                {
                    existing.StartDateBySubset[selectedSubset.Id] = startDate.Value;
                }
                else
                {
                    existing.StartDateBySubset.Remove(selectedSubset.Id);
                }
                existing.ImageURL = imageUrl;
                return existing;
            }
            else
            {
                var newInstance = new EntityInstanceConfiguration
                {
                    ProductShortCode = _productContext.ShortCode, 
                    SubProductId = _productContext.SubProductId, 
                    EntityTypeIdentifier = entityTypeIdentifier,
                    SurveyChoiceId = surveyChoiceId,
                    DisplayNameOverrideBySubset = new Dictionary<string, string> {{ selectedSubset.Id, displayName } },
                    EnabledBySubset = new Dictionary<string, bool>{{selectedSubset.Id, enabled}},
                    StartDateBySubset = startDate.HasValue ?
                        new Dictionary<string, DateTimeOffset>{{selectedSubset.Id, startDate.Value}} : 
                        new Dictionary<string, DateTimeOffset>(),
                    ImageURL = imageUrl,
                };
                dbContext.EntityInstanceConfigurations.Add(newInstance);
                return newInstance;
            }
        }

        private EntityInstanceConfiguration ForTypeAndSurveyChoiceId(
            IQueryable<EntityInstanceConfiguration> entityInstanceConfigurations, string entityTypeIdentifier,
            int surveyChoiceId)
        {
            return entityInstanceConfigurations
                .SingleOrDefault(c => c.SurveyChoiceId == surveyChoiceId && c.EntityTypeIdentifier == entityTypeIdentifier);
        }

        private void Validate(string entityTypeIdentifier, string name, Subset subset)
        {
            if (name.Length > SqlTypeConstants.DefaultVarcharLength)
            {
                throw new ArgumentException($"\"{name}\" exceeds maximum length of {SqlTypeConstants.DefaultVarcharLength} characters");
            }

            bool alreadyInMemory = _entityRepository.GetInstancesOf(entityTypeIdentifier, subset)
                .Any(e => e.Name == name);

            if (alreadyInMemory)
            {
                throw new BadRequestException($"\"{name}\" already exists");
            }
        }

        public IReadOnlyCollection<EntityInstanceConfiguration> GetEntityInstances(string entityTypeIdentifier)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return EntityInstanceConfigurations(dbContext)
                .Where(e => e.EntityTypeIdentifier.Equals(entityTypeIdentifier))
                .ToList();
        }
        
        public IReadOnlyCollection<EntityInstanceConfiguration> GetEntityInstances()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return EntityInstanceConfigurations(dbContext)
                .ToList();
        }

        public IQueryable<EntityInstanceConfiguration> EntityInstanceConfigurations(MetaDataContext dbContext, bool withTracking = false)
        {
            IQueryable<EntityInstanceConfiguration> dbContextEntityInstanceConfigurations = dbContext.EntityInstanceConfigurations;
            if (!withTracking)
            {
                dbContextEntityInstanceConfigurations =
                    dbContextEntityInstanceConfigurations.AsNoTrackingWithIdentityResolution();
            }

            return ForProductContext(dbContextEntityInstanceConfigurations);
        }

        private IQueryable<EntityInstanceConfiguration> ForProductContext(IQueryable<EntityInstanceConfiguration> dbContextEntityInstanceConfigurations)
        {
            return dbContextEntityInstanceConfigurations.Where(p =>
                p.ProductShortCode == _productContext.ShortCode && p.SubProductId == _productContext.SubProductId);
        }
    }
}
