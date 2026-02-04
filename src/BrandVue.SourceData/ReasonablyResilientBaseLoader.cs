using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData
{
    public abstract class ReasonablyResilientBaseLoader<TWidgeroo, TIdentity>
        : BaseLoader<TWidgeroo, TIdentity> where TWidgeroo : class
    {
        private const int BufferSize = 1024 * 1024;
        private const string COMMENT_OUT_LINE_TOKEN = "//";

        protected ReasonablyResilientBaseLoader(
            BaseRepository<TWidgeroo, TIdentity> baseRepository,
            Type loaderSubclass,
            ILogger logger) : base(baseRepository, loaderSubclass, logger)
        {
        }

        public void LoadIfExists(string fullyQualifiedPathToCsvDataFile)
        {
            if (File.Exists(fullyQualifiedPathToCsvDataFile))
            {
                Load(fullyQualifiedPathToCsvDataFile);
            }
            else
            {
                _logger.LogWarning(
                    $"File not found: {fullyQualifiedPathToCsvDataFile}. Skipping loading for {GetType().Name}");
            }
        }

        public override void Load(string fullyQualifiedPathToCsvDataFile)
        {
            base.Load(fullyQualifiedPathToCsvDataFile);

            //  FileShare.ReadWrite so you can have the CSV open in Excel whilst running the tests
            using (var fileStream = new FileStream(
                fullyQualifiedPathToCsvDataFile,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            using (var bs = new BufferedStream(fileStream, BufferSize))
            using (var reader = new StreamReader(bs, Encoding.UTF8, true, BufferSize))
            {
                var parser = new CsvParser(reader, CultureInfo.CurrentCulture);
                var headers = parser.Read();
                if (headers == null)
                {
                    return; //  Nowt to load
                }

                LoadCsvDataRows(
                    fullyQualifiedPathToCsvDataFile,
                    parser,
                    headers);
            }
        }

        private void LoadCsvDataRows(
            string fullyQualifiedPathToCsvDataFile,
            CsvParser parser,
            string [] headers)
        {
            var identityFieldIndex = GetIdentityFieldIndex(headers);
            var lineNumber = 2;
            while (true)
            {
                var data = parser.Read();
                if (data == null)
                {
                    break;  //  That's all folks
                }

                if (data.Length != headers.Length)
                {
                    _logger.LogError("Mismatch between header count of {CsvHeadersLength} and data item count of {CsvDataLength} on line {LineNumber} of {Path}. " +
                                     "This might be just a single bad row so I'm going to carry on reading, and I'll try to process this row, " +
                                     "but be warned that things might get weird, and you should check the dashboard is working OK. " +
                                     "Headers {CsvHeaders}; data {CsvData}.",
                        headers.Length, data.Length, lineNumber, fullyQualifiedPathToCsvDataFile, headers, data);
                }

                if (data.FirstOrDefault()?.StartsWith(COMMENT_OUT_LINE_TOKEN) ?? true) continue;

                CreateAndStoreObjectForCsvDataRow(
                    fullyQualifiedPathToCsvDataFile,
                    lineNumber,
                    headers,
                    data,
                    identityFieldIndex);

                ++lineNumber;
            }
        }
    }
}
