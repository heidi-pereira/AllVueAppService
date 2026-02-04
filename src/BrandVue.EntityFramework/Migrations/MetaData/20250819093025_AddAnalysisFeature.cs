using BrandVue.EntityFramework.MetaData.Authorisation;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddAnalysisFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DECLARE @analysisFeatureId INT;

                -- Insert 'Analysis' into PermissionFeatures
                INSERT INTO [UserFeaturePermissions].[PermissionFeatures] ([Name], [SystemKey])
                VALUES ('Analysis', {(int)SystemKey.AllVue});

                -- Get the ID of the inserted Analysis feature
                SET @analysisFeatureId = SCOPE_IDENTITY();

                -- Enable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] ON;

                -- Insert permission option for 'Analysis' with hardcoded ID = 5
                INSERT INTO [UserFeaturePermissions].[PermissionOptions] ([Id], [Name], [FeatureId])
                VALUES (5, 'access', @analysisFeatureId);

                -- Disable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] OFF;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Delete permission option with ID 5
                DELETE FROM [UserFeaturePermissions].[PermissionOptions]
                WHERE [Id] = 5;

                -- Delete 'Analysis' feature
                DELETE FROM [UserFeaturePermissions].[PermissionFeatures]
                WHERE [Name] = 'Analysis' AND [SystemKey] = 1;
            ");
        }
    }
}
