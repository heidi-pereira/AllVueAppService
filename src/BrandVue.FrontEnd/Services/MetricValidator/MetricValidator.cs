using BrandVue.EntityFramework.Exceptions;
using static BrandVue.Services.MetricValidator.MetricValidationErrors;

namespace BrandVue.EntityFramework.MetaData.Metrics
{
    public class MetricValidator : IMetricValidator
    {
        private readonly IMetricConfigurationRepository _metricConfigurationRepository;
        private readonly Dictionary<Func<string, bool>, string> _metricDisplayNameRules;

        public MetricValidator(IMetricConfigurationRepository metricConfigurationRepository)
        {
            _metricConfigurationRepository = metricConfigurationRepository;
            _metricDisplayNameRules = new Dictionary<Func<string, bool>, string>
            {
                {
                    (newDisplayName) => {
                        return string.IsNullOrWhiteSpace(newDisplayName) || newDisplayName?.Length < 3;
                    },
                    MetricNameMustBeAtLeast3Characters
                },
                {
                    (newDisplayName) =>_metricConfigurationRepository
                    .GetAll()
                    .Where(x => string.Equals(x.DisplayName, newDisplayName, StringComparison.InvariantCultureIgnoreCase))
                    .Count() > 0,
                    MetricNameAlreadyExists
                }
            };
        }

        public void ValidateMetricDisplayName(string newDisplayName)
        {
            List<string> errors = [];

            foreach (var rule in _metricDisplayNameRules)
            {
                if (rule.Key(newDisplayName))
                {
                    errors.Add(rule.Value);
                }
            }

            if (errors.Count > 0)
            {
                throw new BadRequestException(string.Join(", ", errors));
            }
        }
    }
}