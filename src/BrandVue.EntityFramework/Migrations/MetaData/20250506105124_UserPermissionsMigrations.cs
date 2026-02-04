using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class UserPermissionsMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "UserDataPermissions");

            migrationBuilder.EnsureSchema(
                name: "UserFeaturePermissions");

            migrationBuilder.CreateTable(
                name: "BaseRule",
                schema: "UserDataPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SystemKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaseRule", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionFeature",
                schema: "UserFeaturePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SystemKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionFeature", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "UserFeaturePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    Organisation = table.Column<string>(type: "nvarchar(max)", nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, computedColumnSql: "SysStartTime")
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    SysEndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    SysStartTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.CreateTable(
                name: "AllVueRule",
                schema: "UserDataPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Organisation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Product = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubProduct = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AvailableVariableIds = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllVueRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllVueRule_BaseRule_Id",
                        column: x => x.Id,
                        principalSchema: "UserDataPermissions",
                        principalTable: "BaseRule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserDataPermission",
                schema: "UserDataPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    RuleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, computedColumnSql: "SysStartTime")
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    SysEndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    SysStartTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDataPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDataPermission_BaseRule_RuleId",
                        column: x => x.RuleId,
                        principalSchema: "UserDataPermissions",
                        principalTable: "BaseRule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.CreateTable(
                name: "PermissionOption",
                schema: "UserFeaturePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FeatureId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PermissionOption_PermissionFeature_FeatureId",
                        column: x => x.FeatureId,
                        principalSchema: "UserFeaturePermissions",
                        principalTable: "PermissionFeature",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFeaturePermission",
                schema: "UserFeaturePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    UserRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, computedColumnSql: "SysStartTime")
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    SysEndTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime"),
                    SysStartTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:IsTemporal", true)
                        .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                        .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                        .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                        .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFeaturePermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFeaturePermission_Role_UserRoleId",
                        column: x => x.UserRoleId,
                        principalSchema: "UserFeaturePermissions",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.CreateTable(
                name: "AllVueFilter",
                schema: "UserDataPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllVueRuleId = table.Column<int>(type: "int", nullable: false),
                    VariableConfigurationId = table.Column<int>(type: "int", nullable: false),
                    EntitySetId = table.Column<int>(type: "int", nullable: false),
                    EntityIds = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllVueFilter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AllVueFilter_AllVueRule_AllVueRuleId",
                        column: x => x.AllVueRuleId,
                        principalSchema: "UserDataPermissions",
                        principalTable: "AllVueRule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissionOption",
                schema: "UserFeaturePermissions",
                columns: table => new
                {
                    OptionsId = table.Column<int>(type: "int", nullable: false),
                    RolesId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissionOption", x => new { x.OptionsId, x.RolesId });
                    table.ForeignKey(
                        name: "FK_RolePermissionOption_PermissionOption_OptionsId",
                        column: x => x.OptionsId,
                        principalSchema: "UserFeaturePermissions",
                        principalTable: "PermissionOption",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissionOption_Role_RolesId",
                        column: x => x.RolesId,
                        principalSchema: "UserFeaturePermissions",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllVueFilter_AllVueRuleId",
                schema: "UserDataPermissions",
                table: "AllVueFilter",
                column: "AllVueRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionOption_FeatureId",
                schema: "UserFeaturePermissions",
                table: "PermissionOption",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissionOption_RolesId",
                schema: "UserFeaturePermissions",
                table: "RolePermissionOption",
                column: "RolesId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDataPermission_RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDataPermission_UserId_RuleId",
                schema: "UserDataPermissions",
                table: "UserDataPermission",
                columns: new[] { "UserId", "RuleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserFeaturePermission_UserRoleId",
                schema: "UserFeaturePermissions",
                table: "UserFeaturePermission",
                column: "UserRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllVueFilter",
                schema: "UserDataPermissions");

            migrationBuilder.DropTable(
                name: "RolePermissionOption",
                schema: "UserFeaturePermissions");

            migrationBuilder.DropTable(
                name: "UserDataPermission",
                schema: "UserDataPermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserDataPermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserDataPermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropTable(
                name: "UserFeaturePermission",
                schema: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "UserFeaturePermissionHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropTable(
                name: "AllVueRule",
                schema: "UserDataPermissions");

            migrationBuilder.DropTable(
                name: "PermissionOption",
                schema: "UserFeaturePermissions");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "UserFeaturePermissions")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RoleHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");

            migrationBuilder.DropTable(
                name: "BaseRule",
                schema: "UserDataPermissions");

            migrationBuilder.DropTable(
                name: "PermissionFeature",
                schema: "UserFeaturePermissions");
        }
    }
}
