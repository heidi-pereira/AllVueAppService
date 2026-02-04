namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddEntitySetConfigurationsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTemporalTable(
                name: "EntitySetConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Organisation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subset = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Instances = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeyInstances = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MainInstance = table.Column<int>(type: "int", nullable: true),
                    IsFallback = table.Column<bool>(type: "bit", nullable: false),
                    IsSectorSet = table.Column<bool>(type: "bit", nullable: false),
                    IsDisabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntitySetConfigurations", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "EntitySetConfigurations");
        }
    }
}
