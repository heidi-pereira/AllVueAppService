using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using NJsonSchema.Annotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

namespace BrandVue.EntityFramework.MetaData.ReportVue
{
    [Table("Projects", Schema = "ReportVue")]
    public class ReportVueProject
    {
        public ReportVueProject() { 
        }
        public ReportVueProject(IProductContext context, string name)
        {
            Name = name;
            ProductShortCode = context.ShortCode;
            SubProductId = context.SubProductId;
        }

        public int Id { get; set; }
        [Required, MaxLength(20)]
        public string ProductShortCode { get; set; }
        [CanBeNull, MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        [Required, MaxLength(200)]
        public string Name { get; set; }
        public List<ReportVueProjectRelease> ProjectReleases { get; set; } = new List<ReportVueProjectRelease>();
    }


    [Table("ProjectReleases", Schema = "ReportVue")]
    public class ReportVueProjectRelease
    {
        public int Id { get; set; }

        [Required, MaxLength(SqlTypeConstants.DefaultStringLengthOfGuid)]
        public string UniqueFolderName { get; set; }
        public bool IsActive { get; set; }
        public int VersionOfRelease { get; set; }
        public DateTime ReleaseDate { get; set; }

        [Required, MaxLength(200)]
        public string ReasonForRelease{ get; set; }

        [Required, MaxLength(200)]
        public string UserName { get; set; }

        //Metadata
        [Required, MaxLength(200)]
        public string UserTextForBrandEntity { get; set; }

        [Required]
        public string ResultsForSpecificQuestions { get; set; }

        public int ParentProjectId { get; set; }
        public ReportVueProject Project { get; set; }
        public List<ReportVueProjectPage> ProjectPages { get; set; } = new List<ReportVueProjectPage>();

    }



    [Table("ProjectPages", Schema = "ReportVue")]
    public class ReportVueProjectPage
    {
        public int Id { get; set; }

        public int PageId { get; set; }
        public int FilterId { get; set; }
        public int BrandId { get; set; }


        //Metadata

        public string SectionName { get; set; }
        public string PageName { get; set; }
        public string FilterName { get; set; }
        public string BrandName { get; set; }


        public int ProjectReleaseId { get; set; }
        public ReportVueProjectRelease ProjectRelease { get; set; }
        public List<ReportVueProjectPageTag> Tags { get; set; } = new List<ReportVueProjectPageTag> ();
    }

    [Table("Tags", Schema = "ReportVue")]
    public class ReportVueProjectPageTag
    {
        public int Id { get; set; } 
        public string TagName { get; set; }
        public string TagValue { get; set; }
    }


    public class ReportVueProjectConfiguration : IEntityTypeConfiguration<ReportVueProject>
    {
        public void Configure(EntityTypeBuilder<ReportVueProject> builder)
        {
            builder.HasKey(b => new { b.Id });
            builder.HasIndex(b => new { b.ProductShortCode, b.SubProductId, b.Name }).IsUnique(true).HasFilter(null);
            builder.HasMany(j=> j.ProjectReleases).WithOne(j=>j.Project).HasForeignKey(j => j.ParentProjectId);
        }
    }

    public class ReportVueProjectReleaseConfiguration : IEntityTypeConfiguration<ReportVueProjectRelease>
    {
        public void Configure(EntityTypeBuilder<ReportVueProjectRelease> builder)
        {
            builder.HasKey(b => b.Id);
            builder.HasIndex(b => new { b.UniqueFolderName }).IsUnique(true).HasFilter(null);
            builder.HasMany(j => j.ProjectPages).WithOne(j => j.ProjectRelease).HasForeignKey(j => j.ProjectReleaseId);
        }
    }

    public class ReportVueProjectPageConfiguration : IEntityTypeConfiguration<ReportVueProjectPage>
    {
        public void Configure(EntityTypeBuilder<ReportVueProjectPage> builder)
        {
            builder.HasKey(b => new { b.Id });
            builder.HasIndex(b => new { b.ProjectReleaseId, b.PageId, b.FilterId, b.BrandId }).IsUnique(true).HasFilter(null);
        }
    }
    public class ReportVueProjectPageTagConfiguration : IEntityTypeConfiguration<ReportVueProjectPageTag>
    {
        public void Configure(EntityTypeBuilder<ReportVueProjectPageTag> builder)
        {
            builder.HasKey(b => new { b.Id });
        }
    }
}
