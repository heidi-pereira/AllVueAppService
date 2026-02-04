namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class DBPartAddReportOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReportOrder",
                table: "Parts",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReportOrder",
                table: "Parts");
        }
    }
}
