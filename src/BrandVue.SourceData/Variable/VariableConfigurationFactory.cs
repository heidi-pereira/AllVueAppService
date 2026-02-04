using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Utils;
using Humanizer;

namespace BrandVue.SourceData.Variable
{
    public interface IVariableConfigurationFactory
    {
        /// <summary>
        /// Creates configuration model from create model.
        /// The create model gets generated in the front end.
        /// </summary>
        VariableConfiguration CreateVariableConfigFromParameters(string name,
            string identifier,
            VariableDefinition definition,
            out IReadOnlyCollection<string> dependencyVariableInstanceIdentifiers,
            out IReadOnlyCollection<string> entityTypeNames,
            bool shouldVerify = true);
        string CreateIdentifierFromName(string name);
    }

    public class VariableConfigurationFactory : IVariableConfigurationFactory
    {
        private readonly IReadableVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IReadableMetricConfigurationRepository _metricConfigurationRepository;
        private readonly IProductContext _productContext;
        private readonly IResponseFieldManager _responseFieldManager;
        private readonly IVariableValidator _variableValidator;
        private readonly VariableFactory _variableFactory;

        public VariableConfigurationFactory(IFieldExpressionParser fieldExpressionParser,
            IReadableVariableConfigurationRepository variableConfigurationRepository,
            IResponseEntityTypeRepository responseEntityTypeRepository,
            IProductContext productContext,
            IReadableMetricConfigurationRepository metricConfigurationRepository,
            IResponseFieldManager responseFieldManager,
            IVariableValidator variableValidator)
        {
            _variableFactory = new VariableFactory(fieldExpressionParser, responseEntityTypeRepository);
            _variableConfigurationRepository = variableConfigurationRepository;
            _productContext = productContext;
            _metricConfigurationRepository = metricConfigurationRepository;
            _responseFieldManager = responseFieldManager;
            _variableValidator = variableValidator;
        }

        public string CreateIdentifierFromName(string name)
        {
            string sanitizedName = name.SanitizeUrlSegment();
            return CreateUniqueIdentifier(sanitizedName);
        }

        public VariableConfiguration CreateVariableConfigFromParameters(string name,
            string identifier,
            VariableDefinition definition,
            out IReadOnlyCollection<string> dependencyVariableInstanceIdentifiers,
            out IReadOnlyCollection<string> entityTypeNames,
            bool shouldVerify = true)
        {
            if (definition is GroupedVariableDefinition groupedVariableDefinition)
            {
                string uniqueTypeName = _variableFactory.CreateUniqueTypeName(groupedVariableDefinition.ToEntityTypeName, 0, identifier);
                groupedVariableDefinition.ToEntityTypeName = uniqueTypeName;
            }

            var variable = new VariableConfiguration
            {
                Identifier = identifier,
                DisplayName = name,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                Definition = definition,
            };

            _variableValidator.Validate(variable, out dependencyVariableInstanceIdentifiers, out entityTypeNames, shouldVerify);
            return variable;
        }

        private string CreateUniqueIdentifier(string modelName)
        {
            //ensure we can use the identifier for metric creation later
            var existingVariableIdentifiers = _variableConfigurationRepository.GetAll().Select(x => x.Identifier);
            var existingMetricNames = _metricConfigurationRepository.GetAll().Select(x => x.Name);
            var existingFieldNames = _responseFieldManager.GetAllFields().Select(f => f.Name);

            for (var attemptCount = 0; attemptCount <= 10000; attemptCount++)
            {
                var suffix = attemptCount == 0 ? "" : attemptCount.ToString();
                var variableIdentifier = NameGenerator.EnsureValidPythonIdentifier($"{modelName}{suffix}".Dehumanize());

                if (!existingVariableIdentifiers.Contains(variableIdentifier, StringComparer.OrdinalIgnoreCase)
                    && !existingMetricNames.Contains(variableIdentifier, StringComparer.OrdinalIgnoreCase)
                    && !existingFieldNames.Contains(variableIdentifier, StringComparer.OrdinalIgnoreCase))
                {
                    return variableIdentifier;
                }
            }

            throw new InvalidOperationException($"Unable to create unique identifier for {modelName}");
        }
    }
}
