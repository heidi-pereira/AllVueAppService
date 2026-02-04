using System.Collections;

namespace BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// An ordered time series. Each contains a breakdown of total per cell.
    /// </summary>
    public class CellsTotalsSeries: IEnumerable<CellTotals>
    {
        private readonly CellTotals [] _cellTotalsSeries;

        public CellsTotalsSeries(CellTotals[] items)
        {
            _cellTotalsSeries = items;
        }

        public CellTotals this[int index]
        {
            get { return _cellTotalsSeries[index]; }
            set { _cellTotalsSeries[index] = value; }
        }

        public int Count
        {
            get { return _cellTotalsSeries.Length; }
        }

        public IEnumerator<CellTotals> GetEnumerator()
        {
            return ((IEnumerable<CellTotals>)_cellTotalsSeries).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
