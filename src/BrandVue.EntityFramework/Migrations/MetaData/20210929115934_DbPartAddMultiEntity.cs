namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class DbPartAddMultiEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MultipleEntitySplitByAndMain",
                table: "Parts",
                type: "varchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MultipleEntitySplitByAndMain",
                table: "Parts");
        }
    }
}
