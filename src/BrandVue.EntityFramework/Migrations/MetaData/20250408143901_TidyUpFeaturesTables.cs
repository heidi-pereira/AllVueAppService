using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class TidyUpFeaturesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganisationFeatures_Features_FeaturesId",
                table: "OrganisationFeatures");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFeatures_Features_FeaturesId",
                table: "UserFeatures");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "OrganisationFeatures");

            migrationBuilder.RenameColumn(
                name: "FeaturesId",
                table: "UserFeatures",
                newName: "FeatureId");

            migrationBuilder.RenameIndex(
                name: "IX_UserFeatures_FeaturesId_UserId",
                table: "UserFeatures",
                newName: "IX_UserFeatures_FeatureId_UserId");

            migrationBuilder.RenameColumn(
                name: "FeaturesId",
                table: "OrganisationFeatures",
                newName: "FeatureId");

            migrationBuilder.RenameIndex(
                name: "IX_OrganisationFeatures_FeaturesId_OrganisationId",
                table: "OrganisationFeatures",
                newName: "IX_OrganisationFeatures_FeatureId_OrganisationId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationFeatures_Features_FeatureId",
                table: "OrganisationFeatures",
                column: "FeatureId",
                principalTable: "Features",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFeatures_Features_FeatureId",
                table: "UserFeatures",
                column: "FeatureId",
                principalTable: "Features",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrganisationFeatures_Features_FeatureId",
                table: "OrganisationFeatures");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFeatures_Features_FeatureId",
                table: "UserFeatures");

            migrationBuilder.RenameColumn(
                name: "FeatureId",
                table: "UserFeatures",
                newName: "FeaturesId");

            migrationBuilder.RenameIndex(
                name: "IX_UserFeatures_FeatureId_UserId",
                table: "UserFeatures",
                newName: "IX_UserFeatures_FeaturesId_UserId");

            migrationBuilder.RenameColumn(
                name: "FeatureId",
                table: "OrganisationFeatures",
                newName: "FeaturesId");

            migrationBuilder.RenameIndex(
                name: "IX_OrganisationFeatures_FeatureId_OrganisationId",
                table: "OrganisationFeatures",
                newName: "IX_OrganisationFeatures_FeaturesId_OrganisationId");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "OrganisationFeatures",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddForeignKey(
                name: "FK_OrganisationFeatures_Features_FeaturesId",
                table: "OrganisationFeatures",
                column: "FeaturesId",
                principalTable: "Features",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFeatures_Features_FeaturesId",
                table: "UserFeatures",
                column: "FeaturesId",
                principalTable: "Features",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
