using BrandVue.EntityFramework.MetaData;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class AddCreatedFromToEntityTypeConfiguration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedFrom",
                table: "EntityTypeConfigurations",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE EntityTypeConfigurations " +
                                 $"SET CreatedFrom = {(int) EntityTypeCreatedFrom.QuestionField};");
            
migrationBuilder.Sql("UPDATE EntityTypeConfigurations " +
                             $"SET CreatedFrom = {(int) EntityTypeCreatedFrom.Variable} " +
                             "FROM EntityTypeConfigurations AS et " +
                             "WHERE EXISTS (" +
                             "   SELECT *" +
                             "   FROM VariableConfigurations AS vc" +
                             @"   WHERE et.SubProductId = vc.SubProductId and vc.Definition like '{""ToEntityTypeName"":""' + et.Identifier + '"",%'" +
                             ");");
            
            migrationBuilder.Sql("UPDATE EntityTypeConfigurations " +
                                 $"SET CreatedFrom = {(int) EntityTypeCreatedFrom.Default}" +
                                 "WHERE Identifier = \'brand\' OR Identifier = \'profile\';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedFrom",
                table: "EntityTypeConfigurations");
        }
    }
}
