#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class addPropertyHasData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasData",
                table: "MetricConfigurations",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasData",
                table: "MetricConfigurations");
        }
    }
}
