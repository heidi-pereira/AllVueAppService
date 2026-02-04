using System;
using System.Linq;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData
{
    internal class TestMetric
    {
        public string Name => _name + $"({BaseField?.Name}, {PrimaryField?.Name}, {SecondaryField?.Name})";

        public ResponseFieldDescriptor PrimaryField { get; private set; }
        public ResponseFieldDescriptor SecondaryField { get; private set; }
        public IVariable<int?> FieldExpression { get; private set; }
        public FieldOperation FieldOperation { get; private set; }
        public ResponseFieldDescriptor BaseField { get; private set; }
        public IVariable<bool> BaseExpression { get; private set; }

        public EntityValue InstanceToCheck { get; private set; }
        public EntityValue FilterInstance { get; private set; }
        public IFieldExpressionParser FieldExpressionParser { get; private set; }
        public AverageDescriptor AverageDescriptor { get; }

        private readonly CalculationType _calculationType;
        private int[] _primaryTrueValues;
        private int[] _secondaryTrueValues;
        private int[] _baseValues;
        private EntityValue[] _splitByInstances;
        private readonly string _name;


        public TestMetric(string name, CalculationType calculationType, IFieldExpressionParser fieldExpressionParser, AverageDescriptor averageDescriptor = null) : this(name, null, null, calculationType, null, null, null, null, Array.Empty<EntityValue>(), null, null, null, null, FieldOperation.None, fieldExpressionParser, averageDescriptor)
        {
        }

        private TestMetric(string name, ResponseFieldDescriptor primaryField, IVariable<int?> fieldExpression, CalculationType calculationType,
            int[] primaryTrueValues, int[] baseValues, ResponseFieldDescriptor baseField, IVariable<bool> baseExpression,
            EntityValue[] splitByInstances, EntityValue instanceToCheck, EntityValue filterInstance, ResponseFieldDescriptor secondaryField,
            int[] secondaryTrueValues, FieldOperation fieldOperation, IFieldExpressionParser fieldExpressionParser, AverageDescriptor averageDescriptor)
        {
            _name = name;
            PrimaryField = primaryField;
            FieldExpression = fieldExpression;
            InstanceToCheck = instanceToCheck;
            SecondaryField = secondaryField;
            FieldOperation = fieldOperation;
            _calculationType = calculationType;
            _primaryTrueValues = primaryTrueValues;
            _baseValues = baseValues;
            _secondaryTrueValues = secondaryTrueValues;
            BaseField = baseField;
            BaseExpression = baseExpression;
            _splitByInstances = splitByInstances.Any() || instanceToCheck == null ? splitByInstances : new[] {instanceToCheck};
            InstanceToCheck = instanceToCheck;
            FilterInstance = filterInstance;
            FieldExpressionParser = fieldExpressionParser;
            AverageDescriptor = averageDescriptor ?? Averages.SingleDayAverage;
        }

        public TestMetric WithPrimaryField(ResponseFieldDescriptor primaryField)
        {
            var clone = Clone();
            clone.PrimaryField = primaryField;
            return clone;
        }

        public TestMetric WithPrimaryExpression(IVariable<int?> fieldExpression)
        {
            var clone = Clone();
            clone.FieldExpression = fieldExpression;
            return clone;
        }

        public TestMetric WithBaseField(ResponseFieldDescriptor baseField)
        {
            var clone = Clone();
            clone.BaseField = baseField;
            return clone;
        }

        public TestMetric WithBaseExpression(IVariable<bool> baseExpression)
        {
            var clone = Clone();
            clone.BaseExpression = baseExpression;
            return clone;
        }

        public TestMetric WithSplitByInstances(params EntityValue[] splitByInstances)
        {
            var clone = Clone();
            clone._splitByInstances = splitByInstances;
            return clone;
        }

        public TestMetric WithFilterInstance(EntityValue filterInstance)
        {
            var clone = Clone();
            clone.FilterInstance = filterInstance;
            return clone;
        }

        public TestMetric CheckResultFor(EntityValue instanceToCheck)
        {
            var splitByInstances = !_splitByInstances.Any() && instanceToCheck != null ? new[] {instanceToCheck} : _splitByInstances;
            var clone = Clone();
            clone._splitByInstances = splitByInstances;
            clone.InstanceToCheck = instanceToCheck;
            return clone;
        }

        public TestMetric WithTrueAndBaseValues(int[] trueValues, int[] baseValues)
        {
            var clone = Clone();
            clone._primaryTrueValues = trueValues;
            clone._baseValues = baseValues;
            return clone;
        }

        public TestMetric WithSecondaryField(ResponseFieldDescriptor secondaryField, FieldOperation fieldOperation)
        {
            var clone = Clone();
            clone.SecondaryField = secondaryField;
            clone.FieldOperation = fieldOperation;
            clone._secondaryTrueValues = _secondaryTrueValues ?? _primaryTrueValues;
            return clone;
        }

        public TestMetric WithSecondaryTrueValues(int[] secondaryTrueValues)
        {
            var clone = Clone();
            clone._secondaryTrueValues = secondaryTrueValues;
            return clone;
        }

        private TestMetric Clone()
        {
            return new TestMetric(Name,
                PrimaryField,
                FieldExpression,
                _calculationType,
                _primaryTrueValues,
                _baseValues,
                BaseField,
                BaseExpression,
                _splitByInstances,
                InstanceToCheck,
                FilterInstance,
                SecondaryField,
                _secondaryTrueValues,
                FieldOperation,
                FieldExpressionParser,
                AverageDescriptor);
        }

        /// <summary>
        /// Add a single response containing multiple answers
        /// </summary>
        public CalculationTestCaseBuilder WithResponse(params TestAnswer[] answers)
        {
            return WithResponses(new ResponseAnswers(answers));
        }

        /// <summary>
        /// Add a single response containing multiple answers
        /// </summary>
        public CalculationTestCaseBuilder WithResponse(ResponseAnswers responseAnswers)
        {
            return WithResponses(responseAnswers);
        }

        /// <summary>
        /// Add multiple responses with multiple answers
        /// </summary>
        public CalculationTestCaseBuilder WithResponses(params ResponseAnswers[] respondentsAnswers)
        {
            bool invalidTrueValues = FieldExpression is null && _primaryTrueValues is null;
            bool invalidBaseValues = BaseExpression is null && _baseValues is null;
            if (invalidTrueValues || invalidBaseValues)
            {
                throw new InvalidOperationException("No true values or base values specified");
            }

            var measure = new Measure
            {
                Name = "TestMeasure",
                CalculationType = _calculationType,
                Field = PrimaryField,
                PrimaryVariable = FieldExpression,
                LegacyPrimaryTrueValues = { Values = _primaryTrueValues },
                Field2 = SecondaryField,
                LegacySecondaryTrueValues = new AllowedValues{Values = _secondaryTrueValues},
                FieldOperation = FieldOperation,
                BaseField = BaseField,
                BaseExpression = BaseExpression,
                LegacyBaseValues = { Values = _baseValues },
            };

            return new CalculationTestCaseBuilder(measure, AverageDescriptor, respondentsAnswers, _splitByInstances, InstanceToCheck, FilterInstance);
        }

        internal class CalculationTestCaseBuilder
        {
            private readonly Measure _measure;
            private readonly AverageDescriptor _averageDescriptor;
            private readonly ResponseAnswers[] _respondentsAnswers;
            private int _expectedBaseSize;
            private double _expectedResult;
            private readonly Func<EntityWeightedDailyResults[], WeightedDailyResult> _resultSelector = results => results.SingleOrDefault()?.WeightedDailyResults.SingleOrDefault();
            private readonly EntityValue[] _splitByInstances;
            private readonly EntityValue _filterInstance;
            private string _testName;

            public CalculationTestCaseBuilder(Measure measure, AverageDescriptor averageDescriptor,
                ResponseAnswers[] respondentsAnswers, EntityValue[] splitByInstances, EntityValue instanceToCheck,
                EntityValue filterInstance)
            {
                _measure = measure;
                _averageDescriptor = averageDescriptor;
                _respondentsAnswers = respondentsAnswers;
                _splitByInstances = splitByInstances;
                _filterInstance = filterInstance;
                if (instanceToCheck != null)
                {
                    _resultSelector = results => results.SingleOrDefault(r => r.EntityInstance.Id == instanceToCheck.Value)?.WeightedDailyResults.SingleOrDefault();
                }
            }

            private CalculationTestCaseBuilder(Measure measure, AverageDescriptor averageDescriptor, ResponseAnswers[] respondentsAnswers, EntityValue[] splitByInstances, Func<EntityWeightedDailyResults[], WeightedDailyResult> resultSelector, EntityValue filterInstance, int expectedBaseSize, double expectedResult, string testName)
            {
                _measure = measure;
                _averageDescriptor = averageDescriptor;
                _respondentsAnswers = respondentsAnswers;
                _splitByInstances = splitByInstances;
                _filterInstance = filterInstance;
                _expectedBaseSize = expectedBaseSize;
                _expectedResult = expectedResult;
                _testName = testName;
                _resultSelector = resultSelector;
            }

            public CalculationTestCaseBuilder ExpectBaseSize(int expectedBaseSize)
            {
                var clone = Clone();
                clone._expectedBaseSize = expectedBaseSize;
                return clone;
            }

            public CalculationTestCaseBuilder ExpectResult(double result)
            {

                var clone = Clone();
                clone._expectedResult = result;
                return clone;
            }

            public CalculationTestCaseBuilder Named(string testName)
            {
                var clone = Clone();
                clone._testName = testName;
                return clone;
            }

            public TestCaseData Build()
            {
                return new TestCaseData(_measure, _averageDescriptor, _respondentsAnswers, _resultSelector, _expectedResult, _expectedBaseSize, _splitByInstances, _filterInstance).SetName(_testName);
            }

            private CalculationTestCaseBuilder Clone()
            {
                return new CalculationTestCaseBuilder(_measure, _averageDescriptor, _respondentsAnswers, _splitByInstances, _resultSelector, _filterInstance, _expectedBaseSize, _expectedResult, _testName);
            }
        }
    }
}