using EF = BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue;


namespace UserManagement.BackEnd.Domain.UserDataPermissions.Entities
{
    public class AllVueUserDataPermission(int id,string userId, AllVueRule rule, string updatedByUserId, DateTime updatedDate)
    {
        public int Id { get; private set; } = id;
        public string UserId { get; private set; } = userId;
        public int RuleId { get; private set; } = rule.Id;
        public AllVueRule AllVueRule { get; private set; } = rule;
        public string UpdatedByUserId { get; private set; } = updatedByUserId;
        public DateTime UpdatedDate { get; private set; } = updatedDate;

        protected AllVueUserDataPermission() : this(0, string.Empty, null, string.Empty, DateTime.MinValue)
        {

        }
    }

    public static class AllVueUserDataPermissionExtension
    {
        public static AllVueUserDataPermission ToUserDataPermission(this BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.UserDataPermission allVueUserDataPermission)
        {
            return new AllVueUserDataPermission(allVueUserDataPermission.Id,
                allVueUserDataPermission.UserId,
                   ((EF.AllVueRule)allVueUserDataPermission.Rule).ToAllVueRule(),
                allVueUserDataPermission.UpdatedByUserId,
                allVueUserDataPermission.UpdatedDate);
        }
    }
}