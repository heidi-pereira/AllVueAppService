using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using UserManagement.BackEnd.WebApi.Attributes;
using UserManagement.BackEnd.WebApi.Middleware;

namespace UserManagement.Tests.Middleware
{
    public class InternalTokenValidationMiddlewareTests
    {
        private const string RequiredToken = "secret-token";
        private RequestDelegate _next;
        private IConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            _next = Substitute.For<RequestDelegate>();
            _configuration = Substitute.For<IConfiguration>();
            _configuration["Api:Token"].Returns(RequiredToken);
        }

        private static Endpoint CreateEndpointWithAttribute(bool withAttribute)
        {
            var metadata = withAttribute
                ? new EndpointMetadataCollection(new RequireInternalTokenAttribute())
                : new EndpointMetadataCollection();
            return new Endpoint(_ => Task.CompletedTask, metadata, "test");
        }

        [Test]
        public async Task InvokeAsync_TokenRequiredAndValidToken_AllowsRequest()
        {
            var context = new DefaultHttpContext();
            context.SetEndpoint(CreateEndpointWithAttribute(true));
            context.Request.Headers["Authorization"] = $"Bearer {RequiredToken}";

            var middleware = new InternalTokenValidationMiddleware(_next, _configuration);

            await middleware.InvokeAsync(context);

            await _next.Received()(context);
            Assert.That(context.Response.StatusCode == 0 ? 200 : context.Response.StatusCode, Is.EqualTo(200));
        }

        [Test]
        public async Task InvokeAsync_TokenRequiredAndMissingHeader_Returns401()
        {
            var context = new DefaultHttpContext();
            context.SetEndpoint(CreateEndpointWithAttribute(true));

            var middleware = new InternalTokenValidationMiddleware(_next, _configuration);

            await middleware.InvokeAsync(context);

            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status401Unauthorized));
            await _next.DidNotReceive()(context);
        }

        [Test]
        public async Task InvokeAsync_TokenRequiredAndInvalidToken_Returns403()
        {
            var context = new DefaultHttpContext();
            context.SetEndpoint(CreateEndpointWithAttribute(true));
            context.Request.Headers["Authorization"] = "Bearer wrong-token";

            var middleware = new InternalTokenValidationMiddleware(_next, _configuration);

            await middleware.InvokeAsync(context);

            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
            await _next.DidNotReceive()(context);
        }

        [Test]
        public async Task InvokeAsync_TokenNotRequired_AllowsRequest()
        {
            var context = new DefaultHttpContext();
            context.SetEndpoint(CreateEndpointWithAttribute(false));

            var middleware = new InternalTokenValidationMiddleware(_next, _configuration);

            await middleware.InvokeAsync(context);

            await _next.Received()(context);
            Assert.That(context.Response.StatusCode == 0 ? 200 : context.Response.StatusCode, Is.EqualTo(200));
        }
    }
}
