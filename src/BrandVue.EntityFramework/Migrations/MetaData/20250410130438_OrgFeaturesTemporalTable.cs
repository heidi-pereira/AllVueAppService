using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class OrgFeaturesTemporalTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE OrganisationFeatures SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql($"ALTER TABLE OrganisationFeatures DROP PERIOD FOR SYSTEM_TIME");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "OrganisationFeatures",
                type: "datetime2",
                nullable: false,
                computedColumnSql: "SysStartTime")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "OrganisationFeatures_History",
                type: "datetime2(0)",
                nullable: false);

            migrationBuilder.AlterTable(
                name: "OrganisationFeatures")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedByUserId",
                table: "OrganisationFeatures",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "OrganisationId",
                table: "OrganisationFeatures",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "FeatureId",
                table: "OrganisationFeatures",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "OrganisationFeatures",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:Identity", "1, 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE OrganisationFeatures SET (SYSTEM_VERSIONING = OFF);");
            migrationBuilder.Sql($"ALTER TABLE OrganisationFeatures DROP PERIOD FOR SYSTEM_TIME");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "OrganisationFeatures")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");
            
            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "OrganisationFeatures_History");

            migrationBuilder.AlterTable(
                name: "OrganisationFeatures")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", null)
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedByUserId",
                table: "OrganisationFeatures",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", null)
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "OrganisationId",
                table: "OrganisationFeatures",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", null)
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "FeatureId",
                table: "OrganisationFeatures",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", null)
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "OrganisationFeatures",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "OrganisationFeatures_History")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", null)
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");
        }
    }
}
