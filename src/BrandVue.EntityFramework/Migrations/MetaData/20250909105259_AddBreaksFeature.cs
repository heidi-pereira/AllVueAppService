using Microsoft.EntityFrameworkCore.Migrations;
using BrandVue.EntityFramework.MetaData.Authorisation;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddBreaksFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DECLARE @breaksFeatureId INT;

                -- Insert 'Breaks' into PermissionFeatures
                INSERT INTO [UserFeaturePermissions].[PermissionFeatures] ([Name], [SystemKey])
                VALUES ('Breaks', {(int)SystemKey.AllVue});

                -- Get the ID of the inserted Breaks feature
                SET @breaksFeatureId = SCOPE_IDENTITY();

                -- Enable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] ON;

                -- Insert permission options for 'Breaks' with hardcoded IDs
                INSERT INTO [UserFeaturePermissions].[PermissionOptions] ([Id], [Name], [FeatureId])
                VALUES 
                    (10, 'add', @breaksFeatureId),
                    (11, 'edit', @breaksFeatureId),
                    (12, 'view', @breaksFeatureId),
                    (13, 'delete', @breaksFeatureId);

                -- Disable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] OFF;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                -- Delete permission options with IDs 10-13
                DELETE FROM [UserFeaturePermissions].[PermissionOptions]
                WHERE [Id] IN (10, 11, 12, 13);

                -- Delete 'Breaks' feature
                DELETE FROM [UserFeaturePermissions].[PermissionFeatures]
                WHERE [Name] = 'Breaks' AND [SystemKey] = {(int)SystemKey.AllVue};
            ");
        }
    }
}
