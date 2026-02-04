using Microsoft.Data.SqlClient.Server;
using System.Data;
using System.Linq;

namespace BrandVue.EntityFramework.ResponseRepository
{
    public class ResponseHelper
    {
        internal static IEnumerable<SqlDataRecord> GetSqlRecordSetFromResponseWeightings(ResponseWeight[] responseWeights)
        {
            return responseWeights.Select(r =>
            {
                var responseWeighting = new SqlDataRecord(new SqlMetaData("ResponseId", SqlDbType.BigInt),
                    new SqlMetaData("Weighting", SqlDbType.Float));
                responseWeighting.SetInt64(0, r.ResponseId);
                responseWeighting.SetDouble(1, r.Weight);
                return responseWeighting;
            });
        }
        internal static IEnumerable<SqlDataRecord> GetSqlRecordSetFromResponseIds(IList<int> responseWeights)
        {
            return responseWeights.Select(r =>
            {
                var responseWeighting = new SqlDataRecord(new SqlMetaData("id", SqlDbType.Int));
                responseWeighting.SetInt32(0, r);
                return responseWeighting;
            });
        }
    }
}
