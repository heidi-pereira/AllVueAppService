using System.Globalization;
using System.Linq;
using CsvHelper.Configuration;

namespace Test.BrandVue.FrontEnd.DataWarehouseTests
{
    public sealed class MetricResultMap : ClassMap<MetricResult>
    {
        public MetricResultMap()
        {
            AutoMap(CultureInfo.InvariantCulture);

            Map(m => m.Ids).ConvertUsing(row =>
                {
                    return row.Context.HeaderRecord
                        .Where(h => h.EndsWith("Id"))?
                        .ToDictionary(x=>x,y=> int.Parse(row.GetField(y)));

                }
            );
        }
    }
}