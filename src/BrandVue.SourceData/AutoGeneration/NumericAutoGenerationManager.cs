using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.AutoGeneration.Buckets;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Variable;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static BrandVue.SourceData.AutoGeneration.AutoGenerationConstants;

namespace BrandVue.SourceData.AutoGeneration
{
    public class NumericAutoGenerationManager
    {
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly BucketedVariableConfigurationCreator _bucketedVariableConfigurationCreator;
        private readonly BucketedMetricConfigurationCreator _bucketedMetricConfigurationCreator;
        private readonly ILogger<NumericAutoGenerationManager> _logger;
        private readonly IntegerInclusiveBucketCreator<NumericBucket> _bucketCreator = new();
        private readonly NumericFuzzyMatchers _numericFuzzyMatchers = new NumericFuzzyMatchers();

        public NumericAutoGenerationManager(IVariableConfigurationRepository variableConfigurationRepository,
            BucketedVariableConfigurationCreator bucketedVariableConfigurationCreator,
            BucketedMetricConfigurationCreator bucketedMetricConfigurationCreator,
            ILoggerFactory loggerFactory)
        {
            _variableConfigurationRepository = variableConfigurationRepository;
            _bucketedVariableConfigurationCreator = bucketedVariableConfigurationCreator;
            _bucketedMetricConfigurationCreator = bucketedMetricConfigurationCreator;
            _logger = loggerFactory.CreateLogger<NumericAutoGenerationManager>();
        }


        public IList<MetricConfiguration> CreateAllAutoBucketedNumericMetrics(IEnumerable<NumericFieldData> numericFields)
        {
            return numericFields
                .Select(CreateAutoBucketedNumericMetric)
                .Where(bucketedMetric => bucketedMetric != null).ToList();
        }

        [CanBeNull]
        public MetricConfiguration CreateAutoBucketedNumericMetric(NumericFieldData numericFieldData)
        {
            var field = numericFieldData.GetFieldDefinitionModel();
            var buckets = GetBuckets(field.QuestionModel).ToList();
            if (!buckets.Any())
            {
                _logger.LogDebug($"Numeric Auto creation stopped for field: {field.Name}, no buckets created (check min/max values in database)");
                return null;
            }
            
            var bucketedVariableConfiguration = _bucketedVariableConfigurationCreator.CreateBucketedVariable(numericFieldData, buckets);
            _logger.LogInformation($"New bucketed variable has been created for numeric field: {field.Name}");
            return CreateMetricOrUndoVariableCreation(numericFieldData, bucketedVariableConfiguration);
        }

        private MetricConfiguration CreateMetricOrUndoVariableCreation(NumericFieldData numericFieldData, VariableConfiguration bucketedVariableConfiguration)
        {
            try
            {
                var field = numericFieldData.GetFieldDefinitionModel();
                var bucketedMetric = _bucketedMetricConfigurationCreator.CreateBucketedMetricConfiguration(numericFieldData, bucketedVariableConfiguration);
                _logger.LogInformation($"New bucketed metric {bucketedMetric.Name} has been automatically created from numeric field: {field.Name}");
                return bucketedMetric;
            }
            catch (Exception)
            {
                _variableConfigurationRepository.Delete(bucketedVariableConfiguration);
                throw;
            }
        }

        private bool IsAgeQuestion(Question question)
        {
            try
            {
                return _numericFuzzyMatchers.AgeFuzzyMatcher(question);
            }
            catch (Exception e)
            {
                _logger.LogInformation("Age fuzzy matcher error: " + e.Message);
                return false;
            }
        }
        
        private bool IsNoOfChildrenQuestion(Question question)
        {
            try
            {
                return _numericFuzzyMatchers.NoOfChildrenFuzzyMatcher(question);
            }
            catch (Exception e)
            {
                _logger.LogInformation("|No of children fuzzy matcher error: " + e.Message);
                return false;
            }
        }

        private IEnumerable<NumericBucket> GetBuckets(Question question)
        {
            if (IsAgeQuestion(question))
            {
                return _bucketCreator.CreateBucketsForAge();
            }
            if (IsNoOfChildrenQuestion(question))
            {
                return _bucketCreator.CreateBucketsForNoOfChildren();
            }
            if (question != null && !question.MinimumValue.HasValue && !question.MaximumValue.HasValue)
            {
                return Enumerable.Empty<NumericBucket>();
            }
            int min = question?.MinimumValue ?? 0;
            int max = question?.MaximumValue ?? 0;

            if (min == 0 && max == 0)
            {
                return Enumerable.Empty<NumericBucket>();
            }
            int numberOfBuckets = Math.Abs(max - min) >= NumberBeforeBucketing
                ? DefaultNumberOfBuckets
                : Math.Abs(max - min) + 1;
            return _bucketCreator.CreateBuckets(min, max, numberOfBuckets);
        }
    }
}