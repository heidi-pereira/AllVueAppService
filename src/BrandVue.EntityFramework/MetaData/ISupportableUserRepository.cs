namespace BrandVue.EntityFramework.MetaData
{
    public interface ISupportableUserRepository
    {
        SupportableUser GetByUserId(string userId);
        void Create(string userId, string restoreId);
    }
}