using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandVue.EntityFramework.Migrations.MetaData
{
    /// <inheritdoc />
    public partial class RenameRoleOrganisationColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete existing data in the correct order to avoid foreign key constraint violations
            
            // 1. Delete UserFeaturePermissions first (references Roles via foreign key)
            migrationBuilder.Sql("DELETE FROM [UserFeaturePermissions].[UserFeaturePermissions]");
            
            // 2. Delete RolePermissionOption junction table (references Roles)
            migrationBuilder.Sql("DELETE FROM [UserFeaturePermissions].[RolePermissionOption]");
            
            // 3. Finally delete Roles table as the data is no longer valid after column rename
            migrationBuilder.Sql("DELETE FROM [UserFeaturePermissions].[Roles]");
            
            migrationBuilder.RenameColumn(
                name: "Organisation",
                schema: "UserFeaturePermissions",
                table: "Roles",
                newName: "OrganisationId")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrganisationId",
                schema: "UserFeaturePermissions",
                table: "Roles",
                newName: "Organisation")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "RolesHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", "UserFeaturePermissions")
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "SysEndTime")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "SysStartTime");
        }
    }
}
