using System.Threading.Tasks;

namespace BrandVue.SourceData.CalculationLogging
{
    public interface ICalculationLogger
    {
        Task LogAsync(string filteredMetricJson,
                      string calculationPeriodJson,
                      string averageJson,
                      string requestedInstancesJson,
                      string quotaCellsJson,
                      bool calculateSignificance,
                      string resultsJson,
                      string calculationSource);
    }
}
