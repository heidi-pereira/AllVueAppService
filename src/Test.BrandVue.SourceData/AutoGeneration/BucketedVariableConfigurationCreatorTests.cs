using System.Collections;
using System.Collections.Generic;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.AutoGeneration;
using BrandVue.SourceData.AutoGeneration.Buckets;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.AutoGeneration
{

    internal class BucketedVariableConfigurationCreatorTestParameters : IEnumerable
    {
        private readonly FieldDefinitionModel _field = new ("Test", "", "", "", "", null, "", 0.0, "", false, null, new List<EntityFieldDefinitionModel>(), null);

        private readonly NumericBucket _bucketOne = new()
        {
            MinimumInclusive = 1,
            MaximumInclusive = 10
        };
        private readonly NumericBucket _bucketTwo = new()
        {
            MinimumInclusive = 11,
            MaximumInclusive = 20
        };
        private readonly NumericBucket _bucketThree = new()
        {
            MinimumInclusive = 21,
            MaximumInclusive = 30
        };

        public IEnumerator GetEnumerator()
        {
            yield return GetTest(_field, new List<NumericBucket>(), "Test variable creation with no bucket");
            yield return GetTest(_field, new List<NumericBucket>{_bucketOne}, "Test variable creation with single bucket");
            yield return GetTest(_field, new List<NumericBucket>{_bucketOne, _bucketTwo, _bucketThree}, "Test variable creation with multi bucket");
        }

        private static TestCaseData GetTest(FieldDefinitionModel field, IList<NumericBucket> buckets, string description)
        {
            return new TestCaseData(field, buckets)
                .SetName($"With {field.Name} metric and {buckets.Count} buckets")
                .SetDescription(description);
        }
    }


    [TestFixture]
    public class BucketedVariableConfigurationCreatorTests
    {
        [Test]
        [TestCaseSource(typeof(BucketedVariableConfigurationCreatorTestParameters))]
        [Parallelizable(ParallelScope.All)]
        public void CreateBucketedVariable_WithMetricAndBuckets_CreatedExpectedVariableDefinition(FieldDefinitionModel field, IList<NumericBucket> buckets)
        {
            // setup
            var subset = new Subset{ Id = "12345"};
            var numericFieldData = new NumericFieldData(field, subset.Id);
            numericFieldData.SetOriginalMetricName( "Test");

            var variableConfigurationFactory = Substitute.For<IVariableConfigurationFactory>();
            variableConfigurationFactory.CreateIdentifierFromName(Arg.Any<string>())
                .Returns(x => $"{x}id");
            variableConfigurationFactory.CreateVariableConfigFromParameters(Arg.Is<string>(x => x.StartsWith(AutoGenerationConstants.NumericIdentifier + ": Test")),
                Arg.Any<string>(),
                Arg.Any<GroupedVariableDefinition>(),
                out Arg.Any<IReadOnlyCollection<string>>(),
                out Arg.Any<IReadOnlyCollection<string>>(),
                false)
                .Returns(x => new VariableConfiguration
                {
                    Definition = (GroupedVariableDefinition)x[2]
                });

            var variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            variableConfigurationRepository.Create(Arg.Any<VariableConfiguration>(),
                Arg.Any<IReadOnlyCollection<string>>())
                .Returns(x => (VariableConfiguration)x[0]);

            // action
            var variableConfigurationCreator = new BucketedVariableConfigurationCreator(variableConfigurationRepository, variableConfigurationFactory);
            var variableConfiguration = variableConfigurationCreator.CreateBucketedVariable(numericFieldData, buckets);

            // assert
            Assert.That(variableConfiguration.Definition, Is.TypeOf<GroupedVariableDefinition>());
            var definition = (GroupedVariableDefinition) variableConfiguration.Definition;
            Assert.That(definition.ToEntityTypeName, Is.EqualTo(AutoGenerationConstants.NumericIdentifier + field.Name));
            Assert.That(definition.Groups.Count, Is.EqualTo(buckets.Count));
            for (int i = 0; i < buckets.Count; i++)
            {
                Assert.That(definition.Groups[i].ToEntityInstanceId, Is.EqualTo(i + 1));
                Assert.That(definition.Groups[i].ToEntityInstanceName, Is.EqualTo(buckets[i].GetBucketDescriptor(null)));
                var component = (InclusiveRangeVariableComponent) definition.Groups[i].Component;
                Assert.That(component.Min, Is.EqualTo(buckets[i].MinimumInclusive));
                Assert.That(component.Max, Is.EqualTo(buckets[i].MaximumInclusive));
                Assert.That(component.FromVariableIdentifier, Is.EqualTo(field.Name));
                Assert.That(component.Operator, Is.EqualTo(buckets[i].GetBucketOperator()));
            }
        }
    }
}