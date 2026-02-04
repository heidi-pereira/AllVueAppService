using System.Linq;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddParentGroupNameColumnToSubset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentGroupName",
                table: "SubsetConfigurations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("UPDATE SubsetConfigurations SET ParentGroupName='UK' WHERE ProductShortCode='brandvue'");

            migrationBuilder.Sql("INSERT INTO SubsetConfigurations (Identifier, DisplayName, DisplayNameShort, Iso2LetterCountryCode, Description, [Order], Disabled, SurveyIdToAllowedSegmentNames, EnableRawDataApiAccess, ProductShortCode, SubProductId, Alias, OverriddenStartDate, AlwaysShowDataUpToCurrentDate, ParentGroupName) " +
                                 "SELECT " +
                                 "  Identifier + '-US' AS Identifier," +
                                 "  DisplayName," +
                                 "  DisplayNameShort," +
                                 "  'US' AS Iso2LetterCountryCode," +
                                 "  Description," +
                                 "  [Order]," +
                                 "  1," +
                                 "  NULL AS SurveyIdToAllowedSegmentNames," +
                                 "  EnableRawDataApiAccess," +
                                 "  ProductShortCode," +
                                 "  SubProductId," +
                                 "  Alias + '-US' AS Alias," +
                                 "  OverriddenStartDate," +
                                 "  AlwaysShowDataUpToCurrentDate, " +
                                 "  'US' AS ParentGroupName " +
                                 "FROM SubsetConfigurations " +
                                 "WHERE ProductShortCode='brandvue'");

            var surveyForSubset = new Dictionary<string, string>
            {
                { "FMCG-US", "25567" },
                { "BPC-US", "25577" },
                { "HS-US", "25602" },
                { "HT-US", "25582" },
                { "LR-US", "25589" },
                { "M-US", "25559" },
                { "PLG-US", "25529" },
                { "TT-US", "25599" },
                { "A-US", "25540" }
            };

            var sqlCommands = surveyForSubset
                .Select(s => "UPDATE SubsetConfigurations SET SurveyIdToAllowedSegmentNames = '{\""+s.Value+ "\":[]}' WHERE Identifier = '" + s.Key+"';");

            foreach (string command in sqlCommands)
            {
                migrationBuilder.Sql(command);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentGroupName",
                table: "SubsetConfigurations");
        }
    }
}
