using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class UserPermissionsUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AllVueFilter_AllVueRule_AllVueRuleId",
                schema: "UserDataPermissions",
                table: "AllVueFilter");

            migrationBuilder.DropForeignKey(
                name: "FK_AllVueRule_BaseRule_Id",
                schema: "UserDataPermissions",
                table: "AllVueRule");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionOption_PermissionFeature_FeatureId",
                schema: "UserFeaturePermissions",
                table: "PermissionOption");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissionOption_PermissionOption_OptionsId",
                schema: "UserFeaturePermissions",
                table: "RolePermissionOption");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissionOption_Role_RolesId",
                schema: "UserFeaturePermissions",
                table: "RolePermissionOption");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDataPermission_BaseRule_RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermission");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFeaturePermission_Role_UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFeaturePermission",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserDataPermission",
                schema: "UserDataPermissions",
                table: "UserDataPermission")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Role",
                schema: "UserFeaturePermissions",
                table: "Role")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PermissionOption",
                schema: "UserFeaturePermissions",
                table: "PermissionOption");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PermissionFeature",
                schema: "UserFeaturePermissions",
                table: "PermissionFeature");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BaseRule",
                schema: "UserDataPermissions",
                table: "BaseRule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AllVueRule",
                schema: "UserDataPermissions",
                table: "AllVueRule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AllVueFilter",
                schema: "UserDataPermissions",
                table: "AllVueFilter");

            migrationBuilder.DropColumn(
                name: "Product",
                schema: "UserDataPermissions",
                table: "AllVueRule");

            migrationBuilder.RenameTable(
                name: "UserFeaturePermission",
                schema: "UserFeaturePermissions",
                newName: "UserFeaturePermissions",
                newSchema: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions");

            migrationBuilder.RenameTable(
                name: "UserDataPermission",
                schema: "UserDataPermissions",
                newName: "UserDataPermissions",
                newSchema: "UserDataPermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions");

            migrationBuilder.RenameTable(
                name: "Role",
                schema: "UserFeaturePermissions",
                newName: "Roles",
                newSchema: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions");

            migrationBuilder.RenameTable(
                name: "PermissionOption",
                schema: "UserFeaturePermissions",
                newName: "PermissionOptions",
                newSchema: "UserFeaturePermissions");

            migrationBuilder.RenameTable(
                name: "PermissionFeature",
                schema: "UserFeaturePermissions",
                newName: "PermissionFeatures",
                newSchema: "UserFeaturePermissions");

            migrationBuilder.RenameTable(
                name: "BaseRule",
                schema: "UserDataPermissions",
                newName: "BaseRules",
                newSchema: "UserDataPermissions");

            migrationBuilder.RenameTable(
                name: "AllVueRule",
                schema: "UserDataPermissions",
                newName: "AllVueRules",
                newSchema: "UserDataPermissions");

            migrationBuilder.RenameTable(
                name: "AllVueFilter",
                schema: "UserDataPermissions",
                newName: "AllVueFilters",
                newSchema: "UserDataPermissions");

            migrationBuilder.RenameIndex(
                name: "IX_UserFeaturePermission_UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                newName: "IX_UserFeaturePermissions_UserRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserDataPermission_UserId_RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                newName: "IX_UserDataPermissions_UserId_RuleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserDataPermission_RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                newName: "IX_UserDataPermissions_RuleId");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionOption_FeatureId",
                schema: "UserFeaturePermissions",
                table: "PermissionOptions",
                newName: "IX_PermissionOptions_FeatureId");

            migrationBuilder.RenameIndex(
                name: "IX_AllVueFilter_AllVueRuleId",
                schema: "UserDataPermissions",
                table: "AllVueFilters",
                newName: "IX_AllVueFilters_AllVueRuleId");

            migrationBuilder.AlterTable(
                name: "UserFeaturePermissions",
                schema: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterTable(
                name: "UserDataPermissions",
                schema: "UserDataPermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterTable(
                name: "Roles",
                schema: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedByUserId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysStartTime",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysEndTime",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedByUserId",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysStartTime",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysEndTime",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedByUserId",
                schema: "UserFeaturePermissions",
                table: "Roles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysStartTime",
                schema: "UserFeaturePermissions",
                table: "Roles",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysEndTime",
                schema: "UserFeaturePermissions",
                table: "Roles",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "RoleName",
                schema: "UserFeaturePermissions",
                table: "Roles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "Organisation",
                schema: "UserFeaturePermissions",
                table: "Roles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "UserFeaturePermissions",
                table: "Roles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "Organisation",
                schema: "UserDataPermissions",
                table: "AllVueRules",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "AllUserAccessForSubProduct",
                schema: "UserDataPermissions",
                table: "AllVueRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                type: "datetime2",
                nullable: false,
                computedColumnSql: "SysStartTime",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComputedColumnSql: "SysStartTime")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                type: "datetime2",
                nullable: false,
                computedColumnSql: "SysStartTime",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComputedColumnSql: "SysStartTime")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "UserFeaturePermissions",
                table: "Roles",
                type: "datetime2",
                nullable: false,
                computedColumnSql: "SysStartTime",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComputedColumnSql: "SysStartTime")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserFeaturePermissions",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserDataPermissions",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Roles",
                schema: "UserFeaturePermissions",
                table: "Roles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PermissionOptions",
                schema: "UserFeaturePermissions",
                table: "PermissionOptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PermissionFeatures",
                schema: "UserFeaturePermissions",
                table: "PermissionFeatures",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BaseRules",
                schema: "UserDataPermissions",
                table: "BaseRules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AllVueRules",
                schema: "UserDataPermissions",
                table: "AllVueRules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AllVueFilters",
                schema: "UserDataPermissions",
                table: "AllVueFilters",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AllVueRules_Organisation_SubProduct_AllUserAccessForSubProduct",
                schema: "UserDataPermissions",
                table: "AllVueRules",
                columns: new[] { "Organisation", "SubProduct", "AllUserAccessForSubProduct" },
                unique: true,
                filter: "[AllUserAccessForSubProduct] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_AllVueFilters_AllVueRules_AllVueRuleId",
                schema: "UserDataPermissions",
                table: "AllVueFilters",
                column: "AllVueRuleId",
                principalSchema: "UserDataPermissions",
                principalTable: "AllVueRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AllVueRules_BaseRules_Id",
                schema: "UserDataPermissions",
                table: "AllVueRules",
                column: "Id",
                principalSchema: "UserDataPermissions",
                principalTable: "BaseRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionOptions_PermissionFeatures_FeatureId",
                schema: "UserFeaturePermissions",
                table: "PermissionOptions",
                column: "FeatureId",
                principalSchema: "UserFeaturePermissions",
                principalTable: "PermissionFeatures",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissionOption_PermissionOptions_OptionsId",
                schema: "UserFeaturePermissions",
                table: "RolePermissionOption",
                column: "OptionsId",
                principalSchema: "UserFeaturePermissions",
                principalTable: "PermissionOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissionOption_Roles_RolesId",
                schema: "UserFeaturePermissions",
                table: "RolePermissionOption",
                column: "RolesId",
                principalSchema: "UserFeaturePermissions",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserDataPermissions_BaseRules_RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermissions",
                column: "RuleId",
                principalSchema: "UserDataPermissions",
                principalTable: "BaseRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFeaturePermissions_Roles_UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions",
                column: "UserRoleId",
                principalSchema: "UserFeaturePermissions",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AllVueFilters_AllVueRules_AllVueRuleId",
                schema: "UserDataPermissions",
                table: "AllVueFilters");

            migrationBuilder.DropForeignKey(
                name: "FK_AllVueRules_BaseRules_Id",
                schema: "UserDataPermissions",
                table: "AllVueRules");

            migrationBuilder.DropForeignKey(
                name: "FK_PermissionOptions_PermissionFeatures_FeatureId",
                schema: "UserFeaturePermissions",
                table: "PermissionOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissionOption_PermissionOptions_OptionsId",
                schema: "UserFeaturePermissions",
                table: "RolePermissionOption");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissionOption_Roles_RolesId",
                schema: "UserFeaturePermissions",
                table: "RolePermissionOption");

            migrationBuilder.DropForeignKey(
                name: "FK_UserDataPermissions_BaseRules_RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserFeaturePermissions_Roles_UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserFeaturePermissions",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserDataPermissions",
                schema: "UserDataPermissions",
                table: "UserDataPermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Roles",
                schema: "UserFeaturePermissions",
                table: "Roles")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PermissionOptions",
                schema: "UserFeaturePermissions",
                table: "PermissionOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PermissionFeatures",
                schema: "UserFeaturePermissions",
                table: "PermissionFeatures");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BaseRules",
                schema: "UserDataPermissions",
                table: "BaseRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AllVueRules",
                schema: "UserDataPermissions",
                table: "AllVueRules");

            migrationBuilder.DropIndex(
                name: "IX_AllVueRules_Organisation_SubProduct_AllUserAccessForSubProduct",
                schema: "UserDataPermissions",
                table: "AllVueRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AllVueFilters",
                schema: "UserDataPermissions",
                table: "AllVueFilters");

            migrationBuilder.DropColumn(
                name: "AllUserAccessForSubProduct",
                schema: "UserDataPermissions",
                table: "AllVueRules");

            migrationBuilder.RenameTable(
                name: "UserFeaturePermissions",
                schema: "UserFeaturePermissions",
                newName: "UserFeaturePermission",
                newSchema: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions");

            migrationBuilder.RenameTable(
                name: "UserDataPermissions",
                schema: "UserDataPermissions",
                newName: "UserDataPermission",
                newSchema: "UserDataPermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "UserFeaturePermissions",
                newName: "Role",
                newSchema: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions");

            migrationBuilder.RenameTable(
                name: "PermissionOptions",
                schema: "UserFeaturePermissions",
                newName: "PermissionOption",
                newSchema: "UserFeaturePermissions");

            migrationBuilder.RenameTable(
                name: "PermissionFeatures",
                schema: "UserFeaturePermissions",
                newName: "PermissionFeature",
                newSchema: "UserFeaturePermissions");

            migrationBuilder.RenameTable(
                name: "BaseRules",
                schema: "UserDataPermissions",
                newName: "BaseRule",
                newSchema: "UserDataPermissions");

            migrationBuilder.RenameTable(
                name: "AllVueRules",
                schema: "UserDataPermissions",
                newName: "AllVueRule",
                newSchema: "UserDataPermissions");

            migrationBuilder.RenameTable(
                name: "AllVueFilters",
                schema: "UserDataPermissions",
                newName: "AllVueFilter",
                newSchema: "UserDataPermissions");

            migrationBuilder.RenameIndex(
                name: "IX_UserFeaturePermissions_UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                newName: "IX_UserFeaturePermission_UserRoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserDataPermissions_UserId_RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                newName: "IX_UserDataPermission_UserId_RuleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserDataPermissions_RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                newName: "IX_UserDataPermission_RuleId");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionOptions_FeatureId",
                schema: "UserFeaturePermissions",
                table: "PermissionOption",
                newName: "IX_PermissionOption_FeatureId");

            migrationBuilder.RenameIndex(
                name: "IX_AllVueFilters_AllVueRuleId",
                schema: "UserDataPermissions",
                table: "AllVueFilter",
                newName: "IX_AllVueFilter_AllVueRuleId");

            migrationBuilder.AlterTable(
                name: "UserFeaturePermission",
                schema: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterTable(
                name: "UserDataPermission",
                schema: "UserDataPermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterTable(
                name: "Role",
                schema: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedByUserId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysStartTime",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysEndTime",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedByUserId",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysStartTime",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysEndTime",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedByUserId",
                schema: "UserFeaturePermissions",
                table: "Role",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysStartTime",
                schema: "UserFeaturePermissions",
                table: "Role",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SysEndTime",
                schema: "UserFeaturePermissions",
                table: "Role",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "RoleName",
                schema: "UserFeaturePermissions",
                table: "Role",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "Organisation",
                schema: "UserFeaturePermissions",
                table: "Role",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450)
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                schema: "UserFeaturePermissions",
                table: "Role",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<string>(
                name: "Organisation",
                schema: "UserDataPermissions",
                table: "AllVueRule",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddColumn<string>(
                name: "Product",
                schema: "UserDataPermissions",
                table: "AllVueRule",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                type: "datetime2",
                nullable: false,
                computedColumnSql: "SysStartTime",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComputedColumnSql: "SysStartTime")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                type: "datetime2",
                nullable: false,
                computedColumnSql: "SysStartTime",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComputedColumnSql: "SysStartTime")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionsHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedDate",
                schema: "UserFeaturePermissions",
                table: "Role",
                type: "datetime2",
                nullable: false,
                computedColumnSql: "SysStartTime",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldComputedColumnSql: "SysStartTime")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                .OldAnnotation("SqlServer:IsTemporal", true)
                .OldAnnotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .OldAnnotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .OldAnnotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .OldAnnotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserFeaturePermission",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserDataPermission",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Role",
                schema: "UserFeaturePermissions",
                table: "Role",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PermissionOption",
                schema: "UserFeaturePermissions",
                table: "PermissionOption",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PermissionFeature",
                schema: "UserFeaturePermissions",
                table: "PermissionFeature",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BaseRule",
                schema: "UserDataPermissions",
                table: "BaseRule",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AllVueRule",
                schema: "UserDataPermissions",
                table: "AllVueRule",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AllVueFilter",
                schema: "UserDataPermissions",
                table: "AllVueFilter",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AllVueFilter_AllVueRule_AllVueRuleId",
                schema: "UserDataPermissions",
                table: "AllVueFilter",
                column: "AllVueRuleId",
                principalSchema: "UserDataPermissions",
                principalTable: "AllVueRule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AllVueRule_BaseRule_Id",
                schema: "UserDataPermissions",
                table: "AllVueRule",
                column: "Id",
                principalSchema: "UserDataPermissions",
                principalTable: "BaseRule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PermissionOption_PermissionFeature_FeatureId",
                schema: "UserFeaturePermissions",
                table: "PermissionOption",
                column: "FeatureId",
                principalSchema: "UserFeaturePermissions",
                principalTable: "PermissionFeature",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissionOption_PermissionOption_OptionsId",
                schema: "UserFeaturePermissions",
                table: "RolePermissionOption",
                column: "OptionsId",
                principalSchema: "UserFeaturePermissions",
                principalTable: "PermissionOption",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissionOption_Role_RolesId",
                schema: "UserFeaturePermissions",
                table: "RolePermissionOption",
                column: "RolesId",
                principalSchema: "UserFeaturePermissions",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserDataPermission_BaseRule_RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                column: "RuleId",
                principalSchema: "UserDataPermissions",
                principalTable: "BaseRule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserFeaturePermission_Role_UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                column: "UserRoleId",
                principalSchema: "UserFeaturePermissions",
                principalTable: "Role",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
