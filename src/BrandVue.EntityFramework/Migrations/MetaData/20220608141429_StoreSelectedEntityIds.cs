#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class StoreSelectedEntityIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedEntityInstances",
                table: "Parts",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedEntityInstances",
                table: "Parts");
        }
    }
}
