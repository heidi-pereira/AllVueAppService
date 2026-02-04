using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Import;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Import;

[TestFixture]
public class VariableLoaderTests
{
    private ILogger _logger;
    private IFieldExpressionParser _fieldExpressionParser;
    private VariableLoader _variableLoader;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger>();
        _fieldExpressionParser = Substitute.For<IFieldExpressionParser>();
        _variableLoader = new VariableLoader(_fieldExpressionParser, _logger);
    }

    [Test]
    public void ParsePythonExpressionVariablesInDependencyOrder_ShouldCallDeclareOrUpdateVariableInDependencyOrder()
    {
        var variableConfigurations = new List<VariableConfiguration>
        {
            new() { Id = 1, DisplayName = "Var1" }, new() { Id = 2, DisplayName = "Var2" }
        };
        variableConfigurations[0].VariableDependencies.Add(new (){Variable = variableConfigurations[0], DependentUponVariable = variableConfigurations[1] });

        _variableLoader.ParsePythonExpressionVariablesInDependencyOrder(variableConfigurations);

        Received.InOrder(() =>
        {
            _fieldExpressionParser.DeclareOrUpdateVariable(variableConfigurations[1]);
            _fieldExpressionParser.DeclareOrUpdateVariable(variableConfigurations[0]);
        });
    }

    [Test]
    public void ParsePythonExpressionVariablesInDependencyOrder_ShouldLogWarningOnException()
    {
        var variableConfigurations = new List<VariableConfiguration>
        {
            new() { Id = 1, DisplayName = "Var1" }
        };
        _fieldExpressionParser.When(x => x.DeclareOrUpdateVariable(Arg.Any<VariableConfiguration>()))
            .Do(_ => throw new Exception("Test exception"));

        _variableLoader.ParsePythonExpressionVariablesInDependencyOrder(variableConfigurations);

        AssertLoggerCalled(_logger);
    }

    [Test]
    public void GetTransitiveDependencies_ShouldReturnNullOnCyclicDependencies()
    {
        var variable = new VariableConfiguration { Identifier = "Var1", };
        variable.VariableDependencies.Add(new() { Variable = variable, DependentUponVariable = variable });

        _variableLoader.ParsePythonExpressionVariablesInDependencyOrder([variable]);

        _fieldExpressionParser.Received(0).DeclareOrUpdateVariable(Arg.Any<VariableConfiguration>());
        AssertLoggerCalled(_logger);
    }

    private void AssertLoggerCalled(ILogger logger, int numCalls = 1) => Assert.That(
        logger.ReceivedCalls().Select(c => c.GetMethodInfo().Name),
        Is.EquivalentTo(Enumerable.Repeat(nameof(ILogger.Log), numCalls))
    );
}