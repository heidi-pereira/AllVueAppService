using MediatR;
using Microsoft.AspNetCore.Mvc;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.AssignUserRole;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserFeaturePermissions;
using UserManagement.BackEnd.WebApi.Controllers;

namespace UserManagement.BackEnd.Tests.WebApi.Controllers
{
    [TestFixture]
    public class UserFeaturePermissionControllerTests
    {
        private IMediator _mediatorSub = null!;
        private UserFeaturePermissionController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _mediatorSub = Substitute.For<IMediator>();
            _controller = new UserFeaturePermissionController(_mediatorSub);
        }

        [Test]
        public async Task GetAllUserFeaturePermissions_CallsMediatorWithCorrectQuery()
        {
            // Arrange
            var expectedResult = new List<UserFeaturePermissionDto>
            {
                new UserFeaturePermissionDto(1, "user1", 1, "admin", DateTime.UtcNow),
                new UserFeaturePermissionDto(2, "user2", 2, "admin", DateTime.UtcNow)
            };
            _mediatorSub.Send(Arg.Any<GetAllUserFeaturePermissionsQuery>()).Returns(expectedResult);

            // Act
            var result = await _controller.GetAllUserFeaturePermissions();

            // Assert
            await _mediatorSub.Received(1).Send(Arg.Any<GetAllUserFeaturePermissionsQuery>());
            Assert.That(result, Is.TypeOf<ActionResult<IEnumerable<UserFeaturePermissionDto>>>());
            
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult?.Value, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task GetAllUserFeaturePermissions_ReturnsOkResult()
        {
            // Arrange
            var expectedResult = new List<UserFeaturePermissionDto>();
            _mediatorSub.Send(Arg.Any<GetAllUserFeaturePermissionsQuery>()).Returns(expectedResult);

            // Act
            var result = await _controller.GetAllUserFeaturePermissions();

            // Assert
            Assert.That(result, Is.Not.Null);
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult?.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GetAllUserFeaturePermissions_ReturnsCorrectData()
        {
            // Arrange
            var expectedData = new List<UserFeaturePermissionDto>
            {
                new UserFeaturePermissionDto(1, "test-user", 3, "admin@example.com", DateTime.UtcNow.AddDays(-1)),
                new UserFeaturePermissionDto(2, "another-user", 1, "system@example.com", DateTime.UtcNow.AddHours(-2))
            };
            _mediatorSub.Send(Arg.Any<GetAllUserFeaturePermissionsQuery>()).Returns(expectedData);

            // Act
            var result = await _controller.GetAllUserFeaturePermissions();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var returnedData = okResult.Value as IEnumerable<UserFeaturePermissionDto>;
            Assert.That(returnedData, Is.Not.Null);
            Assert.That(returnedData.Count(), Is.EqualTo(2));
            
            var dataList = returnedData.ToList();
            Assert.That(dataList[0].Id, Is.EqualTo(1));
            Assert.That(dataList[0].UserId, Is.EqualTo("test-user"));
            Assert.That(dataList[0].UserRoleId, Is.EqualTo(3));
            Assert.That(dataList[0].UpdatedByUserId, Is.EqualTo("admin@example.com"));
            
            Assert.That(dataList[1].Id, Is.EqualTo(2));
            Assert.That(dataList[1].UserId, Is.EqualTo("another-user"));
            Assert.That(dataList[1].UserRoleId, Is.EqualTo(1));
            Assert.That(dataList[1].UpdatedByUserId, Is.EqualTo("system@example.com"));
        }

        [Test]
        public async Task GetAllUserFeaturePermissions_HandlesEmptyResult()
        {
            // Arrange
            var emptyResult = new List<UserFeaturePermissionDto>();
            _mediatorSub.Send(Arg.Any<GetAllUserFeaturePermissionsQuery>()).Returns(emptyResult);

            // Act
            var result = await _controller.GetAllUserFeaturePermissions();

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var returnedData = okResult.Value as IEnumerable<UserFeaturePermissionDto>;
            Assert.That(returnedData, Is.Not.Null);
            Assert.That(returnedData, Is.Empty);
        }
    }
}
