using System.Data;
using Snowflake.Data.Client;

namespace BrandVue.SourceData.Snowflake
{
    public class SnowflakeDbConnectionFactory : ISnowflakeDbConnectionFactory
    {
        private readonly string _connectionString;

        public SnowflakeDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new SnowflakeDbConnection { ConnectionString = _connectionString };
        }
    }
}
