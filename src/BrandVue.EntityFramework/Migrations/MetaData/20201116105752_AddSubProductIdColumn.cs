namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddSubProductIdColumn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubProductId",
                table: "MetricConfigurations",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubProductId",
                table: "MetricConfigurations");

        }
    }
}
