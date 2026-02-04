namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddingDownIsGoodToMetricConfigurations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "MetricConfigurations",
                nullable: true,
                oldClrType: typeof(DateTime));

            migrationBuilder.AlterColumn<string>(
                name: "CalType",
                table: "MetricConfigurations",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.RenameColumn("CalType", "MetricConfigurations", "CalcType");

            migrationBuilder.AddColumn<bool>(
                name: "DownIsGood",
                table: "MetricConfigurations",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownIsGood",
                table: "MetricConfigurations");

            migrationBuilder.RenameColumn("CalcType", "MetricConfigurations", "CalType");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "MetricConfigurations",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CalType",
                table: "MetricConfigurations",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
