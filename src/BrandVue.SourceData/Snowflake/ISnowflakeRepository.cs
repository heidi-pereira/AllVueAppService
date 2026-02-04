using System.Collections.Generic;
using System.Threading.Tasks;

namespace BrandVue.SourceData.Snowflake
{
    public interface ISnowflakeRepository
    {
        Task<int> ExecuteAsync(string sql, object param = null);
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null);
    }
}
