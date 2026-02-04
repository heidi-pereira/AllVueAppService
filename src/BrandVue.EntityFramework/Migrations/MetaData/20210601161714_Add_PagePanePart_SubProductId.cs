namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class Add_PagePanePart_SubProductId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Parts_ProductShortCode_PaneId",
                table: "Parts");

            migrationBuilder.DropIndex(
                name: "IX_Panes_ProductShortCode_PaneId",
                table: "Panes");

            migrationBuilder.DropIndex(
                name: "IX_Pages_ProductShortCode_Name",
                table: "Pages");

            migrationBuilder.AddColumn<string>(
                name: "SubProductId",
                table: "Parts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubProductId",
                table: "Panes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubProductId",
                table: "Pages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parts_ProductShortCode_SubProductId_PaneId",
                table: "Parts",
                columns: new[] { "ProductShortCode", "SubProductId", "PaneId" });

            migrationBuilder.CreateIndex(
                name: "IX_Panes_ProductShortCode_SubProductId_PaneId",
                table: "Panes",
                columns: new[] { "ProductShortCode", "SubProductId", "PaneId" });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_ProductShortCode_SubProductId_Name",
                table: "Pages",
                columns: new[] { "ProductShortCode", "SubProductId", "Name" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Parts_ProductShortCode_SubProductId_PaneId",
                table: "Parts");

            migrationBuilder.DropIndex(
                name: "IX_Panes_ProductShortCode_SubProductId_PaneId",
                table: "Panes");

            migrationBuilder.DropIndex(
                name: "IX_Pages_ProductShortCode_SubProductId_Name",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "SubProductId",
                table: "Parts");

            migrationBuilder.DropColumn(
                name: "SubProductId",
                table: "Panes");

            migrationBuilder.DropColumn(
                name: "SubProductId",
                table: "Pages");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_ProductShortCode_PaneId",
                table: "Parts",
                columns: new[] { "ProductShortCode", "PaneId" });

            migrationBuilder.CreateIndex(
                name: "IX_Panes_ProductShortCode_PaneId",
                table: "Panes",
                columns: new[] { "ProductShortCode", "PaneId" },
                unique: true,
                filter: "[ProductShortCode] IS NOT NULL AND [PaneId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_ProductShortCode_Name",
                table: "Pages",
                columns: new[] { "ProductShortCode", "Name" },
                unique: true,
                filter: "[ProductShortCode] IS NOT NULL AND [Name] IS NOT NULL");
        }
    }
}
