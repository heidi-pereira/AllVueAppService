#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddDateOverrideBoolToSubset : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AlwaysShowDataUpToCurrentDate",
                schema: "dbo",
                table: "SubsetConfigurations",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlwaysShowDataUpToCurrentDate",
                schema: "dbo",
                table: "SubsetConfigurations");
        }
    }
}
