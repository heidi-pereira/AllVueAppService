using Microsoft.EntityFrameworkCore.Migrations;

namespace BrandVue.EntityFramework.Migrations
{
    public partial class MinorImprovementToVarCodeWordles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER PROCEDURE[dbo].[GetWeightedWordCounts] 
    @brandId int,
    @profileWeightings ProfileWeightingLookup READONLY,
    @productName nvarchar(50),
    @textFieldName nvarchar(255),
	@brandFieldLocation nvarchar(50) = 'varcode'
AS
BEGIN

DECLARE @sql NVARCHAR(MAX)

IF (@brandId IS NOT NULL) BEGIN

IF (@brandFieldLocation = 'CH1' OR @brandFieldLocation = 'CH2' OR @brandFieldLocation = 'optValue') BEGIN
SET @sql =
'
;WITH textResponses AS
(
    SELECT 
           responseId,
           RTRIM(LTRIM(LOWER(text))) text
    FROM   Data_' + @productName + ' WITH (NOLOCK)
    WHERE  varcode = ''' + @textFieldName + ''' AND text != '''' AND '+ @brandFieldLocation+'='+STR(@brandId,10)+'
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
'

END
ELSE
IF (@brandFieldLocation = 'varcode') BEGIN
 
SET @sql =
'
;WITH textResponses AS
(
    SELECT 
           responseId,
           RIGHT(varcode, Charindex(''_'', Reverse(varcode) + ''_'') - 1) brandId, 
           RTRIM(LTRIM(LOWER(text))) text
    FROM   Data_' + @productName + ' WITH (NOLOCK)
    WHERE  varcode LIKE ''' + @textFieldName + '_%'' AND text != ''''
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
