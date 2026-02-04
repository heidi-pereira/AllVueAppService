using System.Collections.Concurrent;
using System.IO;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Import;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BrandVue.SourceData.QuotaCells
{
    public class JsonReferenceWeightingFactory : IQuotaCellReferenceWeightingRepository
    {
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<Subset, QuotaCellReferenceWeightings> _weightings
            = new ConcurrentDictionary<Subset, QuotaCellReferenceWeightings>();

        private readonly IBrandVueDataLoaderSettings _settings;

        public JsonReferenceWeightingFactory(IBrandVueDataLoaderSettings settings, ILogger<JsonReferenceWeightingFactory> logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public QuotaCellReferenceWeightings LoadOrNullIfNotExists(Subset subset)
        {
            var pathToReferenceWeightingFile = _settings.WeightingsFilepath(subset);

            if (File.Exists(pathToReferenceWeightingFile))
            {
                _logger.LogInformation("Loading reference weightings for {@Subset} from {Path}", subset, pathToReferenceWeightingFile);
            }
            else
            {
                _logger.LogWarning("Not loading {@Subset} because {Path} doesn't exist",
                    subset, pathToReferenceWeightingFile);
                return null;
            }

            using (var stream = new FileStream(
                pathToReferenceWeightingFile,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
            {
                return Load(subset, stream);
            }
        }

        private QuotaCellReferenceWeightings Load(Subset subset, FileStream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return Load(subset, reader);
            }
        }

        public QuotaCellReferenceWeightings Load(Subset subset, StreamReader reader)
        {
            var raw = reader.ReadToEnd();
            var loaded = JsonConvert.DeserializeObject<dynamic>(raw) as JObject;
            var dictionary = loaded?.ToObject<Dictionary<string, double>>();
            var reShappedDictionary = dictionary?.ToDictionary(x => x.Key, x => WeightingValue.StandardWeighting(x.Value));
            var weightingsDictionary = new QuotaCellReferenceWeightings(reShappedDictionary);
            _weightings.TryAdd(subset, weightingsDictionary);
            return weightingsDictionary;
        }

        public QuotaCellReferenceWeightings Get(Subset subset)
        {
            try
            {
                _weightings.TryGetValue(subset, out var weightings);
                return weightings;
            }
            catch (KeyNotFoundException knfe)
            {
                throw new KeyNotFoundException(
                    $"No reference weightings for survey segment {subset}: you may not have loaded them.",
                    knfe);
            }
        }
    }
}
