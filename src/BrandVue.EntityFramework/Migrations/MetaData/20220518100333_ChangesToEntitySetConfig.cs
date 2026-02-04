#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class ChangesToEntitySetConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EntitySetAverageMappingConfigurations_ParentEntitySetId",
                table: "EntitySetAverageMappingConfigurations",
                column: "ParentEntitySetId");

            migrationBuilder.AddForeignKey(
                name: "FK_EntitySetAverageMappingConfigurations_EntitySetConfigurations_ParentEntitySetId",
                table: "EntitySetAverageMappingConfigurations",
                column: "ParentEntitySetId",
                principalTable: "EntitySetConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntitySetAverageMappingConfigurations_EntitySetConfigurations_ParentEntitySetId",
                table: "EntitySetAverageMappingConfigurations");
            
            migrationBuilder.DropIndex(
                name: "IX_EntitySetAverageMappingConfigurations_ParentEntitySetId",
                table: "EntitySetAverageMappingConfigurations");
        }
    }
}
