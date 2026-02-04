using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetRolesByCompany;
using UserManagement.BackEnd.WebApi.Controllers;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.Tests.WebApi.Controllers
{
    [TestFixture]
    public class RolesControllerGetRolesByCompanyTests
    {
        private IMediator _mediatorSub;
        private RolesController _controller;

        [SetUp]
        public void SetUp()
        {
            _mediatorSub = Substitute.For<IMediator>();
            _controller = new RolesController(_mediatorSub);
        }

        [Test]
        public async Task GetRolesByCompany_ReturnsOkResult()
        {
            // Arrange
            var companyShortCode = "TESTCOMPANY";
            var expectedRoles = new List<RoleDto>
            {
                new RoleDto(1, "Admin", "TESTCOMPANY", new List<IPermissionFeatureOption>()),
                new RoleDto(2, "User", "TESTCOMPANY", new List<IPermissionFeatureOption>())
            };
            _mediatorSub.Send(Arg.Any<GetRolesByCompanyQuery>()).Returns(expectedRoles);

            // Act
            var result = await _controller.GetRolesByCompany(companyShortCode);

            // Assert
            Assert.That(result, Is.Not.Null);
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult?.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GetRolesByCompany_CallsMediatorWithCorrectQuery()
        {
            // Arrange
            var companyShortCode = "TESTCOMPANY";
            var expectedRoles = new List<RoleDto>();
            _mediatorSub.Send(Arg.Any<GetRolesByCompanyQuery>()).Returns(expectedRoles);

            // Act
            await _controller.GetRolesByCompany(companyShortCode);

            // Assert
            await _mediatorSub.Received(1).Send(Arg.Is<GetRolesByCompanyQuery>(q => q.CompanyId == companyShortCode));
        }

        [Test]
        public async Task GetRolesByCompany_ReturnsBadRequestForNullCompanyId()
        {
            // Act
            var result = await _controller.GetRolesByCompany(null);

            // Assert
            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult?.Value, Is.EqualTo("Company ID is required."));
        }

        [Test]
        public async Task GetRolesByCompany_ReturnsBadRequestForEmptyCompanyId()
        {
            // Act
            var result = await _controller.GetRolesByCompany("");

            // Assert
            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
            var badRequestResult = result.Result as BadRequestObjectResult;
            Assert.That(badRequestResult.Value, Is.EqualTo("Company ID is required."));
        }

        [Test]
        public async Task GetRolesByCompany_ReturnsCorrectData()
        {
            // Arrange
            var companyShortCode = "TESTCOMPANY";
            var expectedRoles = new List<RoleDto>
            {
                new RoleDto(1, "Admin", "TESTCOMPANY", new List<IPermissionFeatureOption>()),
                new RoleDto(2, "User", "CHILD1", new List<IPermissionFeatureOption>())
            };
            _mediatorSub.Send(Arg.Any<GetRolesByCompanyQuery>()).Returns(expectedRoles);

            // Act
            var result = await _controller.GetRolesByCompany(companyShortCode);

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var returnedRoles = okResult.Value as IEnumerable<RoleDto>;
            Assert.That(returnedRoles, Is.Not.Null);
            Assert.That(returnedRoles.Count(), Is.EqualTo(2));
            Assert.That(returnedRoles.Any(r => r.RoleName == "Admin"), Is.True);
            Assert.That(returnedRoles.Any(r => r.RoleName == "User"), Is.True);
        }
    }
}
