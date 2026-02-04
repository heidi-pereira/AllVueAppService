using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using NSubstitute;
using TestCommon.Extensions;
using Vue.Common.Auth;

namespace Test.BrandVue.SourceData.AnswersMetadata;

public class QuestionTypeLookupRepositoryTests
{
    private static readonly Subset Subset = new Subset { Id = "TestSubset" };
    private static readonly ISubsetRepository SubsetRepository = new SubsetRepository { Subset };
    private static IMeasureRepository _measureRepository;
    private QuestionTypeLookupRepository _questionTypeLookupRepository;

    public static IEnumerable<TestCaseData> MeasureTestCases()
    {
        var responseFieldManager =
            new ResponseFieldManager(EntityTypeRepository.GetDefaultEntityTypeRepository());
        var responseFieldDescriptor = responseFieldManager.Add("A field", subset: Subset);
        var variableWithOneDependency = new FilteredVariable(new CachedInMemoryFieldVariableInstance(responseFieldDescriptor),
            _ => true);
        _measureRepository = CreateMeasureRepository(new Measure
        {
            Name = "legacyFieldMeasure",
            Field = responseFieldDescriptor,
            LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 0, Maximum = 3 }
        }, new Measure
        {
            Name = "zeroFieldMeasure",
            PrimaryVariable = new DataWaveProfileVariable()
        }, new Measure
        {
            Name = "zeroFieldCustomVariableMeasure",
            PrimaryVariable = new DataWaveProfileVariable(),
            VariableConfigurationId = 1,
            GenerationType = AutoGenerationType.CreatedFromField
        }, new Measure
        {
            Name = "oneFieldDependencyMeasure",
            PrimaryVariable = variableWithOneDependency
        }, new Measure
        {
            Name = "oneFieldDependencyVariableMeasure",
            VariableConfigurationId = 1,
            PrimaryVariable = variableWithOneDependency
        });

        return _measureRepository.GetAllForCurrentUser().Select(m => new TestCaseData(m){TestName = m.Name});
    }

    [SetUp]
    public void Setup() => _questionTypeLookupRepository = new QuestionTypeLookupRepository(_measureRepository, SubsetRepository);

    [Test]
    public void GetForSubset_ShouldReturnSubsetLookup()
    {
        var subsetLookup = _questionTypeLookupRepository.GetForSubset(Subset);

        Assert.That(subsetLookup, Is.Not.Null);
    }

    [TestCaseSource(nameof(MeasureTestCases))]
    public void AddOrUpdate_ShouldAddOrUpdateMeasureToSubsetLookup(Measure measure)
    {
        _questionTypeLookupRepository.AddOrUpdate(measure);

        var subsetLookup = _questionTypeLookupRepository.GetForSubset(Subset);
        Assert.That(subsetLookup.ContainsKey(measure.Name), Is.True);
    }

    [TestCaseSource(nameof(MeasureTestCases))]
    public void Remove_ShouldRemoveMeasureFromSubsetLookup(Measure measure)
    {
        _questionTypeLookupRepository.AddOrUpdate(measure);

        _questionTypeLookupRepository.Remove(measure);

        var subsetLookup = _questionTypeLookupRepository.GetForSubset(Subset);
        Assert.That(subsetLookup.ContainsKey(measure.Name), Is.False);
    }

    private static MetricRepository CreateMeasureRepository(params Measure[] measures)
    {
        var userPermissionsService = Substitute.For<IUserDataPermissionsOrchestrator>();
        var measureRepository = new MetricRepository(userPermissionsService);
        foreach (var measure in measures)
        {
            measureRepository.TryAdd(measure.Name, measure);
            if (measure.Subset?.Any() != true)
            {
                var allSubsetsMeasure = measure.ShallowCopy();
                allSubsetsMeasure.SetSubsets(SubsetRepository.Select(x=>x.Id), SubsetRepository);
                allSubsetsMeasure.Name += " with subset";
                measureRepository.TryAdd(allSubsetsMeasure.Name, allSubsetsMeasure);
            }

        }
        return measureRepository;
    }
}
