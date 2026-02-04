using Microsoft.EntityFrameworkCore.Migrations;

namespace BrandVue.EntityFramework.Migrations
{
    public partial class InitialMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TYPE [dbo].[ProfileWeightingLookup] AS TABLE(
    [ResponseId] [bigint] NOT NULL,
    [Weighting] [real] NOT NULL,
    PRIMARY KEY CLUSTERED 
(
    [ResponseId] ASC
)WITH (IGNORE_DUP_KEY = OFF)
)
");

            migrationBuilder.Sql(@"
CREATE PROCEDURE [dbo].[GetWeightedWordCounts] 
    @brandId int,
    @profileWeightings ProfileWeightingLookup READONLY,
    @productName nvarchar(50),
    @textFieldName nvarchar(255)
AS
BEGIN

declare @sql nvarchar(max) =
'
;WITH textResponses AS
(
    SELECT 
           responseId,
           RIGHT(varcode, Charindex(''_'', Reverse(varcode) + ''_'') - 1) brandId, 
           RTRIM(LTRIM(LOWER(text))) text
    FROM   Data_' + @productName + ' WITH (NOLOCK)
    WHERE  varcode LIKE ''' + @textFieldName + '%'' AND text != ''''
)
SELECT
    text Text, CAST(SUM(w.weighting) AS REAL) Result
FROM 
    textResponses t 
INNER JOIN 
    @profileWeightings w ON t.responseId = w.responseId
WHERE
    brandId = @brandId
GROUP BY 
    text
ORDER BY
    COUNT(*) DESC
';

    exec sp_executesql  @sql, N'@profileWeightings ProfileWeightingLookup READONLY, @brandId INT', @profileWeightings, @brandId

END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE [dbo].[GetWeightedWordCounts]");
            migrationBuilder.Sql("DROP TYPE [dbo].[ProfileWeightingLookup]");
        }
    }
}
