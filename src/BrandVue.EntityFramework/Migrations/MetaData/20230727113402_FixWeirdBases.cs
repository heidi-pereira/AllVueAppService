#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class FixWeirdBases : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Penetration bases for eatingout, drinks, finance and retail were changed from Consumer_segment to Consumer_segment_entity
            var sql = @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Consumer_segment', 'Consumer_segment_entity') WHERE ProductShortCode = 'eatingout' AND DisplayName LIKE 'Penetration (L%'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Consumer_segment', 'Consumer_segment_entity') WHERE ProductShortCode = 'drinks' AND DisplayName LIKE 'Penetration (L%'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Consumer_segment', 'Consumer_segment_entity') WHERE ProductShortCode = 'finance' AND DisplayName LIKE 'Penetration (L%'";
            migrationBuilder.Sql(sql);

            sql = @"DELETE TOP (1) FROM [VariableConfigurations] WHERE ProductShortCode = 'charities' AND DisplayName LIKE 'Ever supported base'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Consumer_segment', 'Consumer_segment_entity') WHERE ProductShortCode = 'charities' AND DisplayName = 'Ever supported base'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Identifier = 'Supporter_base' WHERE ProductShortCode = 'charities' AND DisplayName = 'Supporter base'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Identifier = 'Lapsed_supporter_base' WHERE ProductShortCode = 'charities' AND DisplayName = 'Lapsed Supporter base'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Identifier = 'Never_supported_base' WHERE ProductShortCode = 'charities' AND DisplayName = 'Never supported base'";
            migrationBuilder.Sql(sql);

            sql = @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, ' segment ', ' ShopperSegment ') WHERE ProductShortCode = 'brandvue' AND DisplayName = 'Penetration (L12M) base'";
            migrationBuilder.Sql(sql);

            sql = @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Consumer_segment', 'Consumer_segment_entity') WHERE ProductShortCode = 'retail' AND DisplayName LIKE 'Penetration (L%'";
            migrationBuilder.Sql(sql);

            // Day of the week and Time of day visited bases for eatingout were changed from Day_of_the_week_ExpPro and Time_of_day_visited_base to Day_of_the_week and Time_of_day_visited
            sql = @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Day_of_the_week', 'Day_of_the_week_ExpPro') WHERE ProductShortCode = 'eatingout' AND DisplayName LIKE 'Day of the week%'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Time_of_day_visited_base', 'Time_of_day_visited') WHERE ProductShortCode = 'eatingout' AND DisplayName LIKE 'Time of day visited%'";
            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var sql =
                @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Consumer_segment_entity', 'Consumer_segment') WHERE ProductShortCode = 'eatingout' AND DisplayName LIKE 'Penetration (L%'";
            migrationBuilder.Sql(sql);
            sql =
                @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Consumer_segment_entity', 'Consumer_segment') WHERE ProductShortCode = 'drinks' AND DisplayName LIKE 'Penetration (L%'";
            migrationBuilder.Sql(sql);
            sql =
                @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Consumer_segment_entity', 'Consumer_segment') WHERE ProductShortCode = 'finance' AND DisplayName LIKE 'Penetration (L%'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Consumer_segment_entity', 'Consumer_segment') WHERE ProductShortCode = 'retail' AND DisplayName LIKE 'Penetration (L%'";
            migrationBuilder.Sql(sql);

            sql = @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Consumer_segment_entity', 'Consumer_segment') WHERE ProductShortCode = 'charities' AND DisplayName = 'Ever supported base'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Identifier = 'L12M_base' WHERE ProductShortCode = 'charities' AND DisplayName = 'Supporter base'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Identifier = 'L12M_base' WHERE ProductShortCode = 'charities' AND DisplayName = 'Lapsed Supporter base'";
            migrationBuilder.Sql(sql);
            sql = @"UPDATE [VariableConfigurations] SET Identifier = 'L12M_base' WHERE ProductShortCode = 'charities' AND DisplayName = 'Never supported base'";
            migrationBuilder.Sql(sql);

            sql =
                @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, ' ShopperSegment ', ' segment ') WHERE ProductShortCode = 'brandvue' AND DisplayName = 'Penetration (L12M) base'";
            migrationBuilder.Sql(sql);

            // Day of the week and Time of day visited bases for eatingout were changed from Day_of_the_week_ExpPro and Time_of_day_visited_base to Day_of_the_week and Time_of_day_visited
            sql =
                @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Day_of_the_week_ExpPro', 'Day_of_the_week') WHERE ProductShortCode = 'eatingout' AND DisplayName LIKE 'Day of the week%'";
            migrationBuilder.Sql(sql);
            sql =
                @"UPDATE [VariableConfigurations] SET Definition = REPLACE(Definition, 'Time_of_day_visited', 'Time_of_day_visited_base') WHERE ProductShortCode = 'eatingout' AND DisplayName LIKE 'Time of day visited%'";
            migrationBuilder.Sql(sql);

        }
    }
}
