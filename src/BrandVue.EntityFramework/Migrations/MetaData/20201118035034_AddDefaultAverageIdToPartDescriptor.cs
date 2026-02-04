namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddDefaultAverageIdToPartDescriptor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultAverageId",
                table: "Parts",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultAverageId",
                table: "Parts");
        }
    }
}
