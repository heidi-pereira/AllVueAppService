#nullable enable

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mixpanel;
using static CustomerPortal.MixPanel.VueEventsDictionary;

namespace CustomerPortal.MixPanel
{
    public class MixPanelLogger
    {
    }

    public static class MixPanel
    {
        private static IMixpanelClient? _client;
        private static ILogger<MixPanelLogger> _logger;
        private static string _product;
        
        public static void Init(
            IMixpanelClient? client,
            ILogger<MixPanelLogger> logger,
            string product)
        {
            _client = client;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");;
            _product = product ?? throw new ArgumentNullException(nameof(product), "Product cannot be null.");
        }

        public static async Task TrackAsync(TrackAsyncEventModel model)
        {
            if (_logger is null || _product is null)
                throw new InvalidOperationException("MixPanel has not been initialized");

            if (_client != null)
            {
                object properties = AddMixPanelProps(
                    _eventsProperties[model.EventName],
                    model.DistinctId,
                    model.IpAddress,
                    model.AdditionalProps);
                var eventName = ConvertToSpaceSeparated(model.EventName.ToString());
                bool result = await _client.TrackAsync(eventName, properties);
                if (!result)
                    _logger.LogError("Failed to track event {eventName}", model.EventName);
            }
        }

        private static object AddMixPanelProps(
            object properties,
            string distinctId,
            string ipAddress,
            Dictionary<string, object>? additionalProps)
        {
            var propertiesDict = ObjectToDictionary(properties);
            propertiesDict.Add("time", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            propertiesDict.Add("distinct_id", distinctId);
            propertiesDict.Add("ip", ipAddress);
            propertiesDict.Add("Product", _product);
            // Add any additional properties
            if (additionalProps != null)
            {
                foreach (var prop in additionalProps)
                {
                    propertiesDict[prop.Key] = prop.Value; // Use indexer to avoid duplicate key exception
                }
            }
            return DictionaryToObject(propertiesDict);
        }

        private static object DictionaryToObject(Dictionary<string, object> dictionary)
        {
            var expandObj = new ExpandoObject();
            var expandDict = expandObj as IDictionary<string, object>;

            foreach (var keyValuePair in dictionary)
            {
                expandDict.Add(keyValuePair);
            }

            return expandObj;
        }

        public static Dictionary<string, object> ObjectToDictionary(object obj)
        {
            if (obj == null)
                return new Dictionary<string, object>();
            return obj.GetType()
                      .GetProperties()
                      .ToDictionary(prop => prop.Name, prop => prop.GetValue(obj))!;
        }

        private static string ConvertToSpaceSeparated(string input)
        {
            // Insert a space before each uppercase letter, except for the first character
            string spaced = Regex.Replace(input, "(?<!^)([A-Z])", " $1");
            return spaced;
        }
    }
}