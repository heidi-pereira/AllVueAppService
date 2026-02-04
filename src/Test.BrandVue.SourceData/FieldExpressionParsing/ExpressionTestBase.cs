using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using TestCommon.Mocks;

namespace Test.BrandVue.SourceData.FieldExpressionParsing
{
    internal class ExpressionTestBase
    {
        protected static readonly EntityInstance[] Brand0AndBrand1 = MockMetadata.CreateEntityInstances(2);
        private readonly IGroupedQuotaCells _quotaCells = MockMetadata.CreateNonInterlockedQuotaCells(Subset, 2);
        protected static readonly Subset Subset = TestResponseFactory.AllSubset;
        protected TestResponseFactory _responseFactory;
        /// <summary>
        /// After first use, will not be aware of any later fields added
        /// </summary>
        protected ResponseFieldManager _responseFieldManager;
        private Lazy<IFieldExpressionParser> _parser;
        /// <summary>
        /// Lazily created on first use to ensure latest fields from _responseFieldManager are known
        /// </summary>
        protected IFieldExpressionParser Parser => _parser.Value;
        protected EntityTypeRepository _entityTypeRepository;
        private VariableEntityLoader _variableEntityLoader;
        protected ILoadableEntityInstanceRepository _entityInstanceRepository;

        [SetUp]
        public virtual void SetUp()
        {
            _entityTypeRepository = new TestEntityTypeRepository();
            _entityInstanceRepository = new TestEntityInstanceRepository();
            foreach (var brand in Brand0AndBrand1)
            {
                _entityInstanceRepository.Add(TestEntityTypeRepository.Brand, brand);
            }
            _responseFieldManager = new ResponseFieldManager(_entityTypeRepository);
            _responseFactory = new TestResponseFactory(_responseFieldManager);
            _variableEntityLoader = new VariableEntityLoader(_entityTypeRepository, _entityInstanceRepository,
                Substitute.For<ILoadableEntitySetRepository>());
            _parser = new(() => TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, _entityInstanceRepository, _entityTypeRepository));
        }

        protected ProfileResponseEntity CreateProfileFromSingleChoices(params (string FieldName, EntityInstance EntityInstance)[] entityResponses)
        {
            foreach (var entityResponse in entityResponses)
            {
                var field = _responseFieldManager.Get(entityResponse.FieldName);
                field.EnsureLoadOrderIndexInitialized_ThreadUnsafe();
            }

            var responses = entityResponses
                .Where(x => x.EntityInstance != null)
                .Select(x => (x.EntityInstance, new[] { (x.FieldName, x.EntityInstance.Id) })).ToArray();
            return CreateProfile(DateTimeOffset.Now, responses);
        }

        protected ProfileResponseEntity CreateProfile(params (EntityInstance EntityInstance, (string FieldName, int Value)[] EntityFields)[] entityResponses) =>
            CreateProfile(DateTimeOffset.Now, entityResponses);

        protected ProfileResponseEntity CreateProfile(DateTimeOffset surveyTime,
            params(EntityInstance EntityInstance, (string FieldName, int Value)[] EntityFields)[] entityResponses)
        {
            var answers = entityResponses.SelectMany(er => er.EntityFields.Select(ef =>
                    {
                        var field = _responseFieldManager.Get(ef.FieldName);
                        return TestAnswer.For(field, ef.Value, new EntityValue(TestEntityTypeRepository.Brand, er.EntityInstance.Id));
                    }
                )
            );
            return _responseFactory.CreateResponse(surveyTime, -1, answers.ToArray()).ProfileResponse;
        }


        protected EntityType AddEntityType(string id, string singular, string plural)
        {
            var eType = _entityTypeRepository.GetOrCreate(id);
            eType.DisplayNameSingular = singular;
            eType.DisplayNamePlural = plural;
            return eType;
        }

        protected void AddVariable(VariableConfiguration variable)
        {
            _variableEntityLoader.CreateOrUpdateEntityForVariable(variable);
            Parser.DeclareOrUpdateVariable(variable);
        }
    }
}
