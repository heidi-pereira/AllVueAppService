using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Variable;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.SourceData.Configuration;

internal class VariableConfigurationRepositoryTests
{
    private const string RequestScopeProductName = "brandvue";
    private static readonly ProductContext ProductContext = new(RequestScopeProductName);

    private ITestMetadataContextFactory _testMetadataContextFactory;
    private VariableConfigurationRepository _repo;
    private VariableConfiguration[] _deps;

    private int _nextId;

    [SetUp]
    public async Task SetUp()
    {
        _nextId = 1; // Reset ID counter for each test
        _testMetadataContextFactory = await ITestMetadataContextFactory.CreateAsync(StorageType.InMemory);
        _repo = NewRepo();
        _repo.GetAll(); // Ensure dodgy cache is initialized
        _deps =
        [
            VariableWithDependencies([]),
            VariableWithDependencies([]),
            VariableWithDependencies([])
        ];
        _deps.ToList().ForEach(v => _repo.Create(v, null));
    }


    [TearDown]
    public async Task TearDown() => await _testMetadataContextFactory.RevertDatabase();

    [Test]
    public void CreateVariableWithDependencies_SavesDependenciesById()
    {
        var variable = VariableWithDependencies([_deps[0].Id, _deps[1].Id]);

        _repo.Create(variable, null);

        AssertGetDependenciesEquivalent(variable, [_deps[0].Id, _deps[1].Id]);
    }

    [Test]
    public void CreateVariableWithDependencies_SavesDependenciesByIdentifierOverrides()
    {
        var variable = VariableWithDependencies([]);

        _repo.Create(variable, [_deps[0].Identifier, _deps[1].Identifier]);

        AssertGetDependenciesEquivalent(variable, [_deps[0].Id, _deps[1].Id]);
    }

    [Test]
    public void UpdateVariableDependencies_SyncsDependencies()
    {

        var variable = VariableWithDependencies([_deps[0].Id, _deps[1].Id]);
        _repo.Create(variable, null);

        // Remove var1, add var3
        var updated = VariableWithDependencies([_deps[1].Id, _deps[2].Id], variable);

        _repo.Update(updated);

        AssertGetDependenciesEquivalent(variable, [_deps[1].Id, _deps[2].Id]);
    }

    [Test]
    public void RemoveAllDependencies_LeavesNone()
    {
        var variable = VariableWithDependencies([_deps[0].Id, _deps[1].Id]);
        _repo.Create(variable, null);

        var updated = VariableWithDependencies([], variable);
        _repo.Update(updated);

        AssertGetDependenciesEquivalent(variable, []);
    }

    [Test]
    public void Create_EvaluatableVariableDefinition_ShouldPopulateCachedPythonExpression()
    {
        // Test that creating a variable with an evaluatable definition populates the cached expression
        var variable = new VariableConfiguration
        {
            Id = _nextId++,
            ProductShortCode = RequestScopeProductName,
            Identifier = "fieldExpressionVar",
            DisplayName = "Field Expression Variable",
            Definition = new FieldExpressionVariableDefinition
            {
                Expression = "any(response.Location(location=result.location))"
            }
        };

        var createdVariable = _repo.Create(variable, null);

        var evaluatableDefinition = createdVariable.Definition as EvaluatableVariableDefinition;
        Assert.That(evaluatableDefinition, Is.Not.Null);
        Assert.That(evaluatableDefinition.CachedPythonExpression, Is.Not.Null.And.Not.Empty);
        Assert.That(evaluatableDefinition.CachedPythonExpression, Contains.Substring("or None"));
    }

    [Test]
    public void Create_QuestionVariableDefinition_ShouldNotHaveCachedPythonExpression()
    {
        // Test that creating a QuestionVariableDefinition does not try to populate cached expression
        var variable = new VariableConfiguration
        {
            Id = _nextId++,
            ProductShortCode = RequestScopeProductName,
            Identifier = "questionVar",
            DisplayName = "Question Variable", 
            Definition = new QuestionVariableDefinition
            {
                QuestionVarCode = "Q1",
                EntityTypeNames = new List<(string, string)> { ("column1", "EntityType1") }
            }
        };

        var createdVariable = _repo.Create(variable, null);

        Assert.That(createdVariable.Definition, Is.InstanceOf<QuestionVariableDefinition>());
        Assert.That(createdVariable.Definition, Is.Not.InstanceOf<EvaluatableVariableDefinition>());
    }

    [Test]
    public void Update_EvaluatableVariableDefinition_ShouldUpdateCachedPythonExpression()
    {
        // Create a variable with an initial expression
        var variable = new VariableConfiguration
        {
            Id = _nextId++,
            ProductShortCode = RequestScopeProductName,
            Identifier = "updateableVar",
            DisplayName = "Updateable Variable",
            Definition = new FieldExpressionVariableDefinition
            {
                Expression = "1"
            }
        };

        var createdVariable = _repo.Create(variable, null);
        var initialCachedExpression = ((EvaluatableVariableDefinition)createdVariable.Definition).CachedPythonExpression;

        // Update the variable with a new expression
        var updatedVariable = createdVariable with
        {
            Definition = new FieldExpressionVariableDefinition
            {
                Expression = "any(response.NewField(field=result.field))"
            }
        };

        _repo.Update(updatedVariable);

        var loadedVariable = _repo.Get(updatedVariable.Id);
        var updatedCachedExpression = ((EvaluatableVariableDefinition)loadedVariable.Definition).CachedPythonExpression;

        Assert.That(updatedCachedExpression, Is.Not.EqualTo(initialCachedExpression));
        Assert.That(updatedCachedExpression, Contains.Substring("NewField"));
        Assert.That(updatedCachedExpression, Contains.Substring("or None"));
    }

    [Test]
    public void Create_GroupedVariableDefinition_ShouldPopulateCachedPythonExpression()
    {
        // Test that creating a GroupedVariableDefinition populates the cached expression
        var variable = new VariableConfiguration
        {
            Id = _nextId++,
            ProductShortCode = RequestScopeProductName,
            Identifier = "groupedVar",
            DisplayName = "Grouped Variable",
            Definition = new GroupedVariableDefinition
            {
                ToEntityTypeName = "TestEntity",
                Groups = new List<VariableGrouping>
                {
                    new()
                    {
                        ToEntityInstanceId = 1,
                        ToEntityInstanceName = "Group1",
                        Component = new InclusiveRangeVariableComponent
                        {
                            Min = 1,
                            Max = 10,
                            Operator = VariableRangeComparisonOperator.Between,
                            FromVariableIdentifier = "Age"
                        }
                    }
                }
            }
        };

        var createdVariable = _repo.Create(variable, null);

        var evaluatableDefinition = createdVariable.Definition as EvaluatableVariableDefinition;
        Assert.That(evaluatableDefinition, Is.Not.Null);
        Assert.That(evaluatableDefinition.CachedPythonExpression, Is.Not.Null.And.Not.Empty);
        Assert.That(evaluatableDefinition.CachedPythonExpression, Contains.Substring("TestEntity"));
    }

    [Test]
    public void Create_SingleGroupVariableDefinition_ShouldPopulateCachedPythonExpression()
    {
        // Test that creating a SingleGroupVariableDefinition populates the cached expression
        var variable = new VariableConfiguration
        {
            Id = _nextId++,
            ProductShortCode = RequestScopeProductName,
            Identifier = "singleGroupVar",
            DisplayName = "Single Group Variable",
            Definition = new SingleGroupVariableDefinition
            {
                Group = new VariableGrouping
                {
                    ToEntityInstanceId = 1,
                    ToEntityInstanceName = "SingleGroup",
                    Component = new InclusiveRangeVariableComponent
                    {
                        Min = 1,
                        Max = 100,
                        Operator = VariableRangeComparisonOperator.Between,
                        FromVariableIdentifier = "Score"
                    }
                },
                AggregationType = AggregationType.MaxOfSingleReferenced
            }
        };

        var createdVariable = _repo.Create(variable, null);

        var evaluatableDefinition = createdVariable.Definition as EvaluatableVariableDefinition;
        Assert.That(evaluatableDefinition, Is.Not.Null);
        Assert.That(evaluatableDefinition.CachedPythonExpression, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Create_InvalidExpressionVariableDefinition_ShouldSetCachedExpressionToNull()
    {
        // Test that when GetPythonExpression fails, the cached expression is set to null
        var variable = new VariableConfiguration
        {
            Id = _nextId++,
            ProductShortCode = RequestScopeProductName,
            Identifier = "invalidExpressionVar",
            DisplayName = "Invalid Expression Variable",
            Definition = new FieldExpressionVariableDefinition
            {
                Expression = "" // Empty expression that might cause GetPythonExpression to fail
            }
        };

        // This should not throw an exception, even if GetPythonExpression fails
        var createdVariable = _repo.Create(variable, null);

        Assert.That(createdVariable, Is.Not.Null);
        Assert.That(createdVariable.Definition, Is.InstanceOf<EvaluatableVariableDefinition>());
    }

    private VariableConfigurationRepository NewRepo() => new VariableConfigurationRepository(_testMetadataContextFactory, ProductContext);

    private VariableConfiguration VariableWithDependencies(int[] depIds, VariableConfiguration idToCopy = null)
    {
        var id = idToCopy?.Id ?? _nextId++;
        return new VariableConfiguration
        {
            Id = id,
            ProductShortCode = RequestScopeProductName,
            Identifier = "var" + id,
            DisplayName = "Var" + id,
            VariableDependencies = depIds.Select(depId => new VariableDependency { VariableId = id, DependentUponVariableId = depId }).ToList()
        };
    }

    private void AssertGetDependenciesEquivalent(VariableConfiguration variable, IEnumerable<int> expected)
    {
        Assert.Multiple(() =>
        {
            var loadedFromCache = _repo.Get(variable.Id);
            Assert.That(loadedFromCache.VariableDependencies.Select(d => d.DependentUponVariableId), Is.EquivalentTo(expected), "Loaded from cache does not match expected dependencies");

            var loadedFresh = NewRepo().Get(variable.Id);
            Assert.That(loadedFresh.VariableDependencies.Select(d => d.DependentUponVariableId), Is.EquivalentTo(expected), "Loaded from fresh repository does not match expected dependencies");
        });
        
    }

}
