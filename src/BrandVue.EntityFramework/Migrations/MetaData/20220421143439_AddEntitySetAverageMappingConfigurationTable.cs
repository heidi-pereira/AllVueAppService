#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddEntitySetAverageMappingConfigurationTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTemporalTable(
                name: "EntitySetAverageMappingConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentEntitySetId = table.Column<int>(type: "int", nullable: false),
                    ChildEntitySetId = table.Column<int>(type: "int", nullable: false),
                    ExcludeMainInstance = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EntitySetAverageMappingConfigurations", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "EntitySetAverageMappingConfigurations");
        }
    }
}
