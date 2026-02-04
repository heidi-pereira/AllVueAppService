namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddColourConfiguration : Migration
    {
        private const string TableName = "ColourConfigurations";
        private const string HistoryTableName = "ColourConfigurations_History";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: TableName,
                columns: table => new
                {
                    ProductShortCode = table.Column<string>(maxLength: 20, nullable: false),
                    Organisation = table.Column<string>(maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(maxLength: 40, nullable: false),
                    EntityInstanceId = table.Column<int>(nullable: false),
                    Colour = table.Column<string>(maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColourConfigurations", x => new { x.ProductShortCode, x.Organisation, x.EntityType, x.EntityInstanceId });
                });

            migrationBuilder.AddTemporalTableSupport(TableName, HistoryTableName);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RemoveTemporalTableSupport(TableName, HistoryTableName);
            migrationBuilder.DropTable(name: TableName);
        }
    }
}
