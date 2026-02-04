namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class BreaksForParts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Breaks",
                table: "Parts",
                type: "varchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OverRideReportSettings",
                table: "Parts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Breaks",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "OverRideReportSettings",
                table: "Parts");
        }
    }
}
