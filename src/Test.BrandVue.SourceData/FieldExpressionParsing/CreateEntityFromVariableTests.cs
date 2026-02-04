using System.Linq;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using NUnit.Framework;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData.FieldExpressionParsing;

class CreateEntityFromVariableTests : ExpressionTestBase
{

    [Test]
    public void EntityInstancesUpdatedWhenRedeclared()
    {
        string fieldName = "age";
        string variableEntityName = "newEntity";
        _responseFieldManager.Add(fieldName, Subset.Id, true);

        var firstBuilder = new GroupedVariableDefinitionBuilder(variableEntityName);
        firstBuilder.WithGreaterThanGroup("OnlyInOne", 1, fieldName);
        firstBuilder.WithGreaterThanGroup("InBoth", 2, fieldName);
        AddVariableConfig(firstBuilder.Build());

        var secondBuilder = new GroupedVariableDefinitionBuilder(variableEntityName);
        secondBuilder.WithGreaterThanGroup("OnlyInOne", 1, fieldName);
        secondBuilder.WithGreaterThanGroup("InBoth", 2, fieldName);
        secondBuilder.WithGreaterThanGroup("OnlyInTwo", 3, fieldName);
        var groupedVariableDefinition = secondBuilder.Build();
        groupedVariableDefinition.Groups.RemoveAt(0); //To make the ids match we add extras then remove one, as the user would have
        AddVariableConfig(groupedVariableDefinition);

        var instanceNames = _entityInstanceRepository.GetInstancesOf(variableEntityName, Subset).Select(x => x.Name);
        Assert.That(instanceNames, Is.EquivalentTo(new[]{"OnlyInTwo", "InBoth"}));
    }

    private void AddVariableConfig(GroupedVariableDefinition definition, string id = "aVariable") => AddVariable(new VariableConfiguration { Definition = definition, Identifier = id });
}