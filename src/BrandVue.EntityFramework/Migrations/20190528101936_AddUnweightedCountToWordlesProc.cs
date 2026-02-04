using Microsoft.EntityFrameworkCore.Migrations;

namespace BrandVue.EntityFramework.Migrations
{
    public partial class AddUnweightedCountToWordlesProc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER PROCEDURE [dbo].[GetWeightedWordCounts] 
    @brandId int,
    @profileWeightings ProfileWeightingLookup READONLY,
    @productName nvarchar(50),
    @textFieldName nvarchar(255)
AS
BEGIN

DECLARE @sql NVARCHAR(MAX)

IF (@brandId IS NOT NULL) BEGIN

SET @sql =
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
    text Text, CAST(SUM(w.weighting) AS REAL) Result, COUNT(Text) UnweightedResult
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

END

ELSE BEGIN
SET @sql =
'
;WITH textResponses AS
(
    SELECT 
           responseId,
           RTRIM(LTRIM(LOWER(text))) text
    FROM   Data_' + @productName + ' WITH (NOLOCK)
    WHERE  varcode = ''' + @textFieldName + ''' AND text != ''''
)
SELECT
    text Text, CAST(SUM(w.weighting) AS REAL) Result, COUNT(Text) UnweightedResult
FROM 
    textResponses t 
INNER JOIN 
    @profileWeightings w ON t.responseId = w.responseId
GROUP BY 
    text
ORDER BY
    COUNT(*) DESC
';
END

    exec sp_executesql  @sql, N'@profileWeightings ProfileWeightingLookup READONLY, @brandId INT', @profileWeightings, @brandId

END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            
        }
    }
}
