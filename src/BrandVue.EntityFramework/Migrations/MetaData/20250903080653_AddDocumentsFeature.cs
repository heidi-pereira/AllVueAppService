using BrandVue.EntityFramework.MetaData.Authorisation;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddDocumentsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DECLARE @documentsFeatureId INT;

                -- Insert 'Documents' into PermissionFeatures
                INSERT INTO [UserFeaturePermissions].[PermissionFeatures] ([Name], [SystemKey])
                VALUES ('Documents', {(int)SystemKey.AllVue});

                -- Get the ID of the inserted Documents feature
                SET @documentsFeatureId = SCOPE_IDENTITY();

                -- Enable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] ON;

                -- Insert permission option for 'Documents' with hardcoded ID = 6
                INSERT INTO [UserFeaturePermissions].[PermissionOptions] ([Id], [Name], [FeatureId])
                VALUES (6, 'access', @documentsFeatureId);

                -- Disable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] OFF;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Delete permission option with ID 6
                DELETE FROM [UserFeaturePermissions].[PermissionOptions]
                WHERE [Id] = 6;

                -- Delete 'Documents' feature
                DELETE FROM [UserFeaturePermissions].[PermissionFeatures]
                WHERE [Name] = 'Documents' AND [SystemKey] = 1;
            ");
        }
    }
}
