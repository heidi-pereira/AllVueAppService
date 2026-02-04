using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserPermissionFeatures;
using UserManagement.BackEnd.WebApi.Controllers.Internal;
using Vue.Common.Auth.Permissions;

namespace UserManagement.Tests.WebApi.Controllers.Internal
{
    [TestFixture]
    public class InternalUserFeaturePermissionControllerTests
    {
        private IMediator _mediatorSub = null!;
        private InternalUserFeaturePermissionController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _mediatorSub = Substitute.For<IMediator>();
            _controller = new InternalUserFeaturePermissionController(_mediatorSub);
        }

        [Test]
        public async Task GetAllUserFeatures_CallsMediatorWithCorrectQuery()
        {
            // Arrange
            var userId = "test-user-id";
            var expectedResult = new List<PermissionFeatureOptionDto>
            {
                new PermissionFeatureOptionDto(1, "Feature1"),
                new PermissionFeatureOptionDto(2, "Feature2")
            };
            _mediatorSub.Send(Arg.Any<GetAllUserPermissionOptionsQuery>()).Returns(expectedResult);

            // Act
            var result = await _controller.GetAllUserFeatures(userId, "User");

            // Assert
            await _mediatorSub.Received(1).Send(Arg.Is<GetAllUserPermissionOptionsQuery>(q => q.UserId == userId));
            Assert.That(result, Is.TypeOf<ActionResult<IEnumerable<PermissionFeatureOptionDto>>>());
            
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult?.Value, Is.EqualTo(expectedResult));
        }

        [Test]
        public async Task GetAllUserFeatures_ReturnsOkResult()
        {
            // Arrange
            var userId = "test-user-id";
            var expectedResult = new List<PermissionFeatureOptionDto>
            {
                new PermissionFeatureOptionDto(1, "Feature1")
            };
            _mediatorSub.Send(Arg.Any<GetAllUserPermissionOptionsQuery>()).Returns(expectedResult);

            // Act
            var result = await _controller.GetAllUserFeatures(userId, "User");

            // Assert
            Assert.That(result, Is.Not.Null);
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult?.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task GetAllUserFeatures_ReturnsCorrectData()
        {
            // Arrange
            var userId = "test-user-123";
            var expectedData = new List<PermissionFeatureOptionDto>
            {
                new PermissionFeatureOptionDto(1, "AllVue Access"),
                new PermissionFeatureOptionDto(2, "Report Builder"),
                new PermissionFeatureOptionDto(3, "User Management")
            };
            _mediatorSub.Send(Arg.Any<GetAllUserPermissionOptionsQuery>()).Returns(expectedData);

            // Act
            var result = await _controller.GetAllUserFeatures(userId, "User");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var returnedData = okResult?.Value as IEnumerable<PermissionFeatureOptionDto>;
            Assert.That(returnedData, Is.Not.Null);
            Assert.That(returnedData?.Count(), Is.EqualTo(3));
            
            var dataList = returnedData?.ToList();
            Assert.That(dataList?[0].Id, Is.EqualTo(1));
            Assert.That(dataList?[0].Name, Is.EqualTo("AllVue Access"));
            
            Assert.That(dataList?[1].Id, Is.EqualTo(2));
            Assert.That(dataList?[1].Name, Is.EqualTo("Report Builder"));
            
            Assert.That(dataList?[2].Id, Is.EqualTo(3));
            Assert.That(dataList?[2].Name, Is.EqualTo("User Management"));
        }

        [Test]
        public async Task GetAllUserFeatures_HandlesEmptyResult()
        {
            // Arrange
            var userId = "user-with-no-permissions";
            var emptyResult = new List<PermissionFeatureOptionDto>();
            _mediatorSub.Send(Arg.Any<GetAllUserPermissionOptionsQuery>()).Returns(emptyResult);

            // Act
            var result = await _controller.GetAllUserFeatures(userId, "User");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var returnedData = okResult?.Value as IEnumerable<PermissionFeatureOptionDto>;
            Assert.That(returnedData, Is.Not.Null);
            Assert.That(returnedData, Is.Empty);
        }

        [Test]
        public async Task GetAllUserFeatures_PassesUserIdCorrectly()
        {
            // Arrange
            var userId = "specific-user-123";
            var expectedResult = new List<PermissionFeatureOptionDto>();
            _mediatorSub.Send(Arg.Any<GetAllUserPermissionOptionsQuery>()).Returns(expectedResult);

            // Act
            await _controller.GetAllUserFeatures(userId, "User");

            // Assert
            await _mediatorSub.Received(1).Send(Arg.Is<GetAllUserPermissionOptionsQuery>(
                query => query.UserId == userId
            ));
        }

        [Test]
        public async Task GetAllUserFeatures_WithNullUserId_StillCallsMediator()
        {
            // Arrange
            string? userId = null;
            var expectedResult = new List<PermissionFeatureOptionDto>();
            _mediatorSub.Send(Arg.Any<GetAllUserPermissionOptionsQuery>()).Returns(expectedResult);

            // Act
            var result = await _controller.GetAllUserFeatures(userId, "User");

            // Assert
            await _mediatorSub.Received(1).Send(Arg.Is<GetAllUserPermissionOptionsQuery>(
                query => query.UserId == userId
            ));
            
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
        }

        [Test]
        public async Task GetAllUserFeatures_WithEmptyUserId_StillCallsMediator()
        {
            // Arrange
            var userId = string.Empty;
            var expectedResult = new List<PermissionFeatureOptionDto>();
            _mediatorSub.Send(Arg.Any<GetAllUserPermissionOptionsQuery>()).Returns(expectedResult);

            // Act
            var result = await _controller.GetAllUserFeatures(userId, "User");

            // Assert
            await _mediatorSub.Received(1).Send(Arg.Is<GetAllUserPermissionOptionsQuery>(
                query => query.UserId == userId
            ));
            
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
        }

        [Test]
        public async Task GetAllUserFeatures_ReturnsGenericTypeCorrectly()
        {
            // Arrange
            var userId = "test-user";
            var expectedResult = new List<PermissionFeatureOptionDto>
            {
                new PermissionFeatureOptionDto(42, "Test Feature")
            };
            _mediatorSub.Send(Arg.Any<GetAllUserPermissionOptionsQuery>()).Returns(expectedResult);

            // Act
            var result = await _controller.GetAllUserFeatures(userId,"User");

            // Assert
            Assert.That(result, Is.TypeOf<ActionResult<IEnumerable<PermissionFeatureOptionDto>>>());
            
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var returnedData = okResult.Value as IEnumerable<PermissionFeatureOptionDto>;
            Assert.That(returnedData, Is.Not.Null);
            Assert.That(returnedData.First().Id, Is.EqualTo(42));
            Assert.That(returnedData.First().Name, Is.EqualTo("Test Feature"));
        }

        [Test]
        public async Task GetAllUserFeatures_ReturnsPolymorphicTypeCorrectly()
        {
            // Arrange
            var userId = "test-user";
            var expectedResult = new List<IPermissionFeatureOption>
            {
                new PermissionFeatureOptionDto(1, "Feature1"),
                new PermissionFeatureOptionDto(2, "Feature2")
            };
            _mediatorSub.Send(Arg.Any<GetAllUserPermissionOptionsQuery>()).Returns(expectedResult);

            // Act
            var result = await _controller.GetAllUserFeatures(userId, "User");

            // Assert
            var okResult = result.Result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            
            var returnedData = okResult?.Value as IEnumerable<IPermissionFeatureOption>;
            Assert.That(returnedData, Is.Not.Null);
            Assert.That(returnedData?.Count(), Is.EqualTo(2));
        }
    }
}
