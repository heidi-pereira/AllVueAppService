using NUnit.Framework;
using NSubstitute;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mixpanel;
using MELT;
using System.Linq;
using System;
using System.Collections.Generic;
using BrandVue.MixPanel;
using static BrandVue.MixPanel.MixPanel;

namespace Test.BrandVue.FrontEnd.MixPanel
{
    public class MixPanelTests
    {
        private Func<string, string> _message = (eventName) => $"Failed to track event {eventName}";
        private IMixpanelClient _client;
        private ILogger<MixPanelLogger> _logger;
        private ITestLoggerFactory _loggerFactory;

        [SetUp]
        public void Setup()
        {
            _client = Substitute.For<IMixpanelClient>();
            _loggerFactory = TestLoggerFactory.Create();
            _logger = _loggerFactory.CreateLogger<MixPanelLogger>();
            Init(_client, _logger, "testProduct");
        }

        [Test]
        public void TrackAsync_AllEnumsInDictionary()
        {
            foreach (var e in Enum.GetValues<VueEvents>())
            {
                if (!VueEventsDictionary._eventsProperties.ContainsKey(e))
                {
                    Assert.Fail($"Enum {e} is not in _eventsProperties");
                }
            }
        }

        [Test]
        public async Task TrackAsync_WhenCalled_ShouldCallClientTrackAsync()
        {
            // Arrange
            var eventName = VueEvents.CreatedNewMetric;
            var distinctId = "testDistinctId";
            var ipAddress = "testIpAddress";
            var model = new TrackAsyncEventModel(eventName, distinctId, ipAddress);

            // Act
            await TrackAsync(model);

            // Assert
            await _client.Received(1).TrackAsync("Created New Metric", Arg.Any<object>());
        }

        [Test]
        public async Task TrackAsync_WhenFailed_ShouldLogError()
        {
            // Arrange
            var eventName = VueEvents.CreateBaseVariable;
            var distinctId = "testDistinctId";
            var ipAddress = "testIpAddress";
            var model = new TrackAsyncEventModel(eventName, distinctId, ipAddress);
            _client.TrackAsync(Arg.Any<string>(), Arg.Any<object>()).Returns(false);

            // Act
            await TrackAsync(model);
            var log = _loggerFactory.Sink.LogEntries.Single();

            // Assert
            Assert.That(log.Message, Is.EqualTo(_message(eventName.ToString())));
        }

        [Test]
        public async Task TrackAsync_CallsTrackAsyncWithCorrectObject()
        {
            // Arrange
            var eventName = VueEvents.CreateBaseVariable;
            var distinctId = "testDistinctId";
            var ipAddress = "testIpAddress";
            var additionalProps = new Dictionary<string, object>
                {
                    { "AdditionalProp1", "Value1" },
                    { "AdditionalProp2", 2 }
                };
            var model = new TrackAsyncEventModel(eventName, distinctId, ipAddress, additionalProps);

            // Act
            await TrackAsync(model);

            // Assert
            await _client.Received(1).TrackAsync(
                Arg.Is<string>(s => s == "Create Base Variable"),
                Arg.Is<object>(o => CheckProperties(o, model)) // Custom method to check properties
            );
        }

        private bool CheckProperties(object properties, TrackAsyncEventModel model)
        {
            // Convert ExpandoObject to Dictionary
            var propertiesDict = properties as IDictionary<string, object>;
            if (propertiesDict == null) return false;

            // Check distinct_id and ip
            if (!propertiesDict.TryGetValue("distinct_id", out var distinctId) || distinctId.ToString() != model.DistinctId)
                return false;
            if (!propertiesDict.TryGetValue("ip", out var ip) || ip.ToString() != model.IpAddress)
                return false;

            // Check additional properties
            foreach (var additionalProp in model.AdditionalProps)
            {
                if (!propertiesDict.TryGetValue(additionalProp.Key, out var value) || !value.Equals(additionalProp.Value))
                    return false;
            }

            // If all checks pass
            return true;
        }
    }
}