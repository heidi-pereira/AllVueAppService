namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddSubsetColumnToDbPane : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subset",
                table: "Panes",
                type: "nvarchar(450)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Subset",
                table: "Panes");
        }
    }
}
