using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.SourceData.Import;

public interface IVariableEntityLoader
{
    void CreateOrUpdateEntityForVariable(VariableConfiguration variableConfig);
    void DeleteEntityForVariable(VariableConfiguration variableConfig);
}