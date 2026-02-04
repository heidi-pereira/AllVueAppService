using BrandVue.EntityFramework.MetaData.Authorisation;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddReportsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DECLARE @reportsFeatureId INT;

                -- Insert 'Reports' into PermissionFeatures
                INSERT INTO [UserFeaturePermissions].[PermissionFeatures] ([Name], [SystemKey])
                VALUES ('Reports', {(int)SystemKey.AllVue});

                -- Get the ID of the inserted Reports feature
                SET @reportsFeatureId = SCOPE_IDENTITY();

                -- Enable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] ON;

                -- Insert permission options for 'Reports' with hardcoded IDs
                INSERT INTO [UserFeaturePermissions].[PermissionOptions] ([Id], [Name], [FeatureId])
                VALUES 
                    (14, 'add/edit', @reportsFeatureId),
                    (15, 'view', @reportsFeatureId),
                    (16, 'delete', @reportsFeatureId);

                -- Disable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] OFF;
                ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                -- Delete permission options with IDs 14-16
                DELETE FROM [UserFeaturePermissions].[PermissionOptions]
                WHERE [Id] IN (14, 15, 16);
                -- Delete 'Reports' feature
                DELETE FROM [UserFeaturePermissions].[PermissionFeatures]
                WHERE [Name] = 'Reports' AND [SystemKey] = {(int)SystemKey.AllVue};
            ");
        }
    }
}
