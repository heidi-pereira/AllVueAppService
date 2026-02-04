using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;
using NUnit.Framework;
using System;
using System.Reflection.Metadata;

namespace BrandVueBuilder.Tests
{
    public class FieldBuilderTests
    {
        [Test]
        public void ShouldSetValueTypeWhenValueIsEntityInstance()
        {
            var entities = new[]
            {
                new Entity("Product", new EntityInstance[0]),
                new Entity("Influence", new EntityInstance[0])
            };
            var field = new Fields {Name = "Most_important_influence", varCode = "Most_important_influence_{Product}", CH1 = "{Influence:value}"};

            var fieldDefinition = TestFieldDefinition.Create(field, entities);

            Assert.That(fieldDefinition.ValueEntityIdentifier, Is.EqualTo("Influence"));
        }

        [Test]
        public void ShouldNotSetValueTypeWhenValueIsNotEntityInstance()
        {
            var entities = new[]
            {
                new Entity("Product", new EntityInstance[0]),
                new Entity("Influence", new EntityInstance[0])
            };
            var field = new Fields {Name = "Most_important_influence", varCode = "Most_important_influence_{Product}", CH1 = "{Influence}", optValue = "{value}"};

            var fieldDefinition = TestFieldDefinition.Create(field, entities);

            Assert.That(fieldDefinition.ValueEntityIdentifier, Is.Null);
        }

        [Test]
        public void ShouldSetTypeOverrideWhenValueHasScaleFactor()
        {
            var field = new Fields { Name = "Household_income", varCode = "Household_income", ScaleFactor = "0.01", RoundingType = "Floor", optValue = "{value}" };

            var fieldDefinition = TestFieldDefinition.Create(field);

            Assert.That(fieldDefinition.TypeOverride, Is.EqualTo("SMALLINT"));
        }

        [Test]
        public void ShouldNotSetTypeOverrideWhenValueHasNoScaleFactor()
        {
            var field = new Fields { Name = "Household_income", varCode = "Household_income", optValue = "{value}" };

            var fieldDefinition = TestFieldDefinition.Create(field);

            Assert.That(fieldDefinition.TypeOverride, Is.Null);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(0)]
        [TestCase(3)]
        [TestCase(9)]
        public void ShouldSetValueTypeWhenValueIsEntityInstanceEndingInNumber(int number)
        {
            var entityEndingWithNumber = $"SEG{number}";
            var testCase = SetupTestCaseWithEntityName(entityEndingWithNumber);

            var fieldDefinition = TestFieldDefinition.Create(testCase.field, testCase.entities);

            Assert.That(fieldDefinition.ValueEntityIdentifier, Is.EqualTo(entityEndingWithNumber));
        }

        private static (Entity[] entities, Fields field) SetupTestCaseWithEntityName(string entityEndingWithNumber)
        {
            var entities = new[]
                        {
                new Entity("Product", new EntityInstance[0]),
                new Entity(entityEndingWithNumber, Array.Empty<EntityInstance>())
            };
            var field = new Fields { Name = "Most_important_influence", varCode = "Most_important_influence_{Product}", CH1 = $"{{{entityEndingWithNumber}:value}}" };
            return (entities, field);
        }

        [TestCase(-1)]
        public void ShouldFailWhenValueIsEntityInstanceEndingInBadNumber(int number)
        {
            var entityEndingWithNumber = $"SEG{number}";
            var testCase = SetupTestCaseWithEntityName(entityEndingWithNumber);

            Assert.Throws<ArgumentException>(() => TestFieldDefinition.Create(testCase.field, testCase.entities), "Should fail with a negative number");
        }

        [TestCase("S1G")]
        [TestCase("S2G")]
        [TestCase("S2G1")]
        [TestCase("S2G1A")]
        public void ShouldSetValueTypeWhenValueIsEntityInstanceContainsANumber(string entityName)
        {
            var testCase = SetupTestCaseWithEntityName(entityName);

            var fieldDefinition = TestFieldDefinition.Create(testCase.field, testCase.entities);
            Assert.That(fieldDefinition.ValueEntityIdentifier, Is.EqualTo(entityName));
        }
    }
}