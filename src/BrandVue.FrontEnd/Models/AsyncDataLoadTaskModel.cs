using System.ComponentModel.DataAnnotations;

namespace BrandVue.Models
{
    public class AsyncDataLoadTaskModel
    {
        [Required]
        public string TaskKey { get; set; }
    }
}
