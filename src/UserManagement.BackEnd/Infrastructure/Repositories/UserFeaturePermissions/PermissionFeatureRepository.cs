using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions;
using Microsoft.EntityFrameworkCore;
using UserManagement.BackEnd.Application.UserFeaturePermissions;
using DomaineEntities = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

namespace UserManagement.BackEnd.Infrastructure.Repositories.UserFeaturePermissions
{
    public class PermissionFeatureRepository : IPermissionFeatureRepository
    {
        private readonly MetaDataContext _context;

        public PermissionFeatureRepository(MetaDataContext dbContext)
        {
            _context = dbContext;
        }

        public async Task<IEnumerable<DomaineEntities.PermissionFeature>> GetAllAsync(CancellationToken cancellationToken)
        {
            var result = await _context.PermissionFeatures.Include(f => f.Options).ToListAsync(cancellationToken);
            return result.Select(CreateDomainPermissionFeature);
        }

        private static DomaineEntities.PermissionFeature CreateDomainPermissionFeature(PermissionFeature f)
        {
            var domainFeature = new DomaineEntities.PermissionFeature(
                            f.Id,
                            f.Name,
                            f.SystemKey);

            foreach (var option in f.Options)
            {
                domainFeature.AddOption(new DomaineEntities.PermissionOption(
                    option.Id,
                    option.Name,
                    domainFeature
                ));
            }
            return domainFeature;
        }

        public async Task<DomaineEntities.PermissionFeature?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var result = await _context.PermissionFeatures.Include(f => f.Options).FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
            if (result == null)
            {
                throw new KeyNotFoundException($"Permission feature with ID {id} not found.");
            }
            return CreateDomainPermissionFeature(result);
        }
    }
}