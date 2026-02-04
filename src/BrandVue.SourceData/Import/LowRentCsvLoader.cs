using System.IO;
using LumenWorks.Framework.IO.Csv;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Import
{
    public class LowRentCsvLoader
    {
        private readonly ILogger _logger;

        public LowRentCsvLoader(ILogger<LowRentCsvLoader> logger)
        {
            _logger = logger;
        }

        public IEnumerable<IDictionary<string, string>> Load(
            string fullyQualifiedPathNameToCsv,
            bool useCaseSensitiveFieldNames = true)
        {
            if (String.IsNullOrEmpty(fullyQualifiedPathNameToCsv))
            {
                throw new ArgumentNullException(
                    nameof(fullyQualifiedPathNameToCsv),
                    "Cannot load CSV data for null or empty path.");    
            }

            _logger.LogDebug("Loading CSV data from {Path}", fullyQualifiedPathNameToCsv);

            using (var csv = CreateCsvReader(fullyQualifiedPathNameToCsv))
            {
                var fieldCount = csv.FieldCount;
                
                var headers = csv.GetFieldHeaders();
                for (int index = 0; index < fieldCount; ++index)
                {
                    headers[index] = string.Intern(headers[index]);
                }

                while (csv.ReadNextRecord())
                {
                    var values = useCaseSensitiveFieldNames
                        ? new Dictionary<string, string>(fieldCount)
                        : new Dictionary<string, string>(
                            fieldCount,
                            StringComparer.OrdinalIgnoreCase);

                    for (var i = 0; i < fieldCount; i++)
                    {
                        values[headers[i]] = csv[i];
                    }
                    yield return values;
                }
            }
        }

        public string[] GetColumnNames(string pathToCsv)
        {
            using (var csv = CreateCsvReader(pathToCsv))
            {
                return csv.GetFieldHeaders();
            }
        }

        private static CsvReader CreateCsvReader(string pathToCsv)
        {
            var fileStream = new FileStream(pathToCsv, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return new CsvReader(new StreamReader(fileStream), true){ SkipEmptyLines = true };
        }
    }
}
