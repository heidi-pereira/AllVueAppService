using BrandVue.EntityFramework.MetaData.Authorisation;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddDataTabAccessFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DECLARE @dataFeatureId INT;

                -- Insert 'Data' into PermissionFeatures
                INSERT INTO [UserFeaturePermissions].[PermissionFeatures] ([Name], [SystemKey])
                VALUES ('Data', {(int)SystemKey.AllVue});

                -- Get the ID of the inserted Data feature
                SET @dataFeatureId = SCOPE_IDENTITY();

                -- Enable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] ON;

                -- Insert permission option for 'Data' with hardcoded ID = 9
                INSERT INTO [UserFeaturePermissions].[PermissionOptions] ([Id], [Name], [FeatureId])
                VALUES (9, 'access', @dataFeatureId);

                -- Disable IDENTITY_INSERT for PermissionOptions
                SET IDENTITY_INSERT [UserFeaturePermissions].[PermissionOptions] OFF;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                -- Delete permission option with ID 9
                DELETE FROM [UserFeaturePermissions].[PermissionOptions]
                WHERE [Id] = 9;

                -- Delete 'Data' feature
                DELETE FROM [UserFeaturePermissions].[PermissionFeatures]
                WHERE [Name] = 'Data' AND [SystemKey] = {(int)SystemKey.AllVue};
            ");
        }
    }
}
