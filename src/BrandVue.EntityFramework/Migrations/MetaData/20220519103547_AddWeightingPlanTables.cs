#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddWeightingPlanTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WeightingPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VariableIdentifier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ParentWeightingTargetId = table.Column<int>(type: "int", nullable: true),
                    IsWeightingGroupRoot = table.Column<bool>(type: "bit", nullable: false),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SubsetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightingPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeightingTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityInstanceId = table.Column<int>(type: "int", nullable: false),
                    Target = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ParentWeightingPlanId = table.Column<int>(type: "int", nullable: false),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SubsetId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightingTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightingTargets_WeightingPlans_ParentWeightingPlanId",
                        column: x => x.ParentWeightingPlanId,
                        principalTable: "WeightingPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightingPlans_ParentWeightingTargetId",
                table: "WeightingPlans",
                column: "ParentWeightingTargetId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightingTargets_ParentWeightingPlanId",
                table: "WeightingTargets",
                column: "ParentWeightingPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_WeightingPlans_WeightingTargets_ParentWeightingTargetId",
                table: "WeightingPlans",
                column: "ParentWeightingTargetId",
                principalTable: "WeightingTargets",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WeightingPlans_WeightingTargets_ParentWeightingTargetId",
                table: "WeightingPlans");

            migrationBuilder.DropTable(
                name: "WeightingTargets");

            migrationBuilder.DropTable(
                name: "WeightingPlans");
        }
    }
}
