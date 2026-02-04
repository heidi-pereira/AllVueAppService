using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData
{
    public class Bookmark
    {
        public string Id { get; set; }
        [Required]
        [MaxLength(1600)]
        public string Url { get; set; }
        [Required]
        public DateTime DateCreated { get; set; }
        [Required]
        public DateTime DateLastGenerated { get; set; }

        public DateTime? DateLastUsed { get; set; }
        [Required]
        public long GenerationCount { get; set; }
        [Required]
        public long UseCount { get; set; }
        [Required]
        public string CreatedByUserName { get; set; }
        [Required]
        public string CreatedByIpAddress { get; set; }
        [Required]
        [MaxLength(100)]
        public string AppBase { get; set; }
    }
}