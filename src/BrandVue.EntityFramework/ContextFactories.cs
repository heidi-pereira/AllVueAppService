using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.ResponseRepository;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace BrandVue.EntityFramework
{
    public abstract class DataContextFactory<T> : IDesignTimeDbContextFactory<T>, IDbContextFactory<T> where T: DbContext
    {
        private static object _migrationLock = new object();
        private static bool _migrated;
        private readonly ILogger _logger;
        private readonly string _connectionString;
        private readonly bool _isAppOnDeploymentBranch;

        protected DataContextFactory(ILogger logger, string connectionString, bool isAppOnDeploymentBranch)
        {
            _logger = logger;
            _connectionString = connectionString;
            _isAppOnDeploymentBranch = isAppOnDeploymentBranch;
        }

        private T CreateDbContext(int? commandMinimumSecondsTimeoutOverride = null)
        {
            var builder = new DbContextOptionsBuilder<T>();
            var connectionString = _connectionString;
            if (commandMinimumSecondsTimeoutOverride.HasValue)
            {
                var connectionStringBuilder = new SqlConnectionStringBuilder(_connectionString);
                connectionStringBuilder.CommandTimeout = Math.Max(commandMinimumSecondsTimeoutOverride.Value, connectionStringBuilder.CommandTimeout);
                connectionString = connectionStringBuilder.ToString();
            }
            builder.UseSqlServer(connectionString);

            var ctx = (T)Activator.CreateInstance(typeof(T), builder.Options);
            return ctx;
        }

        protected void EnsureMigrated()
        {
            if (_migrated || _isAppOnDeploymentBranch)
            {
                return;
            }

            lock (_migrationLock)
            {
                if (!_migrated)
                {
                    try
                    {
                        using (var ctx = CreateDbContext(120))
                        {
                            ctx.Database.Migrate();
                        }
                        _migrated = true;
                    }
                    catch (SqlException exception)
                    {
                        _logger?.LogError(exception, "Error apply any migrations");
                        throw;
                    }
                }
            }
        }

        // This is used at design time, for Add-Migration/Remove-Migration to avoid having to hard-code connection strings into the
        // class library's DbContext. Once we move to Asp.Net Core, things as can use the IServiceCollection there.
        T IDesignTimeDbContextFactory<T>.CreateDbContext(string[] args)
        {
            return CreateDbContext();
        }

        T IDbContextFactory<T>.CreateDbContext()
        {
            EnsureMigrated();
            return CreateDbContext();
        }
    }

    public class ResponseDataContextFactory : DataContextFactory<ResponseDataContext>
    {
        public ResponseDataContextFactory(ILoggerFactory loggerFactory, AppSettings appSettings) : base(loggerFactory.CreateLogger<ResponseDataContextFactory>(), appSettings.ConnectionString, appSettings.IsAppOnDeploymentBranch)
        {
        }

        public ResponseDataContextFactory(ILoggerFactory loggerFactory, string connectionString, bool isOnDeploymentBranch) : base(loggerFactory.CreateLogger<ResponseDataContextFactory>(), connectionString, isOnDeploymentBranch)
        {
        }

        //Used by EF migrations
        public ResponseDataContextFactory() : this(new AppSettings())
        {
        }

        public ResponseDataContextFactory(AppSettings appSettings) : base(null, appSettings.ConnectionString, appSettings.IsAppOnDeploymentBranch)
        {
        }
    }

    public class MetaDataContextFactory : DataContextFactory<MetaDataContext>
    {
        //Used by EF migrations
        public MetaDataContextFactory() : this(null, new AppSettings())
        {
        }

        public MetaDataContextFactory(ILoggerFactory loggerFactory, IMetaDataFactoryConfiguration appSettings) : base(loggerFactory?.CreateLogger<ResponseDataContextFactory>(), appSettings.MetaConnectionString, appSettings.IsAppOnDeploymentBranch)
        {
        }
    }
}