using BrandVue.EntityFramework.MetaData;
using Microsoft.EntityFrameworkCore;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

namespace UserManagement.BackEnd.Infrastructure.Repositories.UserFeaturePermissions
{
    public class PermissionOptionRepository : IPermissionOptionRepository
    {
        private readonly MetaDataContext _context;

        public PermissionOptionRepository(MetaDataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PermissionOption>> GetAllAsync(CancellationToken cancellationToken)
        {
            var result = await _context.PermissionOptions.Include(po => po.Feature).ToListAsync(cancellationToken);
            return result.Select(PermissionOptionMapper.MapFromInfrastructure);
        }

        public async Task<IEnumerable<PermissionOption>> GetAllByIdsAsync(IEnumerable<int> optionIds, CancellationToken cancellationToken)
        {
            var result = await _context.PermissionOptions
                .Where(po => optionIds.Contains(po.Id))
                .Include(po => po.Feature) // <-- include related PermissionFeature
                .ToListAsync(cancellationToken);
            return result.Select(PermissionOptionMapper.MapFromInfrastructure);
        }
    }
}