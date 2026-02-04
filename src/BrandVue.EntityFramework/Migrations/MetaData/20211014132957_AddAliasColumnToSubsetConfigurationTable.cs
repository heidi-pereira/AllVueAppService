namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddAliasColumnToSubsetConfigurationTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "SubsetConfigurations",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alias",
                table: "SubsetConfigurations");
        }
    }
}
