using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.PublicApi.Definitions;
using BrandVue.PublicApi.Services;
using BrandVue.Services;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd.SurveyApi.Services
{
    [TestFixture]
    public class ResponseFieldDescriptorLoaderTests
    {
        private static readonly IEnumerable<ResponseFieldDescriptor> BaseFieldAllowedList = new List<ResponseFieldDescriptor>
        {
            CreateResponseDescriptor("AllowedProfile1Base"),
            CreateResponseDescriptor("AllowedBrand1Base", TestEntityTypeRepository.Brand),
            CreateResponseDescriptor("AllowedProductBrand1Base", TestEntityTypeRepository.Brand, TestEntityTypeRepository.Product),
            CreateResponseDescriptor("AllowedProductProfile1Base", TestEntityTypeRepository.Product),
        };

        private static readonly IEnumerable<ResponseFieldDescriptor> FieldAllowedList = new List<ResponseFieldDescriptor>
        {
            CreateResponseDescriptor("AllowedProfile1"),
            CreateResponseDescriptor("AllowedProfile2"),
            CreateResponseDescriptor("AllowedBrand1", TestEntityTypeRepository.Brand),
            CreateResponseDescriptor("AllowedBrand3", TestEntityTypeRepository.Brand),
            CreateResponseDescriptor("AllowedProductBrand1", TestEntityTypeRepository.Brand, TestEntityTypeRepository.Product),
            CreateResponseDescriptor("AllowedProductProfile1", TestEntityTypeRepository.Product),
        };

        private static readonly IEnumerable<ResponseFieldDescriptor> FullAllowedList =
            FieldAllowedList.Concat(BaseFieldAllowedList);

        private static readonly IEnumerable<ResponseFieldDescriptor> ForbiddenList = new List<ResponseFieldDescriptor>
        {
            CreateResponseDescriptor("ForbiddenProfile1"),
            CreateResponseDescriptor("ForbiddenBrand1", TestEntityTypeRepository.Brand),
            CreateResponseDescriptor("ForbiddenBrand2", TestEntityTypeRepository.Brand),
            CreateResponseDescriptor("ForbiddenProductBrand1", TestEntityTypeRepository.Brand, TestEntityTypeRepository.Product),
            CreateResponseDescriptor("ForbiddenProductProfile1", TestEntityTypeRepository.Product),
        };

        public static readonly IEnumerable<TestCaseData> PrimaryResponseEntityTypeOnlyCases = new List<TestCaseData>
        {
            new(Enumerable.Empty<EntityType>(), FullAllowedList.Where(f => !f.EntityCombination.Any()).ToList()),
            new(TestEntityTypeRepository.Brand.Yield(), FullAllowedList.Where(f => f.EntityCombination.IsEquivalent(new[] {TestEntityTypeRepository.Brand})).ToList()),
            new(TestEntityTypeRepository.Product.Yield(), FullAllowedList.Where(f => f.EntityCombination.IsEquivalent(new[] {TestEntityTypeRepository.Product})).ToList()),
        };

        public static readonly IEnumerable<TestCaseData> SubResponseEntityTypeCases = new List<TestCaseData>
        {
            new(new [] { TestEntityTypeRepository.Product, TestEntityTypeRepository.Brand }, FullAllowedList.Where(f => f.EntityCombination.IsEquivalent(new[] {TestEntityTypeRepository.Brand, TestEntityTypeRepository.Product})).ToList()),
            new(new [] { TestEntityTypeRepository.Product }, FullAllowedList.Where(f => f.EntityCombination.IsEquivalent(new[] {TestEntityTypeRepository.Product})).ToList())
        };

        [Test, TestCaseSource(nameof(PrimaryResponseEntityTypeOnlyCases))]
        public void ResponseFieldDescriptorLoaderCorrectlyRemovesFieldsFromMetricsThatAreNotAllowed_PrimaryResponseEntityTypeOnly(IEnumerable<EntityType> responseEntityTypes,
            List<ResponseFieldDescriptor> expectedResponseFieldDescriptors)
        {
            var realResponseFieldDescriptorLoader = Arrange();
            var fields = Act(responseEntityTypes, realResponseFieldDescriptorLoader);
            Assert(expectedResponseFieldDescriptors, fields);
        }

        [Test, TestCaseSource(nameof(SubResponseEntityTypeCases))]
        public void ResponseFieldDescriptorLoaderCorrectlyRemovesFieldsFromMetricsThatAreNotAllowed_WithSubResponseEntityType(IEnumerable<EntityType> responseEntityTypes,
            List<ResponseFieldDescriptor> expectedResponseFieldDescriptors)
        {
            var realResponseFieldDescriptorLoader = Arrange();
            var fields = Act(responseEntityTypes, realResponseFieldDescriptorLoader);
            Assert(expectedResponseFieldDescriptors, fields);
        }

        private static ResponseFieldDescriptorLoader Arrange()
        {
            var fakeResponseFieldManager = Substitute.For<IResponseFieldManager>();
            var fullFieldList = FullAllowedList.Concat(ForbiddenList).ToList();

            fakeResponseFieldManager.GetOrAddFieldsForEntityType(Arg.Any<IEnumerable<EntityType>>(), Arg.Any<string>())
                .Returns(info => fullFieldList.Where(f => f.EntityCombination.IsEquivalent(info.ArgAt<IEnumerable<EntityType>>(0))).ToList());

            var fakeClaimRestrictedMetricRepo = Substitute.For<IClaimRestrictedMetricRepository>();

            var measures = FieldAllowedList.Select(f => new Measure { Field = f, BaseField = BaseFieldAllowedList.FirstOrDefault(b => b.Name.StartsWith(f.Name)) ?? new ResponseFieldDescriptor(string.Empty) }).ToList();
            fakeClaimRestrictedMetricRepo.GetAllowed(MockRepositoryData.UkSubset)
                .Returns(measures);

            return new ResponseFieldDescriptorLoader(fakeClaimRestrictedMetricRepo, fakeResponseFieldManager);
        }

        private static List<ResponseFieldDescriptor> Act(IEnumerable<EntityType> entityTypes,
            IResponseFieldDescriptorLoader realResponseFieldDescriptorLoader)
        {
            return realResponseFieldDescriptorLoader.GetFieldDescriptors(MockRepositoryData.UkSubset, entityTypes, false)
                .ToList();
        }

        private static void Assert(IReadOnlyCollection<ResponseFieldDescriptor> expectedResponseFieldDescriptors, IReadOnlyCollection<ResponseFieldDescriptor> fields)
        {
            NUnit.Framework.Assert.That(fields.Count, Is.EqualTo(expectedResponseFieldDescriptors.Count));
            NUnit.Framework.Assert.That(fields, Is.EquivalentTo(expectedResponseFieldDescriptors));
        }

        private static ResponseFieldDescriptor CreateResponseDescriptor(string name, params EntityType[] entityTypes)
        {
            var responseFieldDescriptor = new ResponseFieldDescriptor(name, entityTypes);
            var fieldDefinitionModel = new FieldDefinitionModel("", "", "", "", "", 1f, "", EntityInstanceColumnLocation.Unknown, "", false, null, Enumerable.Empty<EntityFieldDefinitionModel>(), null);
            responseFieldDescriptor.AddDataAccessModelForSubset(MockRepositoryData.UkSubset.Id, fieldDefinitionModel);
            return responseFieldDescriptor;
        }
    }
}
