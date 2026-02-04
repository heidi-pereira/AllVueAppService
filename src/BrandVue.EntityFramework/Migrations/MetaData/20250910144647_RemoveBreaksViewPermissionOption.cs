using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class RemoveBreaksViewPermissionOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Delete the Breaks View permission option with ID 12
                DELETE FROM [UserFeaturePermissions].[PermissionOptions]
                WHERE [Id] = 12 AND [Name] = 'view';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DECLARE @breaksFeatureId INT;

                -- Get the ID of the Breaks feature
                SELECT @breaksFeatureId = [Id]
                FROM [UserFeaturePermissions].[PermissionFeatures]
                WHERE [Name] = 'Breaks';

                -- Enable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] ON;

                -- Re-insert the Breaks View permission option with ID 12
                INSERT INTO [UserFeaturePermissions].[PermissionOptions] ([Id], [Name], [FeatureId])
                VALUES (12, 'view', @breaksFeatureId);

                -- Disable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] OFF;
            ");
        }
    }
}
