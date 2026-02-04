using System.Linq;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace TestCommon.DataPopulation;

public static class VariableConfigurationGenerator
{
    public static VariableConfiguration CreateInstanceListVariable(string variableIdentifier, params VariableComponent[] instanceLists)
    {
        var variableConfiguration = new VariableConfiguration
        {
            Definition = new GroupedVariableDefinition()
            {
                ToEntityTypeName = variableIdentifier,
                ToEntityTypeDisplayNamePlural = variableIdentifier + "s",
                Groups = instanceLists.Select((il, i) => new VariableGrouping()
                {
                    ToEntityInstanceId = i + 1, ToEntityInstanceName = (i + 1).ToString(), Component = il
                }).ToList()
            },
            Identifier = variableIdentifier,
            DisplayName = variableIdentifier
        };
        return variableConfiguration;
    }
}
