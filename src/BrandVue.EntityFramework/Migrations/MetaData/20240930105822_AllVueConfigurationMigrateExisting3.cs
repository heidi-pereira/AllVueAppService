#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AllVueConfigurationMigrateExisting3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sqlToExecute = @"  UPDATE [dbo].[AllVueConfigurations] 
  SET AdditionalUIWidgets = '[{""Path"":""dashboard"",""Name"":""Dashboard"",""Icon"":""app_registration"",""AltText"":"""",""Style"":0,""Position"":1,""ReferenceType"":1}]'
  where ReportVueConfiguration is NOT NULL
";
            migrationBuilder.Sql(sqlToExecute);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
