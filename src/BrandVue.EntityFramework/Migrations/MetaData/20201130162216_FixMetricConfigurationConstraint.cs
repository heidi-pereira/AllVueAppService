namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class FixMetricConfigurationConstraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MetricConfigurations_Name_ProductShortCode",
                table: "MetricConfigurations");

            migrationBuilder.AlterColumn<string>(
                name: "SubProductId",
                table: "MetricConfigurations",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricConfigurations_Name_ProductShortCode_SubProductId",
                table: "MetricConfigurations",
                columns: new[] { "Name", "ProductShortCode", "SubProductId" },
                unique: true,
                filter: "[SubProductId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MetricConfigurations_Name_ProductShortCode_SubProductId",
                table: "MetricConfigurations");

            migrationBuilder.AlterColumn<string>(
                name: "SubProductId",
                table: "MetricConfigurations",
                nullable: true,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricConfigurations_Name_ProductShortCode",
                table: "MetricConfigurations",
                columns: new[] { "Name", "ProductShortCode" },
                unique: true);
        }
    }
}
