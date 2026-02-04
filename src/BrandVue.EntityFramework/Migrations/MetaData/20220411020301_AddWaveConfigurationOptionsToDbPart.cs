#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddWaveConfigurationOptionsToDbPart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Waves",
                table: "Parts",
                type: "varchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Waves",
                table: "Parts");
        }
    }
}
