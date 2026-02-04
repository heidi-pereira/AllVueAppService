using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.ResponseRepository
{
    public class RawTextResponse
    {
        [Key]
        public string Text { get; set; }
    }
}
