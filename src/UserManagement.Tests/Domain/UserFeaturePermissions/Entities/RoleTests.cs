using BrandVue.EntityFramework.MetaData.Authorisation;
using DomainEntities = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

namespace UserManagement.BackEnd.Tests.Domain.UserFeaturePermissions.Entities
{
    [TestFixture]
    public class RoleTests
    {
        private const string UserRoleName = "User";
        private const string ManagerRoleName = "Manager";
        private const string AdminRoleName = "Admin";
        private const string Organisation = "OrgC";
        private const string UpdatedByUserId = "user3";

        [Test]
        public void Constructor_Sets_Properties_Correctly()
        {
            const string Organisation = "OrgA";
            const string UpdatedByUserId = "user1";
            var role = new DomainEntities.Role(ManagerRoleName, Organisation, UpdatedByUserId);
            Assert.That(role.RoleName, Is.EqualTo(ManagerRoleName));
            Assert.That(role.OrganisationId, Is.EqualTo(Organisation));
            Assert.That(role.UpdatedByUserId, Is.EqualTo(UpdatedByUserId));
            Assert.That(role.UpdatedDate, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(2)));
            Assert.That(role.Options.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_With_Id_Sets_Properties_Correctly()
        {
            const string Organisation = "OrgB";
            const string UpdatedByUserId = "user2";
            var role = new DomainEntities.Role(5, AdminRoleName, Organisation, UpdatedByUserId);
            Assert.That(role.Id, Is.EqualTo(5));
            Assert.That(role.RoleName, Is.EqualTo(AdminRoleName));
            Assert.That(role.OrganisationId, Is.EqualTo(Organisation));
            Assert.That(role.UpdatedByUserId, Is.EqualTo(UpdatedByUserId));
            Assert.That(role.UpdatedDate, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(2)));
        }

        [Test]
        public void AssignPermission_Adds_Option_If_Not_Exists()
        {
            var role = new DomainEntities.Role(UserRoleName, Organisation, UpdatedByUserId);
            var option = new DomainEntities.PermissionOption("Option", new DomainEntities.PermissionFeature("Feature", SystemKey.AllVue));
            role.AssignPermission(option);
            Assert.That(role.Options.Count, Is.EqualTo(1));
            Assert.That(option, Is.EqualTo(role.Options.First()));
        }

        [Test]
        public void AssignPermission_Does_Not_Add_Duplicate()
        {
            var role = new DomainEntities.Role(UserRoleName, Organisation, UpdatedByUserId);
            var option = new DomainEntities.PermissionOption("Option", new DomainEntities.PermissionFeature("Feature", SystemKey.AllVue));
            role.AssignPermission(option);
            role.AssignPermission(option);
            Assert.That(role.Options.Count, Is.EqualTo(1));
        }

        [Test]
        public void SetId_Updates_Id()
        {
            var role = new DomainEntities.Role(UserRoleName, Organisation, UpdatedByUserId);
            role.SetId(42);
            Assert.That(role.Id, Is.EqualTo(42));
        }
    }
}
