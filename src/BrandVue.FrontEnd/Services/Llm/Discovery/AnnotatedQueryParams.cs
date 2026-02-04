using BrandVue.Models;

namespace BrandVue.Services.Llm.Discovery
{
    public class AnnotatedQueryParams
    {
        public QueryParams QueryParams { get; init; }
        public string PageName { get; init; }
        public string PartType { get; set; }
        public string? MessageToUser { get; init; }
        
    }
}