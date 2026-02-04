#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AdditionalUIConfigurationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllVueDocumentationConfiguration",
                table: "AllVueConfigurations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHelpIconAvailable",
                table: "AllVueConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllVueDocumentationConfiguration",
                table: "AllVueConfigurations");

            migrationBuilder.DropColumn(
                name: "IsHelpIconAvailable",
                table: "AllVueConfigurations");
        }
    }
}
