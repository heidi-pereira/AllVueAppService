namespace BrandVue.Middleware
{
    /// <summary>
    /// The resource the user is trying to access. Documentation can be removed when we move to a new domain.
    /// </summary>
    public enum RequestResource
    {
        Ui, PublicApi, Documentation, InternalApi
    }
}