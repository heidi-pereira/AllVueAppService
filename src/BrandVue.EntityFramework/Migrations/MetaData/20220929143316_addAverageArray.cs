#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class addAverageArray : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AverageType",
                table: "Parts",
                type: "varchar(256)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageType",
                table: "Parts");
        }
    }
}
