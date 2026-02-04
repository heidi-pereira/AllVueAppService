#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AttachResponseWeightsToWeightingTargets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeightingTargetId",
                table: "ResponseWeightingContexts",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ResponseWeightingContexts_WeightingTargets_WeightingTargetId",
                table: "ResponseWeightingContexts",
                column: "WeightingTargetId",
                principalTable: "WeightingTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResponseWeightingContexts_WeightingTargets_WeightingTargetId",
                table: "ResponseWeightingContexts");

            migrationBuilder.DropColumn(
                name: "WeightingTargetId",
                table: "ResponseWeightingContexts");
        }
    }
}
