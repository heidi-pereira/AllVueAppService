using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using Mixpanel;
using CustomerPortal.MixPanel;
using static CustomerPortal.MixPanel.MixPanel;
using System.Threading;
using MELT;
using System.Linq;

namespace CustomerPortal.Tests.MixPanel
{
    public class MixPanelTests
    {
        private const string DistinctId = "123";
        private const string IpAddress = "192.168.1.1";
        private Func<string, string> _message = (eventName) => $"Failed to track event {eventName}";
        private Mock<IMixpanelClient> _mockMixpanelClient;
        private ILogger<MixPanelLogger> _logger;
        private ITestLoggerFactory _loggerFactory;

        [SetUp]
        public void Setup()
        {
            _mockMixpanelClient = new Mock<IMixpanelClient>();
            _loggerFactory = TestLoggerFactory.Create();
            _logger = _loggerFactory.CreateLogger<MixPanelLogger>();
            Init(_mockMixpanelClient.Object, _logger, "TestProduct");
        }

        [Test]
        public void Init_WithNullParameters_ThrowsNoException()
        {
            Assert.Throws<ArgumentNullException>(() => Init(null, null, null));
        }

        [Test]
        public async Task TrackAsync_WithValidParameters_CallsClientTrackAsync()
        {
            // Arrange
            var model = new TrackAsyncEventModel(VueEvents.UploadedDocument, DistinctId, IpAddress);
            _mockMixpanelClient.Setup(x => x.TrackAsync(It.IsAny<string>(), It.IsAny<object>(), CancellationToken.None)).ReturnsAsync(true);

            // Act
            await TrackAsync(model);

            // Assert
            _mockMixpanelClient.Verify(x => x.TrackAsync(It.IsAny<string>(), It.IsAny<object>(), CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task TrackAsync_WithClientReturningFalse_LogsError()
        {
            // Arrange
            var model = new TrackAsyncEventModel(VueEvents.UploadedDocument, DistinctId, IpAddress);
            _mockMixpanelClient.Setup(x => x.TrackAsync(It.IsAny<string>(), It.IsAny<object>(), CancellationToken.None)).ReturnsAsync(false);

            // Act
            await TrackAsync(model);
            var log = _loggerFactory.Sink.LogEntries.Single();

            // Assert
            Assert.That(log.Message, Is.EqualTo(_message(model.EventName.ToString())));
        }
    }
}