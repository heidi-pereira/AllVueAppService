using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Middleware;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Test.BrandVue.FrontEnd.Mocks;
using TestCommon.Extensions;
using Vue.Common.Auth.Permissions;

namespace Test.BrandVue.FrontEnd.Service
{
    [TestFixture]
    public class RequestAdapterTests
    {
        private const string PrimaryTestMeasure = "net-buzz";
        private const string PrimaryTestMeasureName = "Net Buzz";
        private const string TestSubset = "UKSubset";

        [Test]
        public void RequestAdapterCreatesParametersForCuratedResultsModel()
        {
            var measureRepository = SourceDataRepositoryMocks.GetMeasureRepository();
            var requestAdapter = BasicRequestAdapterMock(measureRepository);
            var entityInstances = MockRepositoryData.CreateBrands().ToList();
            var measure = measureRepository.Get(PrimaryTestMeasureName);
            var model = CreateModel(measure, entityInstances);

            var parameters = requestAdapter.CreateParametersForCalculation(
                model,
                new CompositeFilterModel());
            Assert.That(entityInstances.Select(x => x.Id), Is.EqualTo(parameters.EntityInstances.Select(x => x.Id)));
            Assert.That(measure.Name, Is.EqualTo(parameters.Measures.First().Name));
            Assert.That(parameters.QuotaCells.Cells, Is.Not.Empty);
        }

        //<summary>
        //regression 60196
        //</summary>
        [Test]
        public void RequestAdapterThrowsArugmentExceptionWhenPrimaryMeasureHasMultipleEntityCombinations()
        {
            var measureRepository = SourceDataRepositoryMocks.GetMeasureRepository();
            var subsetRepository = SourceDataRepositoryMocks.GetSubsetRepository();
            var requestAdapter = BasicRequestAdapterMock(measureRepository, subsetRepository);
            var field = MockRepositoryData.AddDataAccessModelForSubset(
                new ResponseFieldDescriptor(PrimaryTestMeasureName, TestEntityTypeRepository.Brand,
                    TestEntityTypeRepository.Product), subsetRepository.Get(TestSubset));
            measureRepository.Get(PrimaryTestMeasureName).Returns(x => new Measure
            {
                UrlSafeName = PrimaryTestMeasure,
                Name = PrimaryTestMeasureName,
                Field = field,
                StartDate = new DateTime(2017, 6, 30, 0, 0, 0),
                BaseField = field,
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                LegacyBaseValues = { Values = new[] { 1, 2, 3, 4 } },
            });
            var entityInstances = MockRepositoryData.CreateBrands().ToList();
            var measure = measureRepository.Get(PrimaryTestMeasureName);
            var model = CreateModel(measure, entityInstances);

            Assert.Throws<ArgumentException>(() =>
            {
                _ = requestAdapter.CreateParametersForCalculation(
                    model,
                    new CompositeFilterModel());
            });
        }

        [Test]
        public void CreateParametersForCalculation_ReturnsParametersWithNoFilters_WhenCurrentUserHasNoDataGroup()
        {
            var measureRepository = SourceDataRepositoryMocks.GetMeasureRepository();
            var requestAdapter = BasicRequestAdapterMock(measureRepository);
            var entityInstances = MockRepositoryData.CreateBrands().ToList();
            var measure = measureRepository.Get(PrimaryTestMeasureName);
            var model = CreateModel(measure, entityInstances);

            var parameters = requestAdapter.CreateParametersForCalculation(
                model,
                new CompositeFilterModel());
            Assert.That(parameters.FilterModel.Filters, Is.Empty);
        }

        [Test]
        public void
            CreateParametersForCalculation_ReturnsParametersWithEmptyFilters_WhenCurrentUserHasDataGroupWithNoFilters()
        {
            var measureRepository = SourceDataRepositoryMocks.GetMeasureRepository();
            measureRepository.GetMeasuresByVariableConfigurationIds(Arg.Any<List<int>>())
                .Returns(CreateMockMeasuresWithVariableConfigurationIds(2));
            var userDataPermissionsService = CreateUserDataPermissionsService(0);

            var requestAdapter = BasicRequestAdapterMock(measureRepository,
                userDataPermissionsService: userDataPermissionsService);
            var entityInstances = MockRepositoryData.CreateBrands().ToList();
            var measure = measureRepository.Get(PrimaryTestMeasureName);
            var model = CreateModel(measure, entityInstances);

            var parameters = requestAdapter.CreateParametersForCalculation(
                model,
                new CompositeFilterModel());
            Assert.That(parameters.FilterModel.CompositeFilters, Is.Empty);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public void
            CreateParametersForCalculation_ReturnsParametersWithFilters_WhenCurrentUserHasDataGroupWithFilters(
                int filterCount)
        {
            var measureRepository = SourceDataRepositoryMocks.GetMeasureRepository();
            measureRepository.GetMeasuresByVariableConfigurationIds(Arg.Any<List<int>>())
                .Returns(CreateMockMeasuresWithVariableConfigurationIds(filterCount));
            var userDataPermissionsService = CreateUserDataPermissionsService(filterCount);

            var requestAdapter = BasicRequestAdapterMock(measureRepository,
                userDataPermissionsService: userDataPermissionsService);
            var entityInstances = MockRepositoryData.CreateBrands().ToList();
            var measure = measureRepository.Get(PrimaryTestMeasureName);
            var model = CreateModel(measure, entityInstances);

            var parameters = requestAdapter.CreateParametersForCalculation(
                model,
                new CompositeFilterModel());
            Assert.That(parameters.FilterModel.Filters.Count, Is.EqualTo(filterCount));
        }

        [Test]
        public void
            CreateParametersForCalculation_ReturnsParametersWithFiltersAdded_WhenCurrentUserHasDataGroupWithFiltersAndRequestHasFilters()
        {
            var measureRepository = SourceDataRepositoryMocks.GetMeasureRepository();
            measureRepository.GetMeasuresByVariableConfigurationIds(Arg.Any<List<int>>())
                .Returns(CreateMockMeasuresWithVariableConfigurationIds(2));
            var userDataPermissionsService = CreateUserDataPermissionsService(2);

            var requestAdapter = BasicRequestAdapterMock(measureRepository,
                userDataPermissionsService: userDataPermissionsService);
            var entityInstances = MockRepositoryData.CreateBrands().ToList();
            var measure = measureRepository.Get(PrimaryTestMeasureName);
            var compositeFilterModel = CreateCompositeFilterModel();
            var model = CreateModel(measure, entityInstances);

            var parameters = requestAdapter.CreateParametersForCalculation(
                model, 
                compositeFilterModel);
            Assert.That(parameters.FilterModel.CompositeFilters.Count, Is.EqualTo(1));
            Assert.That(parameters.FilterModel.CompositeFilters.Last(), Is.EqualTo(compositeFilterModel),
                $"{parameters.FilterModel.CompositeFilters.Last().Name}");
        }

        private IEnumerable<Measure> CreateMockMeasuresWithVariableConfigurationIds(int howMany)
        {
            for (var i = 1; i <= howMany; i++)
            {
                yield return new Measure
                {
                    UrlSafeName = $"measure-{i}",
                    Name = $"Measure {i}",
                    VariableConfigurationId = i,
                    BaseField = new ResponseFieldDescriptor("test",
                        new EntityType($"EntityCombination-{i}", "Singular", "Plural"))
                };
            }
        }

        private DataPermissionDto CreateDataPermissionDtoWithFilters(int filterCount)
        {
            var filters = new List<DataPermissionFilterDto>();
            for (var i = 0; i < filterCount; i++)
            {
                filters.Add(new DataPermissionFilterDto(i + 1, new List<int> { 1, 2 }));
            }

            return new DataPermissionDto("Data Group", [], filters);
        }

        private IUserDataPermissionsService CreateUserDataPermissionsService(int filterCount)
        {
            var userDataPermissionsService = Substitute.For<IUserDataPermissionsService>();
            userDataPermissionsService.GetDataPermission().Returns(CreateDataPermissionDtoWithFilters(filterCount));
            return userDataPermissionsService;
        }

        private CuratedResultsModel CreateModel(Measure measure, List<EntityInstance> entityInstances)
        {
            SigDiffOptions sigOptions = new SigDiffOptions(
                false,
                SigConfidenceLevel.NinetyFive,
                DisplaySignificanceDifferences.ShowBoth,
                CrosstabSignificanceType.CompareToTotal);
            return new CuratedResultsModel(new DemographicFilter(new FilterRepository()), entityInstances.Select(x => x.Id).ToArray(),
                TestSubset, new[] { measure.Name }, new Period(), -1, new CompositeFilterModel(), sigOptions);
        }

        private CompositeFilterModel CreateCompositeFilterModel()
        {
            var instanceIds = new int[] { 1, 2, 3 };
            var filters = new List<MeasureFilterRequestModel>();
            filters.Add(new MeasureFilterRequestModel("measure1", new Dictionary<string, int[]>(){{"entity", instanceIds}}, false, false, instanceIds));
            return new CompositeFilterModel(FilterOperator.And, filters, new List<CompositeFilterModel>());
        }

        private RequestAdapter BasicRequestAdapterMock(
            IMeasureRepository measureRepository,
            ISubsetRepository subsetRepository = null,
            IUserDataPermissionsService userDataPermissionsService = null)
        {
            subsetRepository ??= SourceDataRepositoryMocks.GetSubsetRepository();
            if (userDataPermissionsService == null)
            {
                userDataPermissionsService = Substitute.For<IUserDataPermissionsService>();
                userDataPermissionsService.GetDataPermission().Returns(null as DataPermissionDto);
            }

            var averageDescriptorRepository = SourceDataRepositoryMocks.GetAverageDescriptorRepository();
            var entityRepository = SourceDataRepositoryMocks.GetEntityRepository();
            var demographicFilterToQuotaCellMapper = new DemographicFilterToQuotaCellMapper(SourceDataRepositoryMocks.GetRespondentRepository());
            var responseEntityTypeRepository = SourceDataRepositoryMocks.GetResponseEntityTypeRepository();
            var weightingPlanRepository = Substitute.For<IWeightingPlanRepository>();
            var productContext = SourceDataRepositoryMocks.GetProductContext();
            var baseExpressionGenerator = SourceDataRepositoryMocks.GetBaseExpressionGenerator();
            var filterRepository = Substitute.For<IFilterRepository>();

            return new RequestAdapter(subsetRepository, averageDescriptorRepository, measureRepository,
                entityRepository, demographicFilterToQuotaCellMapper,
                responseEntityTypeRepository, weightingPlanRepository, filterRepository, productContext, baseExpressionGenerator, new RequestScope(), 
                Substitute.For<IQuestionTypeLookupRepository>(), userDataPermissionsService);
        }
    }
}
