namespace BrandVue.SourceData.Measures
{
    public interface IMeasureRepository
    {
        Measure Get(string metricName);
        IEnumerable<Measure> GetMany(string[] metricNames);
        IEnumerable<Measure> GetAll();
        IEnumerable<Measure> GetAllForCurrentUser();
        bool TryGet(string metricName, out Measure stored);
        IEnumerable<Measure> GetAllMeasuresWithDisabledPropertyFalseForSubset(Subset selectedSubset);
        IEnumerable<Measure> GetAllMeasuresIncludingDisabledForSubset(Subset selectedSubset);
        IEnumerable<Measure> GetMeasuresByVariableConfigurationIds(List<int> variableIds);
        void RenameMeasure(Measure measure, string newName);
    }
}