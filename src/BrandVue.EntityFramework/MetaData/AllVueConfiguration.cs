using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using BrandVue.EntityFramework.MetaData.Page;

namespace BrandVue.EntityFramework.MetaData
{
    public class AllVueDocumentationConfiguration
    {
        public bool IsClientUploadingAllowed { get; set; } = true;
        public bool EnableSecureFileDownload { get; set; } = false;
    }

    public class CustomUIIntegration
    {
        public enum IntegrationStyle
        {
            Tab = 0,
            Help = 1,
        }

        public enum IntegrationPosition
        {
            Left = 0,
            Right = 1,
        }

        public enum IntegrationReferenceType
        {
            WebLink = 0,
            ReportVue = 1,
            SurveyManagement = 2,
            Page = 3,
        }

        public CustomUIIntegration()
        {

        }

        public CustomUIIntegration(CustomUIIntegration other)
        {
            Style = other.Style;
            Position = other.Position;
            ReferenceType = other.ReferenceType;
            Icon = other.Icon;
            Name = other.Name;
            AltText = other.AltText;
            Path = other.Path;
        }

        public string Path { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string AltText { get; set; }
        public IntegrationStyle Style { get; set; }
        public IntegrationPosition Position  { get; set; }
        public IntegrationReferenceType ReferenceType { get; set; }

        public void SanitizeData()
        {
            if ( (ReferenceType == IntegrationReferenceType.ReportVue) ||
                 (ReferenceType == IntegrationReferenceType.SurveyManagement) ||
                 (ReferenceType == IntegrationReferenceType.WebLink) )
            {
                if (!string.IsNullOrEmpty(Path))
                {
                    Path = PagePanePartHelper.SanitizeUrl(Path);
                }
            }
        }
    }

    [Table("AllVueConfiguration_WaveVariableForSubset")]
    public class WaveVariableForSubset
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubsetIdentifier { get; set; }

        [MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string VariableIdentifier { get; set; }
    }

    public enum SurveyType
    {
        Adhoc = 0,
        Tracker = 1,

    }

    public class AllVueConfiguration
    {
        public AllVueConfiguration()
        {
            AdditionalUiWidgets = new List<CustomUIIntegration>();
            AllVueDocumentationConfiguration = new AllVueDocumentationConfiguration();
            WaveVariableForSubsets = new List<WaveVariableForSubset>();
        }

        public AllVueConfiguration(IProductContext productContext, AllVueConfigurationDetails details)
        {
            ProductShortCode = productContext.ShortCode;
            SubProductId = productContext.SubProductId;
            IsReportsTabAvailable = details.IsDataTabAvailable;
            IsQuotaTabAvailable = details.IsQuotaTabAvailable;
            IsDocumentsTabAvailable = details.IsDocumentsTabAvailable;
            IsDataTabAvailable = details.IsReportsTabAvailable;
            CheckOrphanedMetricsForCanonicalVariables = details.CheckOrphanedMetricsForCanonicalVariables;
            AdditionalUiWidgets = details.AdditionalUiWidgets ?? new List<CustomUIIntegration>();
            AllowLoadFromMapFile = details.AllowLoadingFromMapFile;
            IsHelpIconAvailable = details.IsHelpIconAvailable;
            AllVueDocumentationConfiguration = details.AllVueDocumentationConfiguration ?? new AllVueDocumentationConfiguration();
            SurveyType = details.SurveyType;
            WaveVariableForSubsets = details.WaveVariableForSubsets.Select(x => new WaveVariableForSubset { SubsetIdentifier = x.SubsetIdentifier, VariableIdentifier = x.VariableIdentifier }).ToList();
        }

        public int Id { get; set; }
        public string ProductShortCode { get; set; }
        [MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }

        public bool IsReportsTabAvailable { get; set; }
        public bool IsDataTabAvailable { get; set; }
        
        [DefaultValue(true)]
        public bool IsQuotaTabAvailable { get; set; }

        [DefaultValue(true)]
        public bool IsDocumentsTabAvailable { get; set; }

        [DefaultValue(true)] 
        public bool IsHelpIconAvailable { get; set; } = true;

        public bool CheckOrphanedMetricsForCanonicalVariables { get; set; }

        public List<CustomUIIntegration> AdditionalUiWidgets { get; set; }

        [DefaultValue(false)]
        public bool AllowLoadFromMapFile { get; set; }

        public AllVueDocumentationConfiguration AllVueDocumentationConfiguration { get; set; }
        public SurveyType SurveyType { get; set; }

        public List<WaveVariableForSubset> WaveVariableForSubsets { get; set; }
    }

    public class AllVueConfigurationDatabaseConfiguration : IEntityTypeConfiguration<AllVueConfiguration>
    {
        public void Configure(EntityTypeBuilder<AllVueConfiguration> builder)
        {
            builder.HasKey(ws => ws.Id);
            builder.Property(ws => ws.AdditionalUiWidgets).HasJsonConversion();
            builder.Property(ws => ws.AllVueDocumentationConfiguration).HasJsonConversion();
            // This index ensures that each respondent belongs to a single Weighting Scheme (assuming that the filter metric is 'single choice'). 
            builder.HasIndex(b => new {b.SubProductId, b.ProductShortCode}).IsUnique();
        }
    }
}