namespace BrandVue.EntityFramework
{
    public record MetaDataFactoryConfiguration(string MetaConnectionString, bool IsAppOnDeploymentBranch) :
        IMetaDataFactoryConfiguration;
}