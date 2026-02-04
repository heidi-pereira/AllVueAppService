#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AllowEntityInstancesToBeDisabled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
ALTER TABLE [EntityInstanceConfigurations]
ADD [EnabledBySubset] [nvarchar](4000) NOT NULL CONSTRAINT [DF_EntityInstanceConfigurations_EnabledBySubset] DEFAULT('{}')";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql = @"
ALTER TABLE [dbo].[EntityInstanceConfigurations] DROP CONSTRAINT [DF_EntityInstanceConfigurations_EnabledBySubset]
ALTER TABLE EntityInstanceConfigurations DROP COLUMN [EnabledBySubset]";
            migrationBuilder.Sql(sql);
        }
    }
}
