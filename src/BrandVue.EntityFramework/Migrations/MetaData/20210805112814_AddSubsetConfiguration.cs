namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddSubsetConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubsetConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DisplayNameShort = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Iso2LetterCountryCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Disabled = table.Column<bool>(type: "bit", nullable: false),
                    AllowedSegmentNames = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EnableRawDataApiAccess = table.Column<bool>(type: "bit", nullable: false),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SubProductId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubsetConfigurations", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubsetConfigurations");
        }
    }
}
