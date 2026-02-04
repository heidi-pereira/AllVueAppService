#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AllVueConfigurationDetailsAddTabs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseNewWeightingUI",
                table: "AllVueConfigurations");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalUiWidgets",
                table: "AllVueConfigurations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalUiWidgets",
                table: "AllVueConfigurations");

            migrationBuilder.AddColumn<bool>(
                name: "UseNewWeightingUI",
                table: "AllVueConfigurations",
                type: "bit",
                nullable: true);
        }
    }
}
