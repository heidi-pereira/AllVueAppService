namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddEntityTypeAndInstanceConfigurations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTemporalTable(
                name: "EntityInstanceConfigurations",
                columns: table => new
                {
                    EntityInstanceConfigurationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveyChoiceId = table.Column<int>(type: "int", nullable: false),
                    EntityTypeIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductShortCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SubProductId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StartDateBySubset = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityInstanceConfigurations", x => x.EntityInstanceConfigurationId);
                });

            migrationBuilder.CreateTemporalTable(
                name: "EntityTypeConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SubProductId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Identifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DisplayNameSingular = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DisplayNamePlural = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SurveyChoiceSetNames = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntityTypeConfigurations", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "EntityInstanceConfigurations");

            migrationBuilder.DropTemporalTable(
                name: "EntityTypeConfigurations");
        }
    }
}
