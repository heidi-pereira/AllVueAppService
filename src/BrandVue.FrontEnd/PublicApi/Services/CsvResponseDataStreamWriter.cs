using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using CsvHelper;
using MediaTypeHeaderValue = System.Net.Http.Headers.MediaTypeHeaderValue;

namespace BrandVue.PublicApi.Services
{
    public class CsvResponseDataStreamWriter : IResponseDataStreamWriter
    {

        public StreamedCsvResult StreamSurveyAnswersetsToHttpResponseMessage<TTuple>(IEnumerable<string> headers,
            IEnumerable<TTuple> rowsWithSameColumnOrder, Func<IEnumerable<(string, string)>> extraLines) where TTuple : ITuple
        {
            return new StreamedCsvResult(MediaTypeHeaderValue.Parse("text/csv"), stream =>
            {
                var readOnlyCollection = headers.ToList();
                return StreamResponseDataToOutputStream(stream, readOnlyCollection,
                        (csvWriter) => WriteTCSVRows(csvWriter, rowsWithSameColumnOrder, extraLines));
            });
        }

        public StreamedCsvResult StreamSurveyAnswersetsToHttpResponseMessage(IEnumerable<string> headers,
            IEnumerable<IReadOnlyDictionary<string, string>> surveyResponseData)
        {
            var headersCollection = headers.ToList();
            return new StreamedCsvResult(MediaTypeHeaderValue.Parse("text/csv"), stream =>
            {
                return StreamResponseDataToOutputStream(stream, headersCollection,
                        (csvWriter) => WriteDictionaryRows(csvWriter, headersCollection, surveyResponseData));
            });
        }

        private static async Task<IReadOnlyCollection<IAsyncDisposable>> StreamResponseDataToOutputStream(Stream stream, IReadOnlyCollection<string> headers, Func<CsvWriter, Task> writeRows)
        {
            var streamWriter = new StreamWriter(stream);
            var csvWriter = new CsvWriter(streamWriter, CultureInfo.CurrentCulture);
            await WriteHeaders(csvWriter, headers);
            await writeRows(csvWriter);
            await streamWriter.FlushAsync();
            return new IAsyncDisposable[] {streamWriter, csvWriter};
        }

        private static async Task WriteHeaders(CsvWriter csvWriter, IReadOnlyCollection<string> headers)
        {
            foreach (var header in headers)
            {
                csvWriter.WriteField(header);
            }
            await csvWriter.NextRecordAsync();
        }

        private static async Task WriteTCSVRows<TTuple>(CsvWriter csvWriter, IEnumerable<TTuple> rows, Func<IEnumerable<(string, string)>> extraLines) where TTuple : ITuple
        {
            foreach (var row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    csvWriter.WriteField(row[i]);
                }
                await csvWriter.NextRecordAsync();
            }
            if (extraLines != null)
            {
                foreach (var row in extraLines())
                {
                    csvWriter.WriteField(row.Item1);
                    csvWriter.WriteField("");
                    csvWriter.WriteField("");
                    csvWriter.WriteField(row.Item2);
                    
                    await csvWriter.NextRecordAsync();
                }
            }
        }

        private static async Task WriteDictionaryRows(CsvWriter csvWriter, IReadOnlyCollection<string> headers,
            IEnumerable<IReadOnlyDictionary<string, string>> responseData)
        {
            foreach (var response in responseData)
            {
                foreach (var header in headers)
                {
                    string fieldVal = response.TryGetValue(header, out string v) ? v : "";
                    csvWriter.WriteField(fieldVal);
                }
                await csvWriter.NextRecordAsync();
            }
        }
    }
}
