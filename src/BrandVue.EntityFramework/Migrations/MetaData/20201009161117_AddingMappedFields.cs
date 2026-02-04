using Microsoft.EntityFrameworkCore.Metadata;

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddingMappedFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "ChartConfigurations");

            migrationBuilder.CreateTemporalTable(
                name: "MappedFields",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProductShortCode = table.Column<string>(maxLength: 20, nullable: false),
                    FieldName = table.Column<string>(nullable: false),
                    OriginalFieldName = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MappedFields", x => x.Id);
                });

            migrationBuilder.CreateTemporalTable(
                name: "TypeMappings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    MappedFieldId = table.Column<int>(nullable: false),
                    IsFromEntityType = table.Column<bool>(nullable: false),
                    FromEntityTypeName = table.Column<string>(nullable: true),
                    ToEntityTypeName = table.Column<string>(nullable: false),
                    ToEntityTypeDisplayNameSingular = table.Column<string>(nullable: false),
                    ToEntityTypeDisplayNamePlural = table.Column<string>(nullable: false)
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
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    TypeMappingId = table.Column<int>(nullable: false),
                    FromValuesExpression = table.Column<string>(nullable: false),
                    ToEntityInstanceName = table.Column<string>(nullable: false),
                    ToEntityInstanceId = table.Column<int>(nullable: false)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTemporalTable(
                name: "InstanceMappings");

            migrationBuilder.DropTemporalTable(
                name: "TypeMappings");

            migrationBuilder.DropTemporalTable(
                name: "MappedFields");

            migrationBuilder.CreateTemporalTable(
                name: "ChartConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    IsDraft = table.Column<bool>(nullable: false),
                    MetricDefinition = table.Column<string>(nullable: false),
                    ProductShortCode = table.Column<string>(maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChartConfigurations", x => x.Id);
                });
        }
    }
}
