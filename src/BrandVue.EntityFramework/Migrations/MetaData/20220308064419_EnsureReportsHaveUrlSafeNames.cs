#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    public partial class EnsureReportsHaveUrlSafeNames : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // create a function to remove bad characters
            migrationBuilder.Sql(@"
CREATE FUNCTION [dbo].[fnRemoveBadUrlCharacters] (@string VARCHAR(MAX))
RETURNS VARCHAR(MAX)
BEGIN DECLARE @str VARCHAR(MAX) = REPLACE(LOWER(@string),']','');
	DECLARE @BadUrlCharactersPattern VARCHAR (MAX) = '%[?#[[@!$''()*+,;=%\\]%';
	WHILE PATINDEX(@BadUrlCharactersPattern,@str) > 0 BEGIN SELECT @str = STUFF(@str, PATINDEX(@BadUrlCharactersPattern,@str),1,'')
END RETURN @str END
");

            // create a function to replace spaces and colons, also calling the first function
            migrationBuilder.Sql(@"
CREATE FUNCTION [dbo].[fnReplaceInvalidChars] (@string VARCHAR(MAX))
RETURNS VARCHAR(MAX)
BEGIN DECLARE @str VARCHAR(MAX) = LOWER(@string);
	DECLARE @ReplaceWithHyphensPattern VARCHAR (MAX) = '%[ :]%';
	WHILE PATINDEX(@ReplaceWithHyphensPattern,@str) > 0 BEGIN SELECT @str = STUFF(@str, PATINDEX(@ReplaceWithHyphensPattern,@str),1,'-')
END RETURN [dbo].[fnRemoveBadUrlCharacters](REPLACE(REPLACE(@str,'&','and'),'/','or')) END
");

            // use function to update page name column on panes
            migrationBuilder.Sql(@"
UPDATE [dbo].[Panes]
SET [dbo].[Panes].PageName = [dbo].[fnReplaceInvalidChars]([dbo].[Pages].Name)
FROM [dbo].[Panes]
	INNER JOIN [dbo].[Pages] ON [dbo].[Pages].ProductShortCode = [dbo].[Panes].ProductShortCode AND [dbo].[Pages].SubProductId = [dbo].[Panes].SubProductId AND [dbo].[Pages].Name = [dbo].[Panes].PageName
	INNER JOIN [Reports].[SavedReports] ON [dbo].[Pages].Id = [Reports].[SavedReports].ReportPageId
");

            // use function to update report page names
            migrationBuilder.Sql(@"
UPDATE [dbo].[Pages]
SET [dbo].[Pages].Name = [dbo].[fnReplaceInvalidChars]([dbo].[Pages].Name)
FROM [dbo].[Pages]
    INNER JOIN [Reports].[SavedReports] ON [dbo].[Pages].Id = [Reports].[SavedReports].ReportPageId
");

            // remove function
            migrationBuilder.Sql(@"
DROP FUNCTION [dbo].[fnReplaceInvalidChars]
DROP FUNCTION [dbo].[fnRemoveBadUrlCharacters]
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
