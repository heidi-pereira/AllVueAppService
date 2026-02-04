using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Mocks;

namespace Test.BrandVue.SourceData.FieldExpressionParsing
{
    /// <summary>
    /// All test cases here can be directly pasted into a python interpreter with no other variables defined
    /// </summary>
    class ConstantFieldExpressionTests
    {
        private static readonly Subset Subset = TestResponseFactory.AllSubset;
        private static readonly EntityInstance[] Brand0AndBrand1 = MockMetadata.CreateEntityInstances(2);
        private readonly IGroupedQuotaCells _quotaCells = MockMetadata.CreateNonInterlockedQuotaCells(Subset, 2);
        private ResponseFieldManager _responseFieldManager;
        private TestResponseFactory _responseFactory;
        private IFieldExpressionParser _parser;
        private EntityTypeRepository _entityTypeRepository;
        private ProfileResponseEntity _profile;

        [SetUp]
        public void SetUp()
        {
            _entityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
            _responseFieldManager = new ResponseFieldManager(_entityTypeRepository);
            _responseFactory = new TestResponseFactory(_responseFieldManager);
            _parser = TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, Substitute.For<IEntityRepository>(), _entityTypeRepository);
            _profile = CreateProfile();
        }

        [TestCase("True")]
        [TestCase("1")]
        [TestCase("1 + 1")]
        [TestCase("2 ** 3")]
        [TestCase("4 // 3")]
        [TestCase("(None and True) == None")]
        [TestCase("(0 and True) == 0")]
        [TestCase("(True and 0) == 0")]
        [TestCase("(234 or True) == 234")]
        [TestCase("(False or 234) == 234")]
        [TestCase("None or True")]
        [TestCase("sum(1 for r in [1] if None) + sum([1 for r in [1] if None]) == 0")]
        [TestCase("1 < 2")]
        [TestCase("4 > 3 > 2")]
        [TestCase("2 < 3 < 4")]
        [TestCase("2 < 3 < 4 < 5 < 6 < 7")]
        [TestCase("-4 < -3 < -2")]
        [TestCase("-7 < -6 < -5 < -4 < -3 < -2")]
        [TestCase("False if False else True")]
        [TestCase("True if True else False")]
        [TestCase("False if None else True")]
        [TestCase("6 > 3 if 6 > 5 else 0 > 5")]
        [TestCase("0 > 5 if 3 > 5 else 6 > 5")]
        [TestCase("max([0,1])")]
        [TestCase("min([-1,0])")]
        [TestCase("sum([-1,0, 2])")]
        [TestCase("any([-1])")]
        [TestCase("any([1])")]
        [TestCase("[1].count(1)")]
        [TestCase("[0,None].count(None)")]
        [TestCase("len([None])")]
        [TestCase("len([0])")]
        [TestCase("len([1])")]
        [TestCase("len([1, 2])")]
        [TestCase("min([], default=None)==None")]
        [TestCase("max([], default=None)==None")]
        [TestCase("min([], default=3)==3")]
        [TestCase("max([], default=3)==3")]
        [TestCase("{1:3}.get(1)")]
        [TestCase("len({1:[3]}.get(1))")]
        [TestCase("{1:3}.get(1, 0)")]
        [TestCase("len({1:[3]}.get(1, []))")]
        [TestCase("len({1:[3]}.get(2, [1]))")]
        public void EvaluatesToTrueInPython(string expression)
        {
            var shouldIncludeForResult = Parse(expression);
            var result = shouldIncludeForResult(CreateProfile());
            Assert.That(result, Is.True);
        }

        [TestCase("False")]
        [TestCase("any([None, False])")]
        [TestCase("2 // 3")]
        [TestCase("0 ** 3")]
        [TestCase("4 < 3 < 2")]
        [TestCase("2 > 3 > 4")]
        [TestCase("2 > 3 > 4 > 5 > 6 > 7")]
        [TestCase("-4 > -3 > -2")]
        [TestCase("-7 > -6 > -5 > -4 > -3 > -2")]
        [TestCase("True if False else False")]
        [TestCase("False if True else True")]
        [TestCase("True if None else False")]
        [TestCase("0 > 5 if 3 < 5 else 6 > 5")]
        [TestCase("len([])")]
        [TestCase("min([0,1])")]
        [TestCase("max([-1,0])")]
        [TestCase("sum([-1,0, 1])")]
        [TestCase("[1].count(2)")]
        [TestCase("[2].count(1)")]
        [TestCase("[None].count(1)")]
        [TestCase("[1].count(None)")]
        [TestCase("0 or None or 0")]
        [TestCase("{1:0}.get(1)")]
        [TestCase("{1:1}.get(111)")]
        [TestCase("len({1:[]}.get(1))")]
        [TestCase("len({1:[]}.get(111))")]
        [TestCase("{1:0}.get(1, 1)")]
        [TestCase("{1:1}.get(111, 0)")]
        [TestCase("len({1:[]}.get(1, [1]))")]
        [TestCase("len({1:[1]}.get(111, []))")]
        public void EvaluatesToFalseInPython(string expression)
        {
            var shouldIncludeForResult = Parse(expression);
            var result = shouldIncludeForResult(CreateProfile());
            Assert.That(result, Is.False);
        }

        [TestCase("18/9", ExpectedResult = 2)]
        [TestCase("8//9", ExpectedResult = 0)]
        [TestCase("11//9", ExpectedResult = 1)]
        [TestCase("18//9", ExpectedResult = 2)]
        [TestCase("-3", ExpectedResult = -3)]
        [TestCase("~0", ExpectedResult = -1)]
        [TestCase("~-1", ExpectedResult = 0)]
        [TestCase("0 or None or None", ExpectedResult = null)]
        public int? EvaluatesToValue(string expression)
        {
            var filterExpression = _parser.ParseUserNumericExpressionOrNull(expression);
            var shouldIncludeForResult = filterExpression.CreateForEntityValues(default);
            var result = shouldIncludeForResult(CreateProfile());
            return result;
        }

        /// <summary>
        /// Our execution model slightly differs from Python here since we don't use floating point.
        /// This behaviour is used in finance in order to average numbers with sum/count, when it really should have used "floor divide" i.e. "sum//count"
        /// In future we may want to eradicate the uses of "/" and force people to use "//". That would be most pressing if we decided to allow floating point arithmetic
        /// </summary>
        [TestCase("8/9", ExpectedResult = 0)] //Python would return 0.888...
        [TestCase("11/9", ExpectedResult = 1)] //Python would return 1.222...
        public int? EvaluatesToValueSpecialCase(string expression)
        {
            var filterExpression = _parser.ParseUserNumericExpressionOrNull(expression);
            var shouldIncludeForResult = filterExpression.CreateForEntityValues(default);
            var result = shouldIncludeForResult(CreateProfile());
            return result;
        }

        /// <summary>
        /// There's a minor quirk here which is that because the expression always throws (regardless of profile), we throw before a profile is provided
        /// This is an artifact of folding constants, and hopefully stops people writing/saving expressions that definitely will blow up when run
        /// </summary>
        [TestCase("1 > None")]
        [TestCase("None < 1")]
        [TestCase("sum([None])")]
        [TestCase("max([1,None])")]
        [TestCase("max([None])")]
        [TestCase("min([1,None])")]
        [TestCase("min([None])")]
        [TestCase("sum([None])")]
        [TestCase("~None")]
        [TestCase("-None")]
        public void AlwaysThrowsInPython(string expression)
        {
            Assert.That(
                () => _parser.ParseUserBooleanExpression(expression).CreateForEntityValues(default),
                Throws.InvalidOperationException
            );
        }

        [Test]
        public void ThrowsInParsingWhenCouldOverflowStack()
        {
            string expression = string.Join(" + ", Enumerable.Range(1, 600));
            Assert.That(() => _parser.ParseUserBooleanExpression(expression), Throws.Exception);
        }


        [TestCase("len({1:[3]}.get(2, default=[1]))", Description = "Extra arg")]
        public void ThrowsParseError(string expression)
        {
            Assert.That(() => _parser.ParseUserBooleanExpression(expression), Throws.Exception);
        }

        private Func<IProfileResponseEntity, bool> Parse(string expression)
        {
            var filterExpression = _parser.ParseUserBooleanExpression(expression);
            return filterExpression.CreateForEntityValues(default);
        }

        private ProfileResponseEntity CreateProfile() => _responseFactory.CreateResponse(DateTimeOffset.Now, -1, Array.Empty<TestAnswer>()).ProfileResponse;
    }
}
