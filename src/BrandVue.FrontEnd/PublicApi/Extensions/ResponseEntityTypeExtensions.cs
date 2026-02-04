using BrandVue.SourceData.Entity;

namespace BrandVue.PublicApi.Extensions
{
    public static class ResponseEntityTypeExtensions
    {
        public static IOrderedEnumerable<string> ToOrderedEntityNames(this IEnumerable<EntityType> types) => 
            types.Select(f => f.Identifier).OrderBy(s => s);
    }
}
