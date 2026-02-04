namespace BrandVue.SourceData.AutoGeneration;

public class NumericFieldData
{
    private readonly FieldDefinitionModel _fieldDefinitionModel;
    private readonly List<string> _subsetIds = new ();
    private string _originalMetricName;
    private string _uniqueName;

    public NumericFieldData(FieldDefinitionModel fieldDefinitionModel, string initialSubsetId)
    {
        _fieldDefinitionModel = fieldDefinitionModel;
        _subsetIds.Add(initialSubsetId);
    }

    public void AddSubset(string subsetId)
    {
        _subsetIds.Add(subsetId);
    }

    public FieldDefinitionModel GetFieldDefinitionModel()
    {
        return _fieldDefinitionModel;
    }

    public string GetMetricConfigurationSubsetIdList()
    {
        return string.Join("|", _subsetIds);
    }
    
    public static string GetUniqueIdentifier(FieldDefinitionModel field)
    {
        return field.Name + field.FullV2VarCode + field.QuestionModel?.MasterType + field.QuestionModel?.MinimumValue +
               field.QuestionModel?.MaximumValue;
    }

    public void SetOriginalMetricName(string originalMetricName)
    {
        _originalMetricName = originalMetricName;
        _uniqueName = $"{AutoGenerationConstants.NumericIdentifier}: {_originalMetricName}";
    }

    public void IsDuplicate(int duplicatedCount)
    {
        _uniqueName += $"_{duplicatedCount}";
    }

    public string GetOriginalMetricName()
    {
        return _originalMetricName;
    }

    public string GetUniqueName()
    {
        return _uniqueName;
    }
}