using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace BrandVue.SourceData.Snowflake
{
    public class SnowflakeRepository : ISnowflakeRepository
    {
        private readonly ISnowflakeDbConnectionFactory _factory;

        public SnowflakeRepository(ISnowflakeDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            using (var connection = _factory.CreateConnection())
            {
                connection.Open();

                // Set a unique query tag for tracking
                await connection.ExecuteAsync("ALTER SESSION SET QUERY_TAG = 'calculation_log_insert';");

                // Now, execute the main SQL command
                return await connection.ExecuteAsync(sql, param);
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            using (var connection = _factory.CreateConnection())
            {
                connection.Open();
                return await connection.QueryAsync<T>(sql, param);
            }
        }
    }
}
