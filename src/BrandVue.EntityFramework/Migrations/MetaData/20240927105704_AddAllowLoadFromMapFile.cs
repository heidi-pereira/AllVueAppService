#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddAllowLoadFromMapFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowLoadFromMapFile",
                table: "AllVueConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE AllVueConfigurations SET AllowLoadFromMapFile = 1
                WHERE ProductShortCode NOT IN ('survey', 'brandvue', 'wealth')
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowLoadFromMapFile",
                table: "AllVueConfigurations");
        }
    }
}
