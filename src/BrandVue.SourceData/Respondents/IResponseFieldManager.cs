namespace BrandVue.SourceData.Respondents
{
    public interface IResponseFieldManager
    {
        ICollection<ResponseFieldDescriptor> GetAllFields();
        List<ResponseFieldDescriptor> GetOrAddFieldsForEntityType(IEnumerable<EntityType> entityTypes, string subsetId);
        ResponseFieldDescriptor Get(string fieldName);
        bool TryGet(string fieldName, out ResponseFieldDescriptor field);
        void Load(string fullyQualifiedPathToJsonFile, string baseMetaPath);
    }
}