using System.Globalization;
using System.IO;
using CsvHelper;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Import
{
    internal class SimpleCsvReader
    {
        private readonly ILogger _logger;

        public SimpleCsvReader(ILogger logger)
        {
            _logger = logger;
        }

        public T[] ReadCsv<T>(string filenameForSurveysCsv)
        {
            using var sr = new StreamReader(filenameForSurveysCsv);
            using var csv = new CsvReader(sr, CultureInfo.CurrentCulture);
            var config = csv.Context.ReaderConfiguration;
            config.AllowComments = true;
            config.ShouldSkipRecord = r => r[0].StartsWith("//");
            config.PrepareHeaderForMatch =  (string header, int index) => header.ToLower();
            config.ReadingExceptionOccurred = e =>
            {
                _logger.LogWarning($"Error reading {filenameForSurveysCsv}", e);
                return false;
            };
            var records = csv.GetRecords<T>();
            return records.ToArray();
        }
    }
}