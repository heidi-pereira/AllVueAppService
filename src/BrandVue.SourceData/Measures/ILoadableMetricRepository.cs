namespace BrandVue.SourceData.Measures;

public interface ILoadableMetricRepository : IMeasureRepository, IAddableRepository<Measure, string>
{
    Measure Remove(string objectId);
}