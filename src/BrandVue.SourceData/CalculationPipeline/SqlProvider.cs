using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace BrandVue.SourceData.CalculationPipeline
{
    public class SqlProvider : ISqlProvider
    {
        private readonly string _connectionString;
        private readonly string _productName;

        public SqlProvider(AppSettings appSettings, IProductContext productContext) : this(appSettings.ConnectionString, productContext.ShortCode)
        {
        }

        public SqlProvider(string connectionString, string productName)
        {
            _connectionString = connectionString;
            _productName = productName;
        }

        public async Task ExecuteReaderAsync(string sql, Dictionary<string, object> parameters,
            Action<IDataRecord> handleRow, CancellationToken cancellationToken)
        {
            await using (var con = new SqlConnection(_connectionString))
            {
                await con.OpenAsync(cancellationToken);

                await using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sql.Replace("{ProductName}", _productName);
                    // temporary fix to allow the query for all columns in the data table (that the main answersets public api endpoint
                    // initiates) to execute when column level statistics are missing for them in the database
                    // this problem will go away when the new response table has been implemented
                    cmd.CommandTimeout = 300;

                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            if (parameter.Value is SqlParameter p) cmd.Parameters.Add(p);
                            else cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }
                    }

                    await using (var rdr = await cmd.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await rdr.ReadAsync(cancellationToken))
                        {
                            handleRow(rdr);
                        }
                    }
                }
            }
        }
        public void ExecuteReader(string sql, Dictionary<string, object> parameters, Action<IDataRecord> handleRow)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                con.Open();

                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = sql.Replace("{ProductName}", _productName);
                    // temporary fix to allow the query for all columns in the data table (that the main answersets public api endpoint
                    // initiates) to execute when column level statistics are missing for them in the database
                    // this problem will go away when the new response table has been implemented
                    cmd.CommandTimeout = 300;

                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            if (parameter.Value is SqlParameter p) cmd.Parameters.Add(p);
                            else cmd.Parameters.AddWithValue(parameter.Key, parameter.Value);
                        }
                    }

                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            handleRow(rdr);
                        }
                    }
                }
            }
        }
    }
}