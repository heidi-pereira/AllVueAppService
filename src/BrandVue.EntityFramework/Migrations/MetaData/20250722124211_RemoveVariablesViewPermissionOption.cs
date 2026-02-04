using BrandVue.EntityFramework.MetaData.Authorisation;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class RemoveVariablesViewPermissionOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DECLARE @variableFeatureId INT;                
                DECLARE @viewPermissionOptionId INT;

                SELECT @variableFeatureId = [Id]
                FROM [UserFeaturePermissions].[PermissionFeatures]
                WHERE [Name] = 'Variables'

                SELECT @viewPermissionOptionId = [Id]
                FROM [UserFeaturePermissions].[PermissionOptions]
                WHERE [Name] = 'view'
                AND [FeatureId] = @variableFeatureId

                -- Delete any role mappings that are using the option
                DELETE FROM [UserFeaturePermissions].[RolePermissionOption]
                WHERE [OptionsId] = @viewPermissionOptionId

                -- Get any roles that are left with no options
                CREATE TABLE #RoleIdsToDelete (Id INT);
                
                INSERT INTO #RoleIdsToDelete (Id)
                SELECT r.Id
                FROM [UserFeaturePermissions].[Roles] r
                LEFT OUTER JOIN [UserFeaturePermissions].[RolePermissionOption] rpo
                    ON r.Id = rpo.RolesId
                WHERE rpo.RolesId IS NULL;
                
                -- Delete any user role mappings where the role has no options left
                DELETE FROM UserFeaturePermissions.UserFeaturePermissions
                WHERE UserRoleId IN (SELECT Id FROM #RoleIdsToDelete)
                
                -- Delete the roles left with no options
                DELETE FROM [UserFeaturePermissions].[Roles]
                WHERE Id IN (SELECT Id FROM #RoleIdsToDelete)

                -- Delete the option
                DELETE FROM [UserFeaturePermissions].[PermissionOptions]
                WHERE [Id] = @viewPermissionOptionId
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DECLARE @variableFeatureId INT;

                SELECT @variableFeatureId = [Id]
                FROM [UserFeaturePermissions].[PermissionFeatures]
                WHERE [Name] = 'Variables';

                IF @variableFeatureId IS NOT NULL
                BEGIN
                    INSERT INTO [UserFeaturePermissions].[PermissionOptions] ([Name], [FeatureId])
                    VALUES ('view', @variableFeatureId);
                END
                ELSE
                BEGIN
                    PRINT 'The Variables feature does not exist. Skipping the re-insertion of the view permission option.';
                END
    ");
        }
    }
}
