using Microsoft.EntityFrameworkCore;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.ReportVue;
using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions;
using BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions;
using BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue;

namespace BrandVue.EntityFramework.MetaData
{
    /// <summary>
    /// Context for the BrandVueMeta database
    /// </summary>
    public class MetaDataContext : DbContext
    {
        public MetaDataContext(DbContextOptions<MetaDataContext> builderOptions) : base(builderOptions) { }

        //needed for Mocking reasons
        public MetaDataContext()
        {
        }

        public virtual DbSet<SubsetConfiguration> SubsetConfigurations { get; set; }
        public virtual DbSet<Bookmark> Bookmarks { get; set; }
        public virtual DbSet<SupportableUser> SupportableUsers { get; set; }
        public virtual DbSet<ColourConfiguration> ColourConfigurations { get; set; }
        public virtual DbSet<VariableConfiguration> VariableConfigurations { get; set; }
        public virtual DbSet<MetricConfiguration> MetricConfigurations { get; set; }
        public virtual DbSet<MetricAbout> MetricAbouts { get; set; }
        public virtual DbSet<PageAbout> PageAbouts { get; set; }
        public virtual DbSet<DbPage> Pages { get; set; }
        public virtual DbSet<DbPane> Panes { get; set; }
        public virtual DbSet<DbPart> Parts { get; set; }
        public virtual DbSet<PageSubsetConfiguration> PageSubsetConfigurations { get; set; }
        public virtual DbSet<CustomPeriod> CustomPeriods { get; set; }
        public virtual DbSet<EntitySetConfiguration> EntitySetConfigurations { get; set; }
        public virtual DbSet<SavedBreakCombination> SavedBreaks { get; set; }
        public virtual DbSet<DefaultSavedBreaks> DefaultSavedBreaks { get; set; }
        public virtual DbSet<EntityTypeConfiguration> EntityTypeConfigurations { get; set; }
        public virtual DbSet<EntityInstanceConfiguration> EntityInstanceConfigurations { get; set; }
        public virtual DbSet<SavedReport> SavedReports { get; set; }
        public virtual DbSet<ReportTemplate> ReportTemplates { get; set; }
        public virtual DbSet<DefaultSavedReport> DefaultSavedReports { get; set; }
        [Obsolete("Needs to go")]
        public DbSet<WeightingStrategy> WeightingStrategies { get; set; }
        [Obsolete("Needs to go")]
        public virtual DbSet<WeightingScheme> WeightingSchemes { get; set; }
        public virtual DbSet<AverageConfiguration> Averages { get; set; }
        public virtual DbSet<EntitySetAverageMappingConfiguration> EntitySetAverageMappingConfigurations { get; set; }
        public virtual DbSet<WeightingPlanConfiguration> WeightingPlanConfigurations { get; set; }
        public virtual DbSet<WeightingTargetConfiguration> WeightingTargetConfigurations { get; set; }
        public virtual DbSet<LinkedMetric> LinkedMetrics { get; set; }
        public virtual DbSet<AllVueConfiguration> AllVueConfigurations { get; set; }
        public virtual DbSet<ReportVueProject> ReportVueProjects { get; set; }
        public virtual DbSet<ReportVueProjectRelease> ReportVueProjectReleases { get; set; }
        public virtual DbSet<ReportVueProjectPage> ReportVueProjectPages { get; set; }
        public virtual DbSet<ResponseWeightingContext> ResponseWeightingContexts { get; set; }
        public virtual DbSet<ResponseWeightConfiguration> ResponseWeights { get; set; }
        public virtual DbSet<Feature> Features { get; set; }
        public virtual DbSet<UserFeature> UserFeatures { get; set; }
        public virtual DbSet<OrganisationFeature> OrganisationFeatures { get; set; }
        public virtual DbSet<UserFeaturePermission> UserFeaturePermissions { get; set; }
        public virtual DbSet<PermissionOption> PermissionOptions { get; set; }
        public virtual DbSet<PermissionFeature> PermissionFeatures { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<AllVueRule> AllVueRules { get; set; }
        public virtual DbSet<UserDataPermission> UserPermissions { get; set; }
        public virtual DbSet<AllVueFilter> AllVueFilters { get; set; }
        public virtual DbSet<BaseRule> BaseRules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Bookmark>().Property(a => a.AppBase).IsUnicode(false);
            modelBuilder.Entity<Bookmark>().Property(a => a.Url).IsUnicode(false);
            modelBuilder.Entity<Bookmark>().HasIndex(b => new { b.AppBase, b.Url }).IsUnique();
            modelBuilder.Entity<SupportableUser>().HasIndex(u => u.UserId).IsUnique();
            modelBuilder.Entity<ColourConfiguration>().HasKey(c => new { c.ProductShortCode, c.Organisation, c.EntityType, c.EntityInstanceId });

            modelBuilder.Entity<DbPage>().HasKey(p => new { p.Id });
            // Unique lookup
            modelBuilder.Entity<DbPage>().HasIndex(p => new { p.ProductShortCode, p.SubProductId, p.Name }).IsUnique().HasFilter(null);

            modelBuilder.Entity<DbPane>().HasKey(p => new { p.Id });
            // Unique lookup
            modelBuilder.Entity<DbPane>().HasIndex(p => new { p.ProductShortCode, p.SubProductId, p.PaneId }).HasFilter(null);

            modelBuilder.Entity<DbPart>().HasKey(p => new { p.Id });
            // Non unique lookup
            modelBuilder.Entity<DbPart>().HasIndex(p => new { p.ProductShortCode, p.SubProductId, p.PaneId }).HasFilter(null);


            modelBuilder.Entity<VariableDependency>()
                .HasKey(bc => new { bc.VariableId, bc.DependentUponVariableId });
            modelBuilder.Entity<VariableDependency>()
                .HasOne(bc => bc.Variable)
                .WithMany(b => b.VariableDependencies)
                .HasForeignKey(bc => bc.VariableId)
                .OnDelete(DeleteBehavior.ClientCascade);
            modelBuilder.Entity<VariableDependency>()
                .HasOne(bc => bc.DependentUponVariable)
                .WithMany(c => c.VariablesDependingOnThis)
                .HasForeignKey(bc => bc.DependentUponVariableId)
                .OnDelete(DeleteBehavior.ClientCascade);

            modelBuilder.ApplyConfiguration(new MetricConfigurationConfiguration());
            modelBuilder.ApplyConfiguration(new MetricAboutConfiguration());
            modelBuilder.ApplyConfiguration(new PageAboutConfiguration());
            modelBuilder.ApplyConfiguration(new DbPartConfiguration());
            modelBuilder.ApplyConfiguration(new VariableConfigurationConfiguration());
            modelBuilder.ApplyConfiguration(new CustomPeriodConfiguration());
            modelBuilder.ApplyConfiguration(new SavedBreakCombinationConfiguration());
            modelBuilder.ApplyConfiguration(new DefaultSavedBreaksConfiguration());

            modelBuilder.ApplyConfiguration(new SavedReportConfiguration());
            modelBuilder.ApplyConfiguration(new DefaultSavedReportConfiguration());
            modelBuilder.ApplyConfiguration(new ReportColourSettingsConfiguration());

            modelBuilder.ApplyConfiguration(new WeightingStrategyConfiguration());
            modelBuilder.ApplyConfiguration(new WeightingSchemeConfiguration());
            modelBuilder.ApplyConfiguration(new AverageConfigurationConfiguration());
            modelBuilder.ApplyConfiguration(new LinkedMetricConfiguration());
            modelBuilder.ApplyConfiguration(new EntityInstanceConfigurationConfiguration());

            modelBuilder.ApplyConfiguration(new ReportVueProjectReleaseConfiguration());
            modelBuilder.ApplyConfiguration(new ReportVueProjectConfiguration());
            modelBuilder.ApplyConfiguration(new ReportVueProjectPageConfiguration());
            modelBuilder.ApplyConfiguration(new ReportVueProjectPageTagConfiguration());

            modelBuilder.ApplyConfiguration(new FeaturesConfiguration());
            modelBuilder.ApplyConfiguration(new UserFeaturesConfiguration());
            modelBuilder.ApplyConfiguration(new OrganisationFeaturesConfiguration());

            modelBuilder.Entity<OrganisationFeature>(t =>
                t.ToTable(tb =>
                    tb.IsTemporal(x =>
                        {
                            x.HasPeriodStart("SysStartTime");
                            x.HasPeriodEnd("SysEndTime");
                        }
                    )
                ).Property("UpdatedDate").HasComputedColumnSql("SysStartTime")
            );

            modelBuilder.Entity<EntityTypeConfiguration>()
            .Property(e => e.SurveyChoiceSetNames)
            .HasJsonConversion(SqlTypeConstants.DefaultJsonVarcharLength);

            modelBuilder.Entity<SubsetConfiguration>()
                .Property(e => e.SurveyIdToAllowedSegmentNames)
                .HasJsonConversion();

            modelBuilder.Entity<SubsetConfiguration>()
                .Property(e => e.Iso2LetterCountryCode)
                .HasMaxLength(2)
                .IsFixedLength()
                .IsUnicode(false);

            modelBuilder.Entity<WeightingPlanConfiguration>()
                .HasMany(j => j.ChildTargets)
                .WithOne(j => j.ParentWeightingPlan)
                .HasForeignKey(j => j.ParentWeightingPlanId);

            modelBuilder.Entity<WeightingTargetConfiguration>()
                .HasMany(j => j.ChildPlans)
                .WithOne(j => j.ParentTarget)
                .HasForeignKey(j => j.ParentWeightingTargetId);

            modelBuilder.Entity<WeightingTargetConfiguration>()
                .HasOne(j => j.ResponseWeightingContext)
                .WithOne()
                .HasForeignKey<ResponseWeightingContext>(j => j.WeightingTargetId)
                .IsRequired(false);

            modelBuilder.Entity<WeightingPlanConfiguration>()
                .HasIndex(plan => new { plan.ProductShortCode, plan.SubProductId, plan.SubsetId, plan.ParentWeightingTargetId, plan.VariableIdentifier })
                .IsUnique().HasFilter(null);

            modelBuilder.Entity<WeightingTargetConfiguration>()
                .HasIndex(target => new { target.ProductShortCode, target.SubProductId, target.SubsetId, target.ParentWeightingPlanId, target.EntityInstanceId })
                .IsUnique().HasFilter(null);

            modelBuilder.Entity<WeightingTargetConfiguration>().Property(e => e.Target).HasPrecision(20, 10);
            modelBuilder.ApplyConfiguration(new AllVueConfigurationDatabaseConfiguration());
            modelBuilder.ApplyConfiguration(new ResponseWeightingContextConfiguration());
            modelBuilder.Entity<ResponseWeightConfiguration>().Property(e => e.Weight).HasPrecision(20, 10);

            modelBuilder.Entity<ResponseWeightingContext>()
                .HasMany(e => e.ResponseWeights)
                .WithOne()
                .HasForeignKey(ResponseWeightingContext.ResponseWeightingContextIdShadowPropertyName)
                .IsRequired();
            
            modelBuilder.Entity<ResponseWeightingContext>()
                .HasIndex(e => e.WeightingTargetId)
                .IsUnique()
                .HasFilter("[WeightingTargetId] IS NOT NULL")
                .IncludeProperties(e => new { e.Id, e.ProductShortCode, e.SubProductId, e.Context, e.SubsetId });

            modelBuilder.Entity<Feature>()
                .Property(feature => feature.FeatureCode)
                .HasConversion(x => x.ToString(), x => ConvertFeatureCode(x))
                .HasMaxLength(100);

            modelBuilder.ApplyConfiguration(new ReportTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new UserDataPermissionConfiguration());
            modelBuilder.ApplyConfiguration(new AllVueFilterConfiguration());
            modelBuilder.ApplyConfiguration(new AllVueRuleConfiguration());
            modelBuilder.ApplyConfiguration(new BaseRuleConfiguration());
            modelBuilder.ApplyConfiguration(new UserFeaturePermissionConfiguration());
            modelBuilder.ApplyConfiguration(new PermissionFeatureConfiguration());
            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new PermissionOptionConfiguration());
        }

        private FeatureCode ConvertFeatureCode(string value)
        {
            return Enum.TryParse<FeatureCode>(value, out var code) ? code : FeatureCode.unknown;
        }
    }
}
