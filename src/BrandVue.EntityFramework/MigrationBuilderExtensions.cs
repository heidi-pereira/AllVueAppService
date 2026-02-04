using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace BrandVue.EntityFramework
{
    public static class MigrationBuilderExtensions
    {
        [Obsolete("Use modelBuilder.Entity<T>(c => c.ToTable(tb => tb.IsTemporal())))")]
        public static void DropTemporalTable(this MigrationBuilder builder, string name)
        {
            builder.RemoveTemporalTableSupport(name);
            builder.DropTable(name: name);
        }

        [Obsolete("Use modelBuilder.Entity<T>(c => c.ToTable(tb => tb.IsTemporal())))")]
        public static CreateTableBuilder<TColumns> CreateTemporalTable<TColumns>(this MigrationBuilder builder, string name, Func<ColumnsBuilder, TColumns> columns, string schema = null, Action<CreateTableBuilder<TColumns>> constraints = null)
        {
            var createTableResult = builder.CreateTable(name, columns, schema, constraints);
            builder.AddTemporalTableSupport(name);
            return createTableResult;
        }

        public static void AddTemporalTableSupport(this MigrationBuilder builder, string tableName)
        {
            builder.AddTemporalTableSupport(tableName, $"{tableName}_History");
        }

        [Obsolete("Use modelBuilder.Entity<T>(c => c.ToTable(tb => tb.IsTemporal())))")]
        public static void AddTemporalTableSupport(this MigrationBuilder builder, string tableName, string historyTableName)
        {
            builder.Sql($@"ALTER TABLE [{tableName}] ADD 
                SysStartTime datetime2(0) GENERATED ALWAYS AS ROW START HIDDEN CONSTRAINT [DF_{tableName}_SysStart] DEFAULT SYSUTCDATETIME(),
                SysEndTime datetime2(0) GENERATED ALWAYS AS ROW END HIDDEN CONSTRAINT [DF_{tableName}_SysEnd] DEFAULT CONVERT(datetime2 (0), '9999-12-31 23:59:59'),
                PERIOD FOR SYSTEM_TIME (SysStartTime, SysEndTime);");
            builder.Sql($@"ALTER TABLE [{tableName}] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.[{historyTableName}] ));");
        }

        [Obsolete("Use modelBuilder.Entity<T>(c => c.ToTable(tb => tb.IsTemporal())))")]
        public static void RemoveTemporalTableSupport(this MigrationBuilder builder, string tableName)
        {
            builder.RemoveTemporalTableSupport(tableName, $"{tableName}_History");
        }

        [Obsolete("Use modelBuilder.Entity<T>(c => c.ToTable(tb => tb.IsTemporal())))")]
        public static void RemoveTemporalTableSupport(this MigrationBuilder builder, string tableName, string historyTableName)
        {
            builder.Sql($@"ALTER TABLE [{tableName}] SET (SYSTEM_VERSIONING = OFF);");
            builder.Sql($@"ALTER TABLE [{tableName}] DROP PERIOD FOR SYSTEM_TIME");
            builder.DropTable(name: historyTableName);
        }

        public static void DropStoredProcedure(this MigrationBuilder builder, string storedProcedureName)
        {
            builder.Sql($@"DROP PROCEDURE [{storedProcedureName}];");
        }
    }
}