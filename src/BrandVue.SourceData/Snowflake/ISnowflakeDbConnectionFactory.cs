using System.Data;

namespace BrandVue.SourceData.Snowflake
{
    public interface ISnowflakeDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
