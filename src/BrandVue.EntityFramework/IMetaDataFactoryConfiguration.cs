namespace BrandVue.EntityFramework
{
    public interface IMetaDataFactoryConfiguration
    {
        string MetaConnectionString { get; }
        bool IsAppOnDeploymentBranch { get; }
    }
}