using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BrandVue.Services.Llm.Discovery
{
    public class PagesByMetricsFunction : IDiscoveryFunctionToolInvocation
    {
       
        [Required]
        [Description("A list of metric names to get pages for.")]
        public string[] metrics { get; init; }

    }


    public class MeasuresNames : IDiscoveryFunctionToolInvocation
    {

        [Required]
        [Description("A list of metric names.")]
        public string[] metrics { get; init; }

    }
}
