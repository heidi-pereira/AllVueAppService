using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData.FieldExpressionParsing
{
    class FieldExpressionTests : ExpressionTestBase
    {
        [Test]
        public void ShouldParseEmptyFilter()
        {
            var filterExpression =
                TestFieldExpressionParser.PrePopulateForFields(new ResponseFieldManager(_entityTypeRepository), Substitute.For<IEntityRepository>(), _entityTypeRepository).ParseUserBooleanExpression("");
            Assert.That(filterExpression.CreateForEntityValues(default)(null), Is.True);
        }

        [TestCase("", ExpectedResult = true)]
        [TestCase("age", ExpectedResult = true)]
        [TestCase("not age", ExpectedResult = false)]
        [TestCase("age == -999", ExpectedResult = false)]
        [TestCase("age == 35 and age == 45", ExpectedResult = false)]
        [TestCase("age == 35 or age == 45", ExpectedResult = true)]
        [TestCase("age in [35,36,37]", ExpectedResult = true)]
        [TestCase("age in [36,37,38]", ExpectedResult = false)]
        [TestCase("max(response.age()) == 35", ExpectedResult = true)]
        public bool Age35Respondent(string expression)
        {
            _responseFieldManager.Add("age");
            var response = _responseFactory.WithFieldValues(CreateProfile(), new[]{("age", 35, Enumerable.Empty<EntityValue>())});

            var filterExpression = Parser.ParseUserBooleanExpression(expression);
            var shouldIncludeForResult = filterExpression.CreateForEntityValues(default);
            return shouldIncludeForResult(response);
        }

        [TestCase("max(response.AlwaysOne()) == 1", ExpectedResult = true)]
        public bool AlwaysOne(string expression)
        {
            AddVariable(new VariableConfiguration() { Identifier = "AlwaysOne", Definition = new FieldExpressionVariableDefinition() { Expression = "1" } });
            var response = CreateProfile();
            var filterExpression = Parser.ParseUserBooleanExpression(expression);
            var shouldIncludeForResult = filterExpression.CreateForEntityValues(default);
            return shouldIncludeForResult(response);
        }

        [TestCase("", ExpectedResult = true)]
        [TestCase("consumer_segment == -999", ExpectedResult = false)]
        [TestCase("consumer_segment == 3 and consumer_segment == 4", ExpectedResult = false)]
        [TestCase("consumer_segment == 3 or consumer_segment == 4", ExpectedResult = false)]
        [TestCase("consumer_segment in [3,4,5]", ExpectedResult = false)]
        [TestCase("consumer_segment in [4,5,6]", ExpectedResult = false)]
        public bool DifferentBrandConsumerSegment3Respondent(string expression)
        {
            var brandWithResponse = Brand0AndBrand1[0];
            var brandQueried = Brand0AndBrand1[1];
            _responseFieldManager.Add("consumer_segment", types: new[] { TestEntityTypeRepository.Brand });
            var response = CreateProfile((brandWithResponse, new (string FieldName, int Value)[] { ("consumer_segment", 3) }));

            var filterExpression = Parser.ParseUserBooleanExpression(expression);
            var shouldIncludeForResult =
                filterExpression.CreateForEntityValues(new EntityValueCombination(new EntityValue(TestEntityTypeRepository.Brand, brandQueried.Id)));
            return shouldIncludeForResult(response);
        }

        [TestCase("", ExpectedResult = true)]
        [TestCase("consumer_segment == -999", ExpectedResult = false)]
        [TestCase("age == 35", ExpectedResult = true)]
        [TestCase("consumer_segment == 3 and consumer_segment == 4", ExpectedResult = false)]
        [TestCase("consumer_segment == 3 or consumer_segment == 4", ExpectedResult = true)]
        [TestCase("consumer_segment in [3,4,5]", ExpectedResult = true)]
        [TestCase("consumer_segment in [4,5,6]", ExpectedResult = false)]
        [TestCase("consumer_segment == -999 and age == 35", ExpectedResult = false)]
        [TestCase("consumer_segment == 3 and consumer_segment == 4 and age == 35", ExpectedResult = false)]
        [TestCase("consumer_segment == 3 or consumer_segment == 4 and age == 35", ExpectedResult = true)]
        [TestCase("consumer_segment in [3,4,5] and age == 35", ExpectedResult = true)]
        [TestCase("consumer_segment in [4,5,6] and age == 35", ExpectedResult = false)]
        [TestCase("consumer_segment == -999 and age == 45", ExpectedResult = false)]
        [TestCase("consumer_segment == 3 and consumer_segment == 4 and age == 45", ExpectedResult = false)]
        [TestCase("consumer_segment == 3 or consumer_segment == 4 and age == 45", ExpectedResult = true)]
        [TestCase("consumer_segment == 3 or (consumer_segment == 4 and age == 45)", ExpectedResult = true)]
        [TestCase("consumer_segment in [3,4,5] and age == 45", ExpectedResult = false)]
        [TestCase("consumer_segment in [4,5,6] and age == 45", ExpectedResult = false)]
        public bool SameBrandConsumerSegment3Respondent(string expression)
        {
            var brandWithResponse = Brand0AndBrand1[0];
            var brandQueried = brandWithResponse;

            _responseFieldManager.Add("age");
            _responseFieldManager.Add("consumer_segment", types: new[] { TestEntityTypeRepository.Brand });
            var response = CreateProfile((brandWithResponse, new[]{("consumer_segment", 3)}));
            response = _responseFactory.WithFieldValues(response, new[] { ("age", 35, Enumerable.Empty<EntityValue>()) });

            var filterExpression = Parser.ParseUserBooleanExpression(expression);
            var shouldIncludeForResult = filterExpression.CreateForEntityValues(new EntityValueCombination(new EntityValue(TestEntityTypeRepository.Brand, brandQueried.Id)));
            return shouldIncludeForResult(response);
        }

        [TestCase("", ExpectedResult = true)]
        [TestCase("any(response.consumer_segment())", ExpectedResult = true)]
        [TestCase("any(response.consumer_segment(brand=result.brand))", ExpectedResult = true)]
        [TestCase("any(response.consumer_segment(brand=result.brand)) and age == 35", ExpectedResult = true)]
        [TestCase("any(response.consumer_segment(brand=result.brand)) and age == 36", ExpectedResult = false)]
        [TestCase("any(response.consumer_segment(brand=4))", ExpectedResult = false)]
        [TestCase("any(response.consumer_segment(brand=4)) or any(response.consumer_segment(brand=1))", ExpectedResult = true)]
        [TestCase("any(response.consumer_segment(brand=4)) or age == 35", ExpectedResult = true)]
        public bool IncludeBasedOnOtherBrandResponses(string expression)
        {
            var brandWithResponse = Brand0AndBrand1[0];
            var brandQueried = brandWithResponse;

            _responseFieldManager.Add("age", types: Array.Empty<EntityType>());
            _responseFieldManager.Add("consumer_segment", types: new[] { TestEntityTypeRepository.Brand });
            var response = CreateProfile((brandWithResponse, new[]{("consumer_segment", 1) }));
            response = _responseFactory.WithFieldValues(response, new[] { ("age", 35, Enumerable.Empty<EntityValue>()) });

            var filterExpression = Parser.ParseUserBooleanExpression(expression);
            var shouldIncludeForResult = filterExpression.CreateForEntityValues(new EntityValueCombination
            (
                new EntityValue(TestEntityTypeRepository.Brand, brandQueried.Id)
            ));
            return shouldIncludeForResult(response);
        }

        //Multi-entity specific test cases
        [TestCase("any(response.order_channel(brand=result.brand, occasion=1)) and age == 35", 1, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=result.brand, occasion=2)) and age == 35", 1, ExpectedResult = false)]
        [TestCase("any(response.order_channel(brand=result.brand, occasion=result.occasion)) and age == 35", 1, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=result.brand, occasion=result.occasion)) and age == 35", 2, ExpectedResult = false)]
        //Test cases match previous test to ensure adding entity doesn't break anything
        [TestCase("", 1, ExpectedResult = true)]
        [TestCase("any(response.order_channel())", 1, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=result.brand))", 1, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=result.brand)) and age == 35", 1, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=result.brand)) and age == 36", 1, ExpectedResult = false)]
        [TestCase("any(response.order_channel(brand=4))", 2, ExpectedResult = false)]
        [TestCase("any(response.order_channel(brand=4)) or any(response.order_channel(brand=1))", 1, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=4)) or age == 35", 1, ExpectedResult = true)]
        [TestCase("", 2, ExpectedResult = true)]
        [TestCase("any(response.order_channel())", 2, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=result.brand))", 2, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=result.brand)) and age == 35", 2, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=result.brand)) and age == 36", 2, ExpectedResult = false)]
        [TestCase("any(response.order_channel(brand=4))", 2, ExpectedResult = false)]
        [TestCase("any(response.order_channel(brand=4)) or any(response.order_channel(brand=1))", 2, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=4)) or age == 35", 2, ExpectedResult = true)]
        [TestCase("any(response.order_channel(brand=[3,4])) or age == 35", 2, ExpectedResult = true)]
        public bool IncludeBasedOnOtherMultiEntityResponses(string expression, int resultOccasion)
        {
            var brandWithResponse = Brand0AndBrand1[0];
            var brandQueried = brandWithResponse;

            var occasionEntityType = AddEntityType("occasion", "Occasion", "Occasions");
            _responseFieldManager.Add("age", types: Array.Empty<EntityType>());
            _responseFieldManager.Add("order_channel", types: new[] { TestEntityTypeRepository.Brand, occasionEntityType });
            var response = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("age", 35, Enumerable.Empty<EntityValue>()),
                ("order_channel", 1,
                    new[] {new EntityValue(TestEntityTypeRepository.Brand, brandWithResponse.Id), new EntityValue(occasionEntityType, 1) }
                )
            });

            var filterExpression = Parser.ParseUserBooleanExpression(expression);
            var shouldIncludeForResult = filterExpression.CreateForEntityValues(new EntityValueCombination
            (
                new EntityValue(TestEntityTypeRepository.Brand, brandQueried.Id),
                new EntityValue(occasionEntityType, resultOccasion)
            ));
            return shouldIncludeForResult(response);
        }

        [TestCase("spend > 100000", true, ExpectedResult = true)]
        [TestCase("spend < 100000", true, ExpectedResult = false)]
        [TestCase("spend > 100000", false, ExpectedResult = false)]
        [TestCase("spend < 100000", false, ExpectedResult = true)]
        public bool ScaleLargeValues(string expression, bool isAutoGenerated)
        {
            var subset = new Subset { Id = "scaletest" };
            var spendField = _responseFieldManager.Add("spend", Array.Empty<EntityType>());
            var fieldScaleFactor = 0.01;
            var dataAccessModel = new FieldDefinitionModel(spendField.Name, "", "", "", "", fieldScaleFactor, "", EntityInstanceColumnLocation.Unknown, "", false, null, Enumerable.Empty<EntityFieldDefinitionModel>(), null)
            {
                IsAutoGenerated = isAutoGenerated
            };
            spendField.AddDataAccessModelForSubset(subset.Id, dataAccessModel);
            //Loading from DB would scale 200,000 down to 2000 here, this should then be scaled back up
            var response = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                (spendField.Name, 2000, Enumerable.Empty<EntityValue>()),
            }, subset);
            var filterExpression = Parser.ParseUserBooleanExpression(expression);
            var shouldIncludeForResult = filterExpression.CreateForEntityValues(default);
            return shouldIncludeForResult(response);
        }

        [TestCase("sum([1 for r in response.Consideration_per_occasion(brand = result.brand) if r == 1])", true, ExpectedResult = 2)]
        [TestCase("sum(1 for r in response.Consideration_per_occasion(brand = result.brand) if r == 1)", true, ExpectedResult = 2)]
        [TestCase("sum([1 for r in response.Consideration_per_occasion(brand = result.brand) if r == 1])", false, ExpectedResult = 0)]
        [TestCase("sum(1 for r in response.Consideration_per_occasion(brand = result.brand) if r == 1)", false, ExpectedResult = 0)]
        public int? MultiEntityQuestion_AskedMultipleTimesForBothEntities_QuerySingle(string expression, bool queryBrandWithResponses)
        {
            var brandWithResponse = Brand0AndBrand1[0];
            var brandQueried = queryBrandWithResponses ? brandWithResponse : Brand0AndBrand1[1];
            var occasionEntityType = AddEntityType("likelihoodoccasion", "Occasion", "Occasions");
            _responseFieldManager.Add("age", types: Array.Empty<EntityType>());
            _responseFieldManager.Add("Consideration_per_occasion", types: new[] { TestEntityTypeRepository.Brand, occasionEntityType });
            var response = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("age", 35, Enumerable.Empty<EntityValue>()),
                ("Consideration_per_occasion", 1,
                    new[] {new EntityValue(TestEntityTypeRepository.Brand, brandWithResponse.Id), new EntityValue(occasionEntityType, 1) }
                ),
                ("Consideration_per_occasion", 1,
                    new[] {new EntityValue(TestEntityTypeRepository.Brand, brandWithResponse.Id), new EntityValue(occasionEntityType, 2) }
                ),
                ("Consideration_per_occasion", -99,
                    new[] {new EntityValue(TestEntityTypeRepository.Brand, brandWithResponse.Id), new EntityValue(occasionEntityType, 3) }
                ),
            });

            var filterExpression = Parser.ParseUserNumericExpressionOrNull(expression);
            var calculate = filterExpression.CreateForEntityValues(
                new EntityValueCombination (new EntityValue(TestEntityTypeRepository.Brand, brandQueried.Id))
            );
            return calculate(response);
        }

        [TestCase("Consideration_per_occasion == max(response.Consideration_per_occasion(brand=1))", true, ExpectedResult = 1)]
        [TestCase("Consideration_per_occasion == max(response.Consideration_per_occasion(brand=1))", false, ExpectedResult = 0)]
        public int? MultiEntityQuestion_AskedMultipleTimesForBothEntities_QueryBoth(string expression, bool queryBrandWithResponses)
        {
            var brandWithResponse = Brand0AndBrand1[0];
            var brandQueried = queryBrandWithResponses ? brandWithResponse : Brand0AndBrand1[1];
            var occasionEntityType = AddEntityType("likelihoodoccasion", "Occasion", "Occasions");
            _responseFieldManager.Add("age", types: Array.Empty<EntityType>());
            _responseFieldManager.Add("Consideration_per_occasion", types: new[] { TestEntityTypeRepository.Brand, occasionEntityType });
            int occasionWithResponse1 = 2;
            var response = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("age", 35, Enumerable.Empty<EntityValue>()),
                ("Consideration_per_occasion", 1,
                    new[] {new EntityValue(TestEntityTypeRepository.Brand, brandWithResponse.Id), new EntityValue(occasionEntityType, 1) }
                ),
                ("Consideration_per_occasion", 1,
                    new[] {new EntityValue(TestEntityTypeRepository.Brand, brandWithResponse.Id), new EntityValue(occasionEntityType, occasionWithResponse1) }
                ),
                ("Consideration_per_occasion", -99,
                    new[] {new EntityValue(TestEntityTypeRepository.Brand, brandWithResponse.Id), new EntityValue(occasionEntityType, 3) }
                ),
            });

            var filterExpression = Parser.ParseUserNumericExpressionOrNull(expression);
            var calculate = filterExpression.CreateForEntityValues(
                new EntityValueCombination(new EntityValue(TestEntityTypeRepository.Brand, brandQueried.Id), new EntityValue(occasionEntityType, occasionWithResponse1))
            );
            return calculate(response);
        }

        [TestCase("any(f > 3 for f in response.Likelihood_occasion_scale())", false, ExpectedResult = 0)]
        [TestCase("not any(f > 3 for f in response.Likelihood_occasion_scale())", true, ExpectedResult = 0)]
        [TestCase("any(f > 3 for f in response.Likelihood_occasion_scale())", true, ExpectedResult = 1)]
        [TestCase("response.Likelihood_occasion_scale().count(4) or response.Likelihood_occasion_scale().count(5)", true, ExpectedResult = 1)]
        public int? SingleEntityQuestion_WithScaleValue_AskedForEveryEntity(string expression, bool hasResponseOf4OrGreater)
        {
            var occasionEntityType = AddEntityType("likelihoodoccasion", "Occasion", "Occasions");
            _responseFieldManager.Add("age", types: Array.Empty<EntityType>());
            _responseFieldManager.Add("Likelihood_occasion_scale", types: new[] { occasionEntityType });
            var response = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("age", 35, Enumerable.Empty<EntityValue>()),
                ("Likelihood_occasion_scale", (hasResponseOf4OrGreater ? 4 : 2),
                    new[] {new EntityValue(occasionEntityType, 1) }
                ),
                ("Likelihood_occasion_scale", 3,
                    new[] {new EntityValue(occasionEntityType, 2) }
                ),
                ("Likelihood_occasion_scale", 1,
                    new[] {new EntityValue(occasionEntityType, 3) }
                ),
            });

            var filterExpression = Parser.ParseUserNumericExpressionOrNull(expression);
            var calculate = filterExpression.CreateForEntityValues(default);
            return calculate(response);
        }
    }
}
