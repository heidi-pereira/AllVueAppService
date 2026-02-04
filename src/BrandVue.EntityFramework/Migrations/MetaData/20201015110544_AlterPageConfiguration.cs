namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AlterPageConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "View",
                table: "Panes",
                nullable: true,
                oldClrType: typeof(int));

            migrationBuilder.AddColumn<int>(
                name: "PageDisplayIndex",
                table: "Pages",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Pages",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PageDisplayIndex",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Pages");

            migrationBuilder.AlterColumn<int>(
                name: "View",
                table: "Panes",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);
        }
    }
}
