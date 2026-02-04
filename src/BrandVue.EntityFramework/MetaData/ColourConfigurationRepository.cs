using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.MetaData
{
    public class ColourConfigurationRepository : IColourConfigurationRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly Regex _hexColourRegex = new("^#[0-9A-F]{6}$");

        public ColourConfigurationRepository(IDbContextFactory<MetaDataContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public IReadOnlyCollection<ColourConfiguration> GetAllFor(string productShortCode, string organisation)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.ColourConfigurations.Where(c => c.ProductShortCode == productShortCode && c.Organisation == organisation).ToArray();
        }

        public IReadOnlyCollection<ColourConfiguration> GetFor(string productShortCode, string organisation, string entityType, IEnumerable<int> instanceIds)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.ColourConfigurations
                .Where(
                    c => c.ProductShortCode == productShortCode
                         && c.Organisation == organisation
                         && c.EntityType == entityType
                         && instanceIds.Contains(c.EntityInstanceId)
                ).ToArray();
        }

        public bool Save(string productShortCode, string organisation, string entityType, int instanceId, string colour)
        {
            if (!IsValidHexColour(colour))
            {
                return false;
            }

            using var dbContext = _dbContextFactory.CreateDbContext();
            var existing = dbContext.ColourConfigurations.Find(productShortCode, organisation, entityType, instanceId);
            if (existing != null)
            {
                existing.Colour = colour;
            }
            else
            {
                var newColourConfiguration = new ColourConfiguration
                {
                    ProductShortCode = productShortCode,
                    Organisation = organisation,
                    EntityType = entityType,
                    EntityInstanceId = instanceId,
                    Colour = colour
                };
                dbContext.ColourConfigurations.Add(newColourConfiguration);
            }

            dbContext.SaveChanges();
            return true;
        }

        public void Remove(string productShortCode, string organisation, string entityType, int instanceId)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var existing = dbContext.ColourConfigurations.Find(productShortCode, organisation, entityType, instanceId);
            if (existing != null)
            {
                dbContext.ColourConfigurations.Remove(existing);
                dbContext.SaveChanges();
            }
        }

        public bool IsValidHexColour(string colour)
        {
            return _hexColourRegex.IsMatch(colour);
        }
    }
}