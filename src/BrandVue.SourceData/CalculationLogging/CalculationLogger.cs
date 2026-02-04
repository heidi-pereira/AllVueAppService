using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using BrandVue.SourceData.Snowflake;

namespace BrandVue.SourceData.CalculationLogging
{
    public class CalculationLogger : ICalculationLogger
    {
        private readonly ISnowflakeRepository _repository;
        private readonly string _databaseName;
        private readonly string _schemaName;

        public CalculationLogger(ISnowflakeRepository repository, IOptions<SnowflakeDapperDbSettings> snowflakeDapperDbSettings)
        {
            _repository = repository;
            _databaseName = snowflakeDapperDbSettings.Value.DatabaseName;
            _schemaName = snowflakeDapperDbSettings.Value.SchemaName;
        }

        public async Task LogAsync(string filteredMetricJson,
                                    string calculationPeriodJson,
                                    string averageJson,
                                    string requestedInstancesJson,
                                    string quotaCellsJson,
                                    bool calculateSignificance,
                                    string resultsJson,
                                    string calculationSource)
        {
            // Despite the database and schema being specified in the connection string, they must
            // also be added here, prefixed to the table name, otherwise you get an
            // authorisation / missing table exception returned.
            var sql = $@"insert into {_databaseName}.{_schemaName}.calculation_log (id,
                                                                        filtered_metric_json,
                                                                        calculation_period_json,
                                                                        average_json,
                                                                        requested_instances_json,
                                                                        quota_cells_json,
                                                                        calculate_significance,
                                                                        results_json,
                                                                        created_at,
                                                                        calculation_source)
            select
                :Id,
                parse_json(:FilteredMetricJson),
                parse_json(:CalculationPeriodJson),
                parse_json(:AverageJson),
                parse_json(:RequestedInstancesJson),
                parse_json(:QuotaCellsJson),
                :CalculateSignificance,
                parse_json(:ResultsJson),
                current_timestamp,
                :CalculationSource";

            var entry = new
            {
                Id = Guid.NewGuid(),
                FilteredMetricJson = filteredMetricJson,
                CalculationPeriodJson = calculationPeriodJson,
                AverageJson = averageJson,
                RequestedInstancesJson = requestedInstancesJson,
                QuotaCellsJson = quotaCellsJson,
                CalculateSignificance = calculateSignificance,
                ResultsJson = resultsJson,
                CalculationSource = calculationSource,
            };

            await _repository.ExecuteAsync(sql, entry);
        }
    }
}
