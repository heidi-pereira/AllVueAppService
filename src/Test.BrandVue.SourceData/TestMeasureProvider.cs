using System;
using System.Collections.Generic;
using BrandVue.EntityFramework;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using Microsoft.Extensions.Logging.Abstractions;
using Test.BrandVue.SourceData.FieldExpressionParsing;
using TestCommon;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{
    internal class TestMeasureProvider
    {
        private readonly CalculationType _calculationType;

        public TestMeasureProvider(CalculationType calculationType)
        {
            _calculationType = calculationType;
        }

        public TestMetric[] GetAllTestMeasures()
        {
            var entityTypeRepository = new TestEntityTypeRepository();
            var responseFieldManager = new ResponseFieldManager(entityTypeRepository);
            var profileField1 = responseFieldManager.Add("Profile1");
            var profileField2 = responseFieldManager.Add("Profile2");
            var profileField3 = responseFieldManager.Add("Profile3");

            var brandField1 = responseFieldManager.Add("brand1", TestEntityTypeRepository.Brand);
            var brandField2 = responseFieldManager.Add("brand2", TestEntityTypeRepository.Brand);
            var brandField3 = responseFieldManager.Add("brand3", TestEntityTypeRepository.Brand);

            var productField1 = responseFieldManager.Add("product1", TestEntityTypeRepository.Product);

            var brandProductField1 = responseFieldManager.Add("brandProduct1", TestEntityTypeRepository.Brand, TestEntityTypeRepository.Product);
            var brandProductField2 = responseFieldManager.Add("brandProduct2", TestEntityTypeRepository.Brand, TestEntityTypeRepository.Product);
            var brandProductField3 = responseFieldManager.Add("brandProduct3", TestEntityTypeRepository.Brand, TestEntityTypeRepository.Product);

            var brand = new EntityValue(TestEntityTypeRepository.Brand, 1);
            var product = new EntityValue(TestEntityTypeRepository.Product, 11);
            var waveEntityType = new EntityType("DataWave", "wave", "waves");
            var wave = new EntityValue(waveEntityType, 1);
            var surveyEntityType = new EntityType("SurveyCondition", "survey", "surveys");
            var survey = new EntityValue(surveyEntityType, 1);
            var entityRepository = new EntityInstanceRepository();
            entityRepository.Add(TestEntityTypeRepository.Brand, brand.AsInstance());
            entityRepository.Add(TestEntityTypeRepository.Product, product.AsInstance());
            entityRepository.Add(waveEntityType, wave.AsInstance());
            entityRepository.Add(surveyEntityType, survey.AsInstance());

            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(responseFieldManager, entityRepository, entityTypeRepository);
            var dataWaveGroupDefinition = DateWaveVariableTests.GetDataWaveGroupDefinition(DateTime.Parse("2019-01-01"), TimeSpan.FromDays(30));
            var waveMeasure = new TestMetric("WaveMeasureWithAlways1Base", _calculationType, fieldExpressionParser, DefaultAverageRepositoryData.CustomPeriodAverage)
                .WithPrimaryExpression(new DataWaveVariable(dataWaveGroupDefinition))
                .WithBaseExpression(fieldExpressionParser.ParseUserBooleanExpression("1")).CheckResultFor(wave);

            var surveyGroupDefinition = SurveyIdVariableTests.GetSurveyCondition(1.Yield(), 2.Yield());
            var surveyMeasure = new TestMetric("SurveyIdMeasureWithAlways1Base", _calculationType, fieldExpressionParser, DefaultAverageRepositoryData.CustomPeriodAverage)
                .WithPrimaryExpression(new SurveyIdVariable(surveyGroupDefinition))
                .WithBaseExpression(fieldExpressionParser.ParseUserBooleanExpression("1")).CheckResultFor(survey);

            var profileNoBase = new TestMetric("ProfilePrimaryWithNoBase", _calculationType, fieldExpressionParser).WithPrimaryField(profileField1);
            var profileProfileBase = new TestMetric("ProfilePrimaryWithProfileBase", _calculationType, fieldExpressionParser).WithPrimaryField(profileField1).WithBaseField(profileField3);
            var singleEntityPrimaryWithNoBase = new TestMetric("SingleEntityPrimaryWithNoBase", _calculationType, fieldExpressionParser).WithPrimaryField(brandField1).CheckResultFor(brand);
            var singleEntityPrimaryAndBase = new TestMetric("SingleEntityPrimaryAndBase", _calculationType, fieldExpressionParser).WithPrimaryField(brandField1).WithBaseField(brandField3).CheckResultFor(brand);
            var singleEntityPrimaryAndProfileBase = new TestMetric("SingleEntityPrimaryAndProfileBase", _calculationType, fieldExpressionParser).WithPrimaryField(brandField1).WithBaseField(profileField1).CheckResultFor(brand);
            var profilePrimaryAndSingleEntityBase = new TestMetric("ProfilePrimaryAndSingleEntityBase", _calculationType, fieldExpressionParser).WithPrimaryField(profileField1).WithBaseField(brandField1).CheckResultFor(brand);
            var multiEntityPrimaryNoBase = new TestMetric("MultiEntityPrimaryNoBase", _calculationType, fieldExpressionParser).WithPrimaryField(brandProductField1).WithFilterInstance(brand).CheckResultFor(product);
            var profilePrimaryMultiEntityBase = new TestMetric("ProfilePrimaryMultiEntityBase", _calculationType, fieldExpressionParser).WithPrimaryField(profileField1).WithBaseField(brandProductField1).WithFilterInstance(brand).CheckResultFor(product);
            var multiEntityPrimaryProfileBase = new TestMetric("MultiEntityPrimaryProfileBase", _calculationType, fieldExpressionParser).WithPrimaryField(brandProductField1).WithBaseField(profileField1).WithFilterInstance(brand).CheckResultFor(product);
            var entity1PrimaryEntity2Base = new TestMetric("Entity1PrimaryEntity2Base", _calculationType, fieldExpressionParser).WithPrimaryField(brandField1).WithBaseField(productField1).WithFilterInstance(brand).CheckResultFor(product);
            var singleEntityPrimaryMultiEntityBase = new TestMetric("SingleEntityPrimaryMultiEntityBase", _calculationType, fieldExpressionParser).WithPrimaryField(brandField1).WithBaseField(brandProductField1).WithFilterInstance(brand).CheckResultFor(product);
            var multiEntityPrimarySingleEntityBase = new TestMetric("MultiEntityPrimarySingleEntityBase", _calculationType, fieldExpressionParser).WithPrimaryField(brandProductField1).WithBaseField(brandField1).WithFilterInstance(brand).CheckResultFor(product);
            var multiEntityPrimaryMultiEntityBase = new TestMetric("MultiEntityPrimaryMultiEntityBase", _calculationType, fieldExpressionParser).WithPrimaryField(brandProductField1).WithBaseField(brandProductField3).WithFilterInstance(product).CheckResultFor(brand);

            return new[]
            {
                surveyMeasure,
                waveMeasure,
                profileNoBase,
                profileNoBase.WithSecondaryField(profileField2, FieldOperation.Minus),
                profileNoBase.WithSecondaryField(profileField2, FieldOperation.Plus),
                profileNoBase.WithSecondaryField(profileField2, FieldOperation.Or),
                profileNoBase.WithSecondaryField(profileField2, FieldOperation.Filter),
                profileProfileBase,
                profileProfileBase.WithSecondaryField(profileField2, FieldOperation.Minus),
                profileProfileBase.WithSecondaryField(profileField2, FieldOperation.Plus),
                profileProfileBase.WithSecondaryField(profileField2, FieldOperation.Or),
                profileProfileBase.WithSecondaryField(profileField2, FieldOperation.Filter),
                profilePrimaryAndSingleEntityBase,
                profilePrimaryAndSingleEntityBase.WithSecondaryField(profileField2, FieldOperation.Minus),
                profilePrimaryAndSingleEntityBase.WithSecondaryField(profileField2, FieldOperation.Plus),
                profilePrimaryAndSingleEntityBase.WithSecondaryField(profileField2, FieldOperation.Or),
                profilePrimaryAndSingleEntityBase.WithSecondaryField(profileField2, FieldOperation.Filter),
                profilePrimaryMultiEntityBase,
                profilePrimaryMultiEntityBase.WithSecondaryField(profileField2, FieldOperation.Minus),
                profilePrimaryMultiEntityBase.WithSecondaryField(profileField2, FieldOperation.Plus),
                profilePrimaryMultiEntityBase.WithSecondaryField(profileField2, FieldOperation.Or),
                profilePrimaryMultiEntityBase.WithSecondaryField(profileField2, FieldOperation.Filter),
                singleEntityPrimaryWithNoBase,
                singleEntityPrimaryWithNoBase.WithSecondaryField(brandField2, FieldOperation.Minus),
                singleEntityPrimaryWithNoBase.WithSecondaryField(brandField2, FieldOperation.Plus),
                singleEntityPrimaryWithNoBase.WithSecondaryField(brandField2, FieldOperation.Or),
                singleEntityPrimaryWithNoBase.WithSecondaryField(brandField2, FieldOperation.Filter),
                singleEntityPrimaryAndBase,
                singleEntityPrimaryAndBase.WithSecondaryField(brandField2, FieldOperation.Minus),
                singleEntityPrimaryAndBase.WithSecondaryField(brandField2, FieldOperation.Plus),
                singleEntityPrimaryAndBase.WithSecondaryField(brandField2, FieldOperation.Or),
                singleEntityPrimaryAndBase.WithSecondaryField(brandField2, FieldOperation.Filter),
                singleEntityPrimaryAndProfileBase,
                singleEntityPrimaryAndProfileBase.WithSecondaryField(brandField2, FieldOperation.Minus),
                singleEntityPrimaryAndProfileBase.WithSecondaryField(brandField2, FieldOperation.Plus),
                singleEntityPrimaryAndProfileBase.WithSecondaryField(brandField2, FieldOperation.Or),
                singleEntityPrimaryAndProfileBase.WithSecondaryField(brandField2, FieldOperation.Filter),
                singleEntityPrimaryAndProfileBase.WithSecondaryField(productField1, FieldOperation.Minus),
                singleEntityPrimaryAndProfileBase.WithSecondaryField(productField1, FieldOperation.Plus),
                singleEntityPrimaryAndProfileBase.WithSecondaryField(productField1, FieldOperation.Or),
                singleEntityPrimaryAndProfileBase.WithSecondaryField(productField1, FieldOperation.Filter),
                singleEntityPrimaryMultiEntityBase,
                singleEntityPrimaryMultiEntityBase.WithSecondaryField(brandField2, FieldOperation.Minus),
                singleEntityPrimaryMultiEntityBase.WithSecondaryField(brandField2, FieldOperation.Plus),
                singleEntityPrimaryMultiEntityBase.WithSecondaryField(brandField2, FieldOperation.Or),
                singleEntityPrimaryMultiEntityBase.WithSecondaryField(brandField2, FieldOperation.Filter),
                entity1PrimaryEntity2Base,
                entity1PrimaryEntity2Base.WithSecondaryField(brandField2, FieldOperation.Minus),
                entity1PrimaryEntity2Base.WithSecondaryField(brandField2, FieldOperation.Plus),
                entity1PrimaryEntity2Base.WithSecondaryField(brandField2, FieldOperation.Or),
                entity1PrimaryEntity2Base.WithSecondaryField(brandField2, FieldOperation.Filter),
                multiEntityPrimaryNoBase,
                multiEntityPrimaryNoBase.WithSecondaryField(brandProductField2, FieldOperation.Minus),
                multiEntityPrimaryNoBase.WithSecondaryField(brandProductField2, FieldOperation.Plus),
                multiEntityPrimaryNoBase.WithSecondaryField(brandProductField2, FieldOperation.Or),
                multiEntityPrimaryNoBase.WithSecondaryField(brandProductField2, FieldOperation.Filter),
                multiEntityPrimaryProfileBase,
                multiEntityPrimaryProfileBase.WithSecondaryField(brandProductField2, FieldOperation.Minus),
                multiEntityPrimaryProfileBase.WithSecondaryField(brandProductField2, FieldOperation.Plus),
                multiEntityPrimaryProfileBase.WithSecondaryField(brandProductField2, FieldOperation.Or),
                multiEntityPrimaryProfileBase.WithSecondaryField(brandProductField2, FieldOperation.Filter),
                multiEntityPrimarySingleEntityBase,
                multiEntityPrimarySingleEntityBase.WithSecondaryField(brandProductField2, FieldOperation.Minus),
                multiEntityPrimarySingleEntityBase.WithSecondaryField(brandProductField2, FieldOperation.Plus),
                multiEntityPrimarySingleEntityBase.WithSecondaryField(brandProductField2, FieldOperation.Or),
                multiEntityPrimarySingleEntityBase.WithSecondaryField(brandProductField2, FieldOperation.Filter),
                multiEntityPrimarySingleEntityBase.WithSecondaryField(profileField1, FieldOperation.Minus),
                multiEntityPrimarySingleEntityBase.WithSecondaryField(profileField1, FieldOperation.Plus),
                multiEntityPrimarySingleEntityBase.WithSecondaryField(profileField1, FieldOperation.Or),
                multiEntityPrimarySingleEntityBase.WithSecondaryField(profileField1, FieldOperation.Filter),
                multiEntityPrimaryMultiEntityBase,
                multiEntityPrimaryMultiEntityBase.WithSecondaryField(brandProductField2, FieldOperation.Minus),
                multiEntityPrimaryMultiEntityBase.WithSecondaryField(brandProductField2, FieldOperation.Plus),
                multiEntityPrimaryMultiEntityBase.WithSecondaryField(brandProductField2, FieldOperation.Or),
                multiEntityPrimaryMultiEntityBase.WithSecondaryField(brandProductField2, FieldOperation.Filter),
            };
        }
    }
}
