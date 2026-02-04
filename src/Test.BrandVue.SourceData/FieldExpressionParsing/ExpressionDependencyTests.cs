using System;
using System.Linq;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Respondents;
using NUnit.Framework;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData.FieldExpressionParsing
{
    class ExpressionDependencyTests : ExpressionTestBase
    {
        private readonly EntityType _brandEntityType = TestEntityTypeRepository.Brand;
        private EntityType _occasionEntityType;
        private ResponseFieldDescriptor _zeroEntityField;
        private ResponseFieldDescriptor _brandField;
        private ResponseFieldDescriptor _brandOccasionField;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _occasionEntityType = AddEntityType("occasion", "Occasion", "Occasions");
            _zeroEntityField = _responseFieldManager.Add("zeroEntityField", types: Array.Empty<EntityType>());
            _brandField = _responseFieldManager.Add("brandField", types: new[] { _brandEntityType });
            _brandOccasionField = _responseFieldManager.Add("brandOccasionField", types: new[] { _brandEntityType, _occasionEntityType });
        }

        [Test]
        public void ZeroEntity() => AssertParsedUserEntities("zeroEntityField");
        [Test]
        public void OneEntityField_AllImplicit() => AssertParsedUserEntities("brandField", _brandEntityType);
        [Test]
        public void OneEntityField_ExplicitBrand() => AssertParsedUserEntities("any(response.brandField(brand=result.brand))", _brandEntityType);
        [Test]
        public void OneEntityField_ExplicitBrandExpression() => AssertParsedUserEntities("any(response.brandField(brand=result.brand - 1))", _brandEntityType);
        [Test]
        public void OneEntityField_AllBrand() => AssertParsedUserEntities("any(response.brandField())");
        [Test]
        public void OneEntityField_NotUserControlledBrand() => AssertParsedUserEntities("any(response.brandField(brand=3))");
        [Test]
        public void TwoEntityField_AllImplicit() => AssertParsedUserEntities("brandOccasionField", _brandEntityType, _occasionEntityType);
        [Test]
        public void TwoEntityField_AllBrand_AllOccasion() => AssertParsedUserEntities("any(response.brandOccasionField())");
        [Test]
        public void TwoEntityField_ExplicitBrand_AllOccasion() => AssertParsedUserEntities("any(response.brandOccasionField(brand=result.brand))", _brandEntityType);
        [Test]
        public void TwoEntityField_NotUserControlledBrand_AllOccasion() => AssertParsedUserEntities("any(response.brandOccasionField(brand=3))");
        [Test]
        public void TwoEntityField_AllBrand_ExplicitOccasion() => AssertParsedUserEntities("any(response.brandOccasionField(occasion=result.occasion))", _occasionEntityType);
        [Test]
        public void TwoEntityField_ExplicitBrand_ExplicitOccasion() => AssertParsedUserEntities("any(response.brandOccasionField(brand=result.brand, occasion=result.occasion))", _brandEntityType, _occasionEntityType);
        [Test]
        public void TwoEntityField_NotUserControlledBrand_ExplicitOccasion() => AssertParsedUserEntities("any(response.brandOccasionField(brand=3, occasion=result.occasion))", _occasionEntityType);
        [Test]
        public void TwoEntityField_AllBrand_NotUserControlledOccasion() => AssertParsedUserEntities("any(response.brandOccasionField(occasion=2%7))");
        [Test]
        public void TwoEntityField_ExplicitBrand_NotUserControlledOccasion() => AssertParsedUserEntities("any(response.brandOccasionField(brand=((result.brand)), occasion=4**4))", _brandEntityType);
        [Test]
        public void TwoEntityField_NotUserControlledBrand_NotUserControlledOccasion() => AssertParsedUserEntities("any(response.brandOccasionField(brand=3, occasion=8-3))");
        [Test]
        public void TwoEntityField_ExplicitBrand_VariableNotUserControlledOccasion() => AssertParsedUserEntities("any(response.brandOccasionField(brand=result.brand, occasion=result.brand))", _brandEntityType);

        private void AssertParsedUserEntities(string expression, params EntityType[] expectedUserEntityCombination)
        {
            var parsed = Parser.ParseUserNumericExpressionOrNull(expression);
            var possibleEntities = parsed.FieldDependencies.SelectMany(x => x.EntityCombination).Distinct();
            Assert.That(parsed.UserEntityCombination,
                Is.EquivalentTo(expectedUserEntityCombination));
        }
    }
}
