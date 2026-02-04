using AuthServer.GeneratedAuthApi;
using BrandVue.EntityFramework;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Vue.AuthMiddleware;
using Vue.Common.Auth.Permissions;
using Vue.Common.AuthApi;
using Vue.Common.Constants.Constants;

namespace Test.BrandVue.FrontEnd
{
    public class SubProductSecurityRestrictionsTests
    {
        private static readonly string[] EmptySecurityGroups = Array.Empty<string>();
        private const string Savanta = "savanta";
        private const string AnotherCompany = "anothercompany";
        private const string ExternalCompany = "externalcompany";
        private const string GroupId1 = "group-id-1";
        private const string GroupId2 = "group-id-2";
        private const string ProjectId1 = "project-1";
        private const string UserId = "1234-5678-abcd-efgh";
        private const string ProjectId = "XXXX";

        private static readonly IList<SurveyCompanyShortCodeRequirement> ExternalCompanyOnly = [
            new(1, [ExternalCompany])];
        private static readonly IList<SurveyCompanyShortCodeRequirement> AnotherAndExternalCompanyOnly = [
            new(1,[AnotherCompany,ExternalCompany]),
        ];
        private static readonly IList<SurveyCompanyShortCodeRequirement> SavantaCompanyOnly = [
            new(1,[Savanta]),
        ];

        static IPermissionService PermissionServiceReturningNothing()
        {
            IPermissionService permissionsServiceReturningNothing = Substitute.For<IPermissionService>();
                permissionsServiceReturningNothing.GetUserDataPermissionForCompanyAndProjectAsync(default, default, default, default)
                    .ReturnsForAnyArgs(Task.FromResult<DataPermissionDto?>(null));
            return permissionsServiceReturningNothing;
        }

        static IPermissionService PermissionServiceReturningSomething()
        {
            IPermissionService permissionsServiceReturningNothing = Substitute.For<IPermissionService>();
            permissionsServiceReturningNothing.GetUserDataPermissionForCompanyAndProjectAsync(default, default, default, default)
                .ReturnsForAnyArgs(Task.FromResult<DataPermissionDto?>(new DataPermissionDto("Blank", new List<int>(), new List<DataPermissionFilterDto>())));
            return permissionsServiceReturningNothing;
        }

        private static IAuthApiClient AuthClientReturningFalse()
        {
            var authClientReturningFalse = Substitute.For<IAuthApiClient>();
            authClientReturningFalse.CanUserAccessProject(default, default, default, CancellationToken.None).ReturnsForAnyArgs(false);
            authClientReturningFalse.GetCompanyByShortcode(default, CancellationToken.None).ReturnsForAnyArgs(Task.FromResult<CompanyModel>(new CompanyModel()));
            return authClientReturningFalse;
        }
        private static IAuthApiClient AuthClientReturningTrue()
        {
            var authClientReturningFalse = Substitute.For<IAuthApiClient>();
            authClientReturningFalse.CanUserAccessProject(default, default, default, CancellationToken.None).ReturnsForAnyArgs(true);
            authClientReturningFalse.GetCompanyByShortcode(default, CancellationToken.None).ReturnsForAnyArgs(Task.FromResult<CompanyModel>(new CompanyModel()));
            return authClientReturningFalse;
        }

        private static IEnumerable<TestCaseData> ShouldAuthorizeCases()
        {

            //Group claim tests for Savanta organisations
            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted(GroupId1.Yield(), ExternalCompanyOnly, ProjectId, AuthClientReturningTrue(), PermissionServiceReturningNothing()), CreateClaimsFor(Roles.User, Savanta, GroupId1))
                .SetName($"Require group id match between sub product and group claim for {Savanta} org");

            //Beta or Live environment cases where we test company requirements and enabled flag
            yield return new TestCaseData(SubProductSecurityRestrictions.Unrestricted(), CreateClaimsFor(Roles.User, ExternalCompany))
                .SetName("For BrandVue should authorize by default as there are no sub-products");

            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted(EmptySecurityGroups, ExternalCompanyOnly, ProjectId, AuthClientReturningTrue(), PermissionServiceReturningNothing()), CreateClaimsFor(Roles.User, ExternalCompany))
                .SetName("Require company equivalence match between restrictions and company claim");

            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted(EmptySecurityGroups, AnotherAndExternalCompanyOnly, ProjectId, AuthClientReturningTrue(), PermissionServiceReturningNothing()), CreateClaimsFor(Roles.User, ExternalCompany))
                .SetName("Require company equivalence match between restrictions and company claim");

            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted(EmptySecurityGroups, AnotherAndExternalCompanyOnly, ProjectId, AuthClientReturningFalse(), PermissionServiceReturningSomething()), CreateClaimsFor(Roles.User, ExternalCompany))
                .SetName("Require company equivalence match between restrictions and company claim");

            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted(EmptySecurityGroups,
                    [
                        new SurveyCompanyShortCodeRequirement(1,[AnotherCompany,ExternalCompany]),
                        new SurveyCompanyShortCodeRequirement(1,[Savanta,ExternalCompany]),
                    ],
                    ProjectId, AuthClientReturningTrue(), PermissionServiceReturningNothing()), CreateClaimsFor(Roles.User, ExternalCompany))
                .SetName("Require company equivalence match between restrictions and company claim");
            
            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted(EmptySecurityGroups, ExternalCompanyOnly, ProjectId, AuthClientReturningTrue(), PermissionServiceReturningNothing()), CreateClaimsFor(Roles.User, Savanta, GroupId1))
                .SetName("Savanta should be authorised to see external company sub-product");
            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted(EmptySecurityGroups, ExternalCompanyOnly, ProjectId, AuthClientReturningTrue(), PermissionServiceReturningNothing()), CreateClaimsFor(Roles.User, Savanta))
                .SetName("Savanta should be authorised to see external company sub-product, even when sub-product not enabled");
        }

        private static IEnumerable<TestCaseData> ShouldNotAuthorizeCases()
        {
            //Group claim tests for Savanta organisations
            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted([GroupId1, GroupId2], ExternalCompanyOnly, ProjectId, AuthClientReturningFalse(), PermissionServiceReturningNothing()), CreateClaimsFor(Roles.User, Savanta, GroupId1))
                .SetName($"Group id mismatch between sub product and group claim For {Savanta} org");

            //Beta or Live environment cases where we test company requirements and enabled flag
            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted(EmptySecurityGroups, SavantaCompanyOnly, ProjectId, AuthClientReturningFalse(), PermissionServiceReturningNothing()), CreateClaimsFor(Roles.User, ExternalCompany))
                .SetName("Company equivalence mismatch between restrictions and company claim");
            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted(EmptySecurityGroups,
                    [
                        new SurveyCompanyShortCodeRequirement(1,[AnotherCompany]),
                        new SurveyCompanyShortCodeRequirement(2,[ExternalCompany]),
                    ], ProjectId, AuthClientReturningFalse(), PermissionServiceReturningNothing()), CreateClaimsFor(Roles.User, ExternalCompany))
                .SetName("Multiple company restriction is not yet supported");
            yield return new TestCaseData(SubProductSecurityRestrictions.Restricted(EmptySecurityGroups, ExternalCompanyOnly, ProjectId, AuthClientReturningFalse(), PermissionServiceReturningNothing()), CreateClaimsFor(Roles.User, AnotherCompany))
                .SetName("Should not be authorised to see other company's sub-product");
        }

        [Test, TestCaseSource(nameof(ShouldAuthorizeCases))]
        public void TestAuthorized(ISubProductSecurityRestrictions subProductSecurityRestrictions, IEnumerable<Claim> claims)
        {
            Assert.That(subProductSecurityRestrictions.IsAuthorizedForThisOrganisation(claims.ToArray()), Is.True);
        }

        [Test, TestCaseSource(nameof(ShouldAuthorizeCases))]
        public async Task TestIsAuthorizedForThisProject(ISubProductSecurityRestrictions subProductSecurityRestrictions, IEnumerable<Claim> claims)
        {
            Assert.That(await subProductSecurityRestrictions.IsAuthorizedForThisProject(claims.ToArray(), CancellationToken.None), Is.True);
        }

        [Test, TestCaseSource(nameof(ShouldNotAuthorizeCases))]
        public void TestNotAuthorized(ISubProductSecurityRestrictions subProductSecurityRestrictions, IEnumerable<Claim> claims)
        {
            Assert.That(subProductSecurityRestrictions.IsAuthorizedForThisOrganisation(claims.ToArray()), Is.False);
        }

        [Test]
        public async Task ShouldAllowAccessWhenUserCanAccessProjectViaLegacy()
        {
            var authClientReturningTrue = Substitute.For<IAuthApiClient>();
            authClientReturningTrue.CanUserAccessProject(default, default, default, CancellationToken.None).ReturnsForAnyArgs(true);
            var securityRestrictions = SubProductSecurityRestrictions.Restricted(
                [], [], ProjectId1, authClientReturningTrue, PermissionServiceReturningNothing());
            var claims = CreateClaimsFor(Roles.User, ExternalCompany);
            Assert.That(await securityRestrictions.IsAuthorizedForThisProject(claims.ToArray(), CancellationToken.None), Is.True);
        }

        [Test]
        public async Task ShouldDisallowAccessWhenUserCannotAccessProject()
        {
            var securityRestrictions = SubProductSecurityRestrictions.Restricted(
                [], [], ProjectId1, AuthClientReturningFalse(), PermissionServiceReturningNothing());
            var claims = CreateClaimsFor(Roles.User, ExternalCompany);
            Assert.That(await securityRestrictions.IsAuthorizedForThisProject(claims.ToArray(), CancellationToken.None), Is.False);
        }


        private static IEnumerable<Claim> CreateClaimsFor(string role, string organisation, string group = null)
        {
            yield return new Claim(RequiredClaims.UserId, UserId);
            yield return new Claim(RequiredClaims.Role, role);
            yield return new Claim(RequiredClaims.CurrentCompanyShortCode, organisation);
            if (group is { }) yield return new Claim(OptionalClaims.Groups, group);
        }
    }
}
