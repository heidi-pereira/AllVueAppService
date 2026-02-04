using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddOverTimeReportSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OverTimeConfig",
                schema: "Reports",
                table: "SavedReports",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "ShowOvertimeData",
                table: "Parts",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false,
                defaultValue: null);

            //This column was added with default false and isn't in use (outside of feature flag) yet, so update any existing ones to null
            migrationBuilder.Sql("UPDATE [Parts] SET [ShowOvertimeData] = NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OverTimeConfig",
                schema: "Reports",
                table: "SavedReports");

            migrationBuilder.AlterColumn<bool>(
                name: "ShowOvertimeData",
                table: "Parts",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);
        }
    }
}
