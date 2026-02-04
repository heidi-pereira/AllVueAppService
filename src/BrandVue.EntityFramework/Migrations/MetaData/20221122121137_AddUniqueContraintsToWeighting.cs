#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddUniqueContraintsToWeighting : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WeightingTargets_ProductShortCode_SubProductId_SubsetId_ParentWeightingPlanId_EntityInstanceId",
                table: "WeightingTargets",
                columns: new[] { "ProductShortCode", "SubProductId", "SubsetId", "ParentWeightingPlanId", "EntityInstanceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeightingPlans_ProductShortCode_SubProductId_SubsetId_ParentWeightingTargetId_VariableIdentifier",
                table: "WeightingPlans",
                columns: new[] { "ProductShortCode", "SubProductId", "SubsetId", "ParentWeightingTargetId", "VariableIdentifier" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeightingTargets_ProductShortCode_SubProductId_SubsetId_ParentWeightingPlanId_EntityInstanceId",
                table: "WeightingTargets");

            migrationBuilder.DropIndex(
                name: "IX_WeightingPlans_ProductShortCode_SubProductId_SubsetId_ParentWeightingTargetId_VariableIdentifier",
                table: "WeightingPlans");
        }
    }
}
