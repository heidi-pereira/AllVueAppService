using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace BrandVue.SourceData.CalculationPipeline
{
    public interface ISqlProvider
    {
        void ExecuteReader(string sql, Dictionary<string, object> parameters, Action<IDataRecord> handleRow);
        Task ExecuteReaderAsync(string sql, Dictionary<string, object> parameters, Action<IDataRecord> handleRow, CancellationToken cancellationToken);
    }
}