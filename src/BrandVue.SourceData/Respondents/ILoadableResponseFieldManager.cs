namespace BrandVue.SourceData.Respondents
{
    public interface ILoadableResponseFieldManager : IResponseFieldManager
    {
        void Load(params (string SubsetId, FieldDefinitionModel Model)[] models);
    }
}