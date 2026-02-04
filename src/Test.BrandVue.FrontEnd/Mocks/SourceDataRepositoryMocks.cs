using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd.Mocks
{
    public static class SourceDataRepositoryMocks
    {
        public static ISubsetRepository GetSubsetRepository()
        {
            ISubsetRepository subsetRepository = Substitute.For<ISubsetRepository>();
            subsetRepository.Get(Arg.Any<string>()).Returns(args =>
                MockRepositoryData.FullSubsetList.Single(sub => sub.Id == args.Arg<string>()));
            subsetRepository.HasSubset(Arg.Any<string>()).Returns(args =>
                MockRepositoryData.FullSubsetList.Any(sub => sub.Id == args.Arg<string>()));
            subsetRepository.TryGet(Arg.Any<string>(), out _).Returns(args =>
            {
                args[1] = MockRepositoryData.FullSubsetList.Single(sub => sub.Id == args.Arg<string>());
                return MockRepositoryData.FullSubsetList.Any(sub => sub.Id == args.Arg<string>());
            });
            subsetRepository.Count.Returns(args => MockRepositoryData.FullSubsetList.Count);
            subsetRepository.GetEnumerator().Returns(MockRepositoryData.FullSubsetList.GetEnumerator());
            return subsetRepository;
        }

        public static IAverageDescriptorRepository GetAverageDescriptorRepository()
        {
            var averageDescriptorRepository = Substitute.For<IAverageDescriptorRepository>();
            var sampleAverage = new AverageDescriptor {
                    AllowPartial = false,
                    AverageId = "Monthly",
                    AverageStrategy = AverageStrategy.OverAllPeriods,
                    Disabled = false,
                    DisplayName = "Monthly",
                    Environment = null,
                    Group = new[] { "Calendar", "CalendarShort" },
                    IncludeResponseIds = false,
                    InternalIndex = 0,
                    IsDefault = true,
                    IsHiddenFromUsers = false,
                    MakeUpTo = MakeUpTo.MonthEnd,
                    NumberOfPeriodsInAverage = 1,
                    Order = 100,
                    Roles = null,
                    Subset = null,
                    TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                    WeightAcross = WeightAcross.SinglePeriod,
                    WeightingMethod = WeightingMethod.QuotaCell,
                    WeightingPeriodUnit = WeightingPeriodUnit.SameAsTotalization,
            };
            averageDescriptorRepository.GetEnumerator()
                .Returns(MockRepositoryData.MockAverageRepositorySource().GetEnumerator());
            averageDescriptorRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(_ => sampleAverage);
            averageDescriptorRepository.TryGet(Arg.Any<string>(), out _).Returns((args) =>
            {
                args[1] = sampleAverage;
                return true;
            });
            averageDescriptorRepository.Count.Returns(MockRepositoryData.MockAverageRepositorySource().Count);


            return averageDescriptorRepository;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="variable">Set to include additional variable testing measures</param>
        /// <returns></returns>
        public static IMeasureRepository GetMeasureRepository(Variable<int?> variable = null, IEnumerable<Measure> measuresNotAvailableToCurrentUser = null)
        {
            var sampleMeasures = MockRepositoryData.CreateSampleMeasures();
            if (variable != null)
            {
                sampleMeasures = sampleMeasures.Union(MockRepositoryData.VariableTestingMeasures(variable));
            }
            var allMeasures = measuresNotAvailableToCurrentUser != null ? sampleMeasures.Union(measuresNotAvailableToCurrentUser) : sampleMeasures;
            var measureRepository = Substitute.For<IMeasureRepository>();
            measureRepository.GetAllMeasuresIncludingDisabledForSubset(Arg.Any<Subset>())
                .Returns(sampleMeasures);
            measureRepository.Get(Arg.Any<string>()).Returns((args) => sampleMeasures.First(m=>args.Arg<string>() == m.Name));
            measureRepository.GetAll().Returns(_ => allMeasures);
            measureRepository.GetAllForCurrentUser().Returns(_ => sampleMeasures);
            measureRepository.GetMany(Arg.Any<string[]>()).Returns((args) => sampleMeasures.Where(m => args.Arg<string[]>().Contains(m.Name)));
            measureRepository.GetMeasuresByVariableConfigurationIds(Arg.Any<List<int>>()).Returns(sampleMeasures.Where(measure => measure.VariableConfigurationId.HasValue));
            return measureRepository;
        }

        public static IEntityRepository GetEntityRepository()
        {
            var entityRepository = Substitute.For<IEntityRepository>();
            entityRepository.GetInstancesOf(EntityType.Brand, MockRepositoryData.UkSubset)
                .Returns(MockRepositoryData.CreateBrands());
            entityRepository.GetInstancesOf(EntityType.Product, MockRepositoryData.UkSubset)
                .Returns(MockRepositoryData.CreateProducts());
            entityRepository.GetInstances(EntityType.Brand, Arg.Any<IEnumerable<int>>(), Arg.Any<Subset>())
                .Returns(args =>MockRepositoryData.CreateBrands().Where(x=> args.Arg<IEnumerable<int>>().Contains(x.Id)));
            entityRepository.GetInstances(EntityType.Product, Arg.Any<IEnumerable<int>>(), Arg.Any<Subset>())
                .Returns(args => MockRepositoryData.CreateProducts().Where(x => args.Arg<IEnumerable<int>>().Contains(x.Id)));
            return entityRepository;
        }

        public static IRespondentRepositorySource GetRespondentRepository()
        {
            var respondentRepository = Substitute.For<IRespondentRepositorySource>();
            respondentRepository.GetForSubset(Arg.Any<Subset>())
                .Returns(args => MockRepositoryData.GetRespondentRepositoryValues(args.Arg<Subset>(), 1)[args.Arg<Subset>()]);
            return respondentRepository;
        }

        public static IResponseEntityTypeRepository GetResponseEntityTypeRepository()
        {
            return new TestEntityTypeRepository();

        }

        public static IProductContext GetProductContext()
        {
            var productContext = Substitute.For<IProductContext>();
            productContext.ShortCode.Returns("survey");
            productContext.GenerateFromSurveyIds.Returns(true);
            return productContext;
        }

        public static IBaseExpressionGenerator GetBaseExpressionGenerator()
        {
            var baseExpressionGenerator = Substitute.For<IBaseExpressionGenerator>();
            baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(Arg.Any<Measure>(), Arg.Any<BaseExpressionDefinition>()).Returns(args=>args.Arg<Measure>());
            return baseExpressionGenerator;
        }

        public static IPagesRepository GetPagesRepository()
        {
            var pagesRepository = Substitute.For<IPagesRepository>();
            pagesRepository.GetPages().Returns(new List<PageDescriptor>
            {
                new()
                {
                    Id = 1,
                    DisplayName = "Net Buzz",
                    Name = "Net Buzz",
                    PageType = "MinorPage",
                    MinUserLevel = 100,
                    PageTitle = "Buzz",
                }
            });
            return pagesRepository;
        }

        public static IPanesRepository GetPanesRepository()
        {
            var panesRepository = Substitute.For<IPanesRepository>();
            panesRepository.GetPanes().Returns(new List<PaneDescriptor>
            {
                new()
                {
                    Id = "Net Buzz_1",
                    PageName = "Net Buzz",
                    PaneType = "Standard",
                    View = 1
                }
            });
            return panesRepository;
        }

        public static IPartsRepository GetPartsRepository()
        {
            var partsRepository = Substitute.For<IPartsRepository>();
            partsRepository.GetParts().Returns(new List<PartDescriptor>
            {
                new()
                {
                    PaneId = "Net Buzz_1",
                    Spec1 = "Net Buzz",
                    PartType = "BoxChartTall",
                }
            });
            return partsRepository;
        }
    }
}
