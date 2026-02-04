using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.MetaData
{
    public class SupportableUserRepository : ISupportableUserRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;

        public SupportableUserRepository(IDbContextFactory<MetaDataContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public SupportableUser GetByUserId(string userId)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                return ctx.SupportableUsers.SingleOrDefault(u => u.UserId == userId);
            }
        }

        public void Create(string userId, string restoreId)
        {
            using (var ctx = _dbContextFactory.CreateDbContext())
            {
                if (GetByUserId(userId) != null)
                {
                    throw new Exception($"User with Id {userId} already exists");
                }

                ctx.SupportableUsers.Add(new SupportableUser
                    {DateCreated = DateTime.UtcNow, UserId = userId, FreshchatConversationId = restoreId});

                ctx.SaveChanges();
            }
        }
    }
}