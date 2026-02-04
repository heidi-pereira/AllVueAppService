namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class showParts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShowTop",
                table: "Parts",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowTop",
                table: "Parts");
        }
    }
}
