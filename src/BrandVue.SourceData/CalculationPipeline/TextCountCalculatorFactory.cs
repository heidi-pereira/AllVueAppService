using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.FeatureToggle;
using Vue.Common.App_Start;
using Vue.Common.FeatureFlags;

namespace BrandVue.SourceData.CalculationPipeline;

public class TextCountCalculatorFactory : ITextCountCalculatorFactory
{
    private readonly IFeatureToggleService _featureToggleService;
    private readonly IRequestAwareFactory<SnowflakeTextCountCalculator> _snowflakeFactory;
    private readonly IRequestAwareFactory<SqlServerTextCountCalculator> _sqlServerFactory;

    public TextCountCalculatorFactory(
        IFeatureToggleService featureToggleService,
        IRequestAwareFactory<SnowflakeTextCountCalculator> snowflakeFactory,
        IRequestAwareFactory<SqlServerTextCountCalculator> sqlServerFactory)
    {
        _featureToggleService = featureToggleService;
        _snowflakeFactory = snowflakeFactory;
        _sqlServerFactory = sqlServerFactory;
    }

    public async Task<ITextCountCalculator> CreateAsync()
    {
        bool useSnowflake = await _featureToggleService.IsFeatureEnabledAsync(FeatureCode.use_snowflake_for_text_count_calculation);
        return useSnowflake ? _snowflakeFactory.Create() : _sqlServerFactory.Create();
    }
}