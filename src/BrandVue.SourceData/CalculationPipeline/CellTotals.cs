using System.ComponentModel;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Total broken down by cell.
    /// The date represents the last day in the period being totalled.
    /// </summary>
    public class CellTotals
    {
        private readonly Dictionary<int, Total> _resultsByQuotaCell;

        public CellTotals(
            DateTimeOffset date,
            int? waveInstanceId)
        {
            Date = date;
            _resultsByQuotaCell = new Dictionary<int, Total>();
            WaveInstanceId = waveInstanceId;
        }

        internal int? WaveInstanceId { get; }

        public DateTimeOffset Date { get; }

        /// <returns>If there is sample for this cell, the result
        /// Otherwise, null
        /// </returns>
        public Total this[QuotaCell quotaCell]
        {
            get
            {
                if (quotaCell == null)
                {
                    throw new ArgumentNullException(
                        nameof(quotaCell),
                        $"Cannot retrieve daily measure values for {Date} for null quota cell.");
                }

                if (_resultsByQuotaCell.TryGetValue(quotaCell.Index, out var r))
                {
                    return r;
                }

                return null;
            }
            set
            {
                if (quotaCell == null)
                {
                    throw new ArgumentNullException(
                        nameof(quotaCell),
                        $"Cannot set daily measure values for {Date} for null quota cell.");
                }

                _resultsByQuotaCell[quotaCell.Index] = value ?? throw new ArgumentNullException(nameof(value), $"Cannot set null result for {Date} for quota cell {quotaCell}.");
            }
        }

        public double SingleAverageResult() => _resultsByQuotaCell.Values.SingleOrDefault()?.TotalForAverage.Result ?? 0;

        public ResultSampleSizePair SingleTotalForAverage() => _resultsByQuotaCell.Values.SingleOrDefault()?.TotalForAverage;

        /// <summary>
        /// Not for general use. Use calculated results instead.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Dictionary<int, Total>.ValueCollection CellResultsWithSample => _resultsByQuotaCell.Values;
    }
}
