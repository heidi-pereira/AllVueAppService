using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UserManagement.BackEnd.Application.Middleware;

namespace UserManagement.Tests
{
    public class ExceptionHandlingMiddlewareTests
    {
        private RequestDelegate? _next;
        private ILogger<ExceptionHandlingMiddleware>? _logger;
        private ExceptionHandlingMiddleware? _middleware;
        private DefaultHttpContext? _context;

        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
            _context = new DefaultHttpContext();
        }

        [Test]
        public async Task InvokeAsync_NoException_CallsNextMiddleware()
        {
            var wasCalled = false;
            _next = ctx =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            };

            _middleware = new ExceptionHandlingMiddleware(_next, _logger!);

            await _middleware.InvokeAsync(_context!);

            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public async Task InvokeAsync_ExceptionThrown_UsesCorrectStrategy()
        {
            var expectedException = new NotFoundException("Resource not found");
            _next = ctx => throw expectedException;

            var notFoundStrategy = Substitute.For<IExceptionHandlerStrategy>();
            notFoundStrategy.CanHandle(expectedException).Returns(true);
            notFoundStrategy.HandleAsync(_context!, expectedException).Returns(Task.CompletedTask);

            var validationStrategy = Substitute.For<IExceptionHandlerStrategy>();
            validationStrategy.CanHandle(Arg.Any<Exception>()).Returns(false);

            var defaultStrategy = Substitute.For<IExceptionHandlerStrategy>();
            defaultStrategy.CanHandle(Arg.Any<Exception>()).Returns(false);

            var middleware = new ExceptionHandlingMiddleware(_next, _logger!);

            // Inject test strategies
            typeof(ExceptionHandlingMiddleware)
                .GetField("_strategies", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(middleware, new List<IExceptionHandlerStrategy>
                {
                    notFoundStrategy,
                    validationStrategy,
                    defaultStrategy
                });

            await middleware.InvokeAsync(_context!);

            await notFoundStrategy.Received(1).HandleAsync(_context!, expectedException);
            await validationStrategy.DidNotReceiveWithAnyArgs().HandleAsync(null!, null!);
            await defaultStrategy.DidNotReceiveWithAnyArgs().HandleAsync(null!, null!);
        }
    }

    // Dummy custom exceptions to support testing
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }
}
