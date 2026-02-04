namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddBaseExpressionOverrideToDbPart : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseExpressionOverride",
                table: "Parts",
                type: "varchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseExpressionOverride",
                table: "Parts");
        }
    }
}
