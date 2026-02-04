#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class ChangeAverageSubsetIdsToNonNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE [dbo].[Averages]
SET [SubsetIds] = '[]'
WHERE [SubsetIds] IS NULL
");
            migrationBuilder.AlterColumn<string>(
                name: "SubsetIds",
                table: "Averages",
                type: "varchar(max)",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "varchar(max)",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SubsetIds",
                table: "Averages",
                type: "varchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(max)");
        }
    }
}
