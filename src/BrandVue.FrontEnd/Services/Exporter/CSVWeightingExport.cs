using System.Runtime.CompilerServices;
using BrandVue.PublicApi.Services;

namespace BrandVue.Services.Exporter
{
    public class CSVWeightingExport
    {
        private int _rowCount = 0;
        private double _weightSum = 0.0;
        private readonly IResponseDataStreamWriter _csvStreamWriter;
        public CSVWeightingExport(IResponseDataStreamWriter csvStreamWriter)
        {
            _csvStreamWriter = csvStreamWriter;
        }
        IEnumerable<TTuple> Intercept<TTuple>(IEnumerable<TTuple> items) where TTuple : ITuple
        {
            foreach (var item in items)
            {
                _rowCount++;
                yield return item;
                var weight = (double?)item[3];
                if (weight.HasValue)
                {
                    _weightSum += weight.Value;
                }
            }
        }
        IEnumerable<(string, string)> ExtraLinesAtTheEnd()
        {
            const double maxTolerance = 0.05;
            var extra = new List<(string, string)>();
            extra.Add(("Total rows", _rowCount.ToString()));
            extra.Add(("Sum of Weights", _weightSum.ToString()));

            var difference = Math.Abs(_weightSum - _rowCount);
            if (difference > maxTolerance * _rowCount)
            {
                extra.Add(($"Caution large difference > {(maxTolerance*100)}%", difference.ToString(".0")));
                extra.Add(("Difference", (difference/_rowCount).ToString("0.0%")));
            }
            return extra;
        }

        public StreamedCsvResult StreamSurveyAnswersetsToHttpResponseMessage<TTuple>(IEnumerable<string> headers,
        IEnumerable<TTuple> rowsWithSameColumnOrder) where TTuple : ITuple
        {
            return _csvStreamWriter.StreamSurveyAnswersetsToHttpResponseMessage(headers, Intercept(rowsWithSameColumnOrder), ExtraLinesAtTheEnd);
        }
        public StreamedCsvResult StreamDataToHttpResponseMessage<TTuple>(IEnumerable<string> headers,
        IEnumerable<TTuple> rowsWithSameColumnOrder) where TTuple : ITuple
        {
            return _csvStreamWriter.StreamSurveyAnswersetsToHttpResponseMessage(headers, rowsWithSameColumnOrder, () => { return new List<(string, string)>(); });
        }

    }
}