using BrandVue.EntityFramework.MetaData.Averages;

namespace BrandVue.SourceData.Averages;

public interface IAverageDescriptorSqlLoader
{
    void Load(AverageDescriptorMapFileLoader temporaryMapFileLoader, string fullyQualifiedPathToCsvDataFile);
    AverageDescriptor AverageDescriptorFrom(AverageConfiguration configuration);
    void PopulateAverageDescriptorFrom(AverageDescriptor toDescriptor, AverageConfiguration fromConfiguration);
    void AddOrUpdate(AverageConfiguration average);
    void Remove(AverageConfiguration average);
}