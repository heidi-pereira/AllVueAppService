using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData
{
    [Table("PageAbout")]
    public class PageAbout
    {
        public int Id { get; set; }
        [MaxLength(20)]
        public string ProductShortCode { get; set; }
        public int PageId { get; set; }
        [MaxLength(200)]
        public string AboutTitle { get; set; }
        [MaxLength(1000)]
        public string AboutContent { get; set; }
        [MaxLength(200)]
        [CanBeNull]
        public string User { get; set; }
        public bool Editable { get; set; }
    }

    public class PageAboutConfiguration : IEntityTypeConfiguration<PageAbout>
    {
        public void Configure(EntityTypeBuilder<PageAbout> builder)
        {
            builder.Property(p => p.AboutTitle).IsRequired();
            builder.Property(p => p.AboutContent).IsRequired();
            builder.Property(p => p.ProductShortCode).IsRequired();
            builder.Property(p => p.PageId).IsRequired();
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => new { p.ProductShortCode, p.PageId });
        }
    }
}
