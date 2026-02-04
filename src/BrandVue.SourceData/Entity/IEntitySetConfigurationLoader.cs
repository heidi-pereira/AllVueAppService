using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Entity;

public interface IEntitySetConfigurationLoader
{
    void AddOrUpdateAll();
    void AddOrUpdate(EntitySetConfiguration entitySetConfiguration);
    void Remove(EntitySetConfiguration entitySetConfiguration);
}