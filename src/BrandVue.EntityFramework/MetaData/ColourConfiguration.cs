using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData
{
    public class ColourConfiguration
    {
        [Required, MaxLength(20)]
        public string ProductShortCode { get; set; }
        [Required, MaxLength(50)]
        public string Organisation { get; set; }
        [Required, MaxLength(40)]
        public string EntityType { get; set; }
        [Required]
        public int EntityInstanceId { get; set; }
        [Required, MaxLength(7)]
        public string Colour { get; set; }
    }
}