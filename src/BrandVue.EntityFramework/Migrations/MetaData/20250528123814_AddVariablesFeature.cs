using BrandVue.EntityFramework.MetaData.Authorisation;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class AddVariablesFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DECLARE @featureId INT;

                -- Insert 'Variables' into PermissionFeatures
                INSERT INTO [UserFeaturePermissions].[PermissionFeatures] ([Name], [SystemKey])
                VALUES ('Variables', {(int)SystemKey.AllVue});

                -- Get the ID of the inserted feature
                SET @featureId = SCOPE_IDENTITY();

                -- Insert permission options for 'Variables'
                INSERT INTO [UserFeaturePermissions].[PermissionOptions] ([Name], [FeatureId])
                VALUES 
                    ('view', @featureId),
                    ('create', @featureId),
                    ('edit', @featureId),
                    ('delete', @featureId);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [UserFeaturePermissions].[PermissionOptions]
                WHERE [FeatureId] IN (
                    SELECT [Id] FROM [UserFeaturePermissions].[PermissionFeatures] WHERE [Name] = 'Variables'
                );

                DELETE FROM [UserFeaturePermissions].[PermissionFeatures]
                WHERE [Name] = 'Variables';
            ");
        }
    }
}
