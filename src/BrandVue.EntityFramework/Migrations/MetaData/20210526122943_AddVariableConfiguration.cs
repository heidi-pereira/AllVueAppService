namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddVariableConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "InstanceMappings");

            migrationBuilder.DropTemporalTable(
                name: "TypeMappings");

            migrationBuilder.DropTemporalTable(
                name: "MappedFields");

            migrationBuilder.CreateTemporalTable(
                name: "VariableConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProductId = table.Column<string>(type: "nvarchar(20)", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Definition = table.Column<string>(type: "varchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariableConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VariableConfigurations_ProductShortCode_SubProductId_DisplayName",
                table: "VariableConfigurations",
                columns: new[] { "ProductShortCode", "SubProductId", "DisplayName" },
                unique: true,
                filter: "[SubProductId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "VariableConfigurations");

            migrationBuilder.CreateTemporalTable(
                name: "MappedFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FieldName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OriginalFieldName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProductShortCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MappedFields", x => x.Id);
                });

            migrationBuilder.CreateTemporalTable(
                name: "TypeMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromEntityTypeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsFromEntityType = table.Column<bool>(type: "bit", nullable: false),
                    MappedFieldId = table.Column<int>(type: "int", nullable: false),
                    ToEntityTypeDisplayNamePlural = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToEntityTypeDisplayNameSingular = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToEntityTypeName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypeMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TypeMappings_MappedFields_MappedFieldId",
                        column: x => x.MappedFieldId,
                        principalTable: "MappedFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTemporalTable(
                name: "InstanceMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromValuesExpression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToEntityInstanceId = table.Column<int>(type: "int", nullable: false),
                    ToEntityInstanceName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TypeMappingId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstanceMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstanceMappings_TypeMappings_TypeMappingId",
                        column: x => x.TypeMappingId,
                        principalTable: "TypeMappings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstanceMappings_TypeMappingId",
                table: "InstanceMappings",
                column: "TypeMappingId");

            migrationBuilder.CreateIndex(
                name: "IX_MappedFields_ProductShortCode_FieldName",
                table: "MappedFields",
                columns: new[] { "ProductShortCode", "FieldName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TypeMappings_MappedFieldId",
                table: "TypeMappings",
                column: "MappedFieldId");
        }
    }
}
