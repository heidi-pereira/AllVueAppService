using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Respawn;
using Respawn.Graph;

namespace TestCommon
{
    public enum StorageType
    {
        InMemory,
        InMemoryTransactionless,
        LocallyInstalledDb,
        SqlServerExpressLocalDb
    }

    /// <summary>
    /// IMPORTANT: If this is set up in an NUnit TestFixture, the InMemory and InMemoryTransactionless storage types
    /// need to be new'd up in the [SetUp] method, whereas the LocallyInstalledDb and SqlServerExpressLocalDb storage
    /// types need to be new'd up in the [OneTimeSetUp] method.
    /// </summary>
    public interface ITestMetadataContextFactory : IDbContextFactory<MetaDataContext>
    {
        public static ITestMetadataContextFactory Create(StorageType storageType, string databaseName = "")
        {
            var testMetadataContextFactory = CreateTestMetadataContextFactory(storageType);
            testMetadataContextFactory.Initialise(databaseName).GetAwaiter().GetResult();
            return testMetadataContextFactory;
        }

        public static async Task<ITestMetadataContextFactory> CreateAsync(StorageType storageType, string databaseName = "")
        {
            var testMetadataContextFactory = CreateTestMetadataContextFactory(storageType);
            await testMetadataContextFactory.Initialise(databaseName);
            return testMetadataContextFactory;
        }

        private static ITestMetadataContextFactory CreateTestMetadataContextFactory(StorageType storageType)
        {
            ITestMetadataContextFactory testMetadataContextFactory = storageType switch
            {
                StorageType.InMemory => new TestMetadataContextFactoryInMemory(),
                StorageType.InMemoryTransactionless => new TestMetadataContextFactoryTransactionless(),
                StorageType.LocallyInstalledDb => new TestMetadataContextFactoryLocallyInstalledDb(),
                StorageType.SqlServerExpressLocalDb => new TestMetadataContextFactoryLocalDb(),
                _ => throw new ArgumentException("Invalid StorageType passed to ITestMetadataContextFactory.Create")
            };
            return testMetadataContextFactory;
        }

        async Task Initialise(string databaseName = "") { }

        async Task Dispose() { }

        async Task RevertDatabase() { }
    } 

    public class TestMetadataContextFactoryLocalDb : TestMetadataContextFactorySqlBase
    {
        protected override string ConnectionString { get; set; } = "Server=(localdb)\\MSSQLLocalDB;database=<DatabaseNameToUse>";
    }

    public class TestMetadataContextFactoryLocallyInstalledDb : TestMetadataContextFactorySqlBase
    {
        protected override string ConnectionString { get; set; } = "Server=.\\sql2022;Database=<DatabaseNameToUse>;TrustServerCertificate=True;Trusted_Connection=True;Integrated Security=True;MultipleActiveResultSets=true;Encrypt=True;";
    }

    public abstract class TestMetadataContextFactorySqlBase : ITestMetadataContextFactory
    {
        protected abstract string ConnectionString { get; set; }

        private Respawner _respawner;

        private string DatabaseName { get; set; } = string.Empty;

        private DbContextOptions<MetaDataContext> _dbContextOptions;

        public async Task Initialise(string databaseName = "")
        {
            DatabaseName = databaseName + "_" + System.Random.Shared.Next(999999);
            ConnectionString = ConnectionString.Replace("<DatabaseNameToUse>", DatabaseName);
            _dbContextOptions = new DbContextOptionsBuilder<MetaDataContext>().UseSqlServer(ConnectionString).Options;

            await using var dbContext = CreateDbContext();
            await dbContext.Database.MigrateAsync(); // This is needed to create the database and all the tables in the db context.

            // Initialize Respawner after the database has been migrated
            var historyTableList = GetHistoryTables().Select(t => new Table(t));
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
            {
                // We have to get Respawner to ignore the temporal tables.  For some reason it can't handle them.
                TablesToIgnore = [.. await historyTableList.ToListAsync()],
                WithReseed = true
            });
        }

        public async IAsyncEnumerable<string> GetHistoryTables()
        {
            await using var sqlConnection = new SqlConnection(ConnectionString);

            await sqlConnection.OpenAsync();
            await using var command = new SqlCommand($@"SELECT name FROM sys.tables WHERE temporal_type = 1 ORDER BY NAME", sqlConnection);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
                yield return reader.GetString(0);
        }

        public MetaDataContext CreateDbContext() => new(_dbContextOptions);

        public async Task RevertDatabase()
        {
            // Reset the database to a clean state before each test
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }

        public async Task Dispose()
        {
            await using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                // Set the database to SINGLE_USER mode, and force disconnect any users
                await sqlConnection.OpenAsync();
                await using (var command = new SqlCommand($@"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;", sqlConnection))
                {
                    await command.ExecuteNonQueryAsync();
                }

                await System.Threading.Tasks.Task.Delay(100);

                await using (var command = new SqlCommand($@"USE master; DROP DATABASE [{DatabaseName}];", sqlConnection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }

            Console.WriteLine($"Deleted local DB - {System.DateTime.Now:s}: {DatabaseName}");
        }
    }

    public class TestMetadataContextFactoryInMemory : ITestMetadataContextFactory
    {
        private DbContextOptions<MetaDataContext> _dbContextOptions;

        public async Task Initialise(string databaseName = "")
        {
            _dbContextOptions = new DbContextOptionsBuilder<MetaDataContext>()
                .UseInMemoryDatabase(nameof(TestMetadataContextFactoryInMemory), new InMemoryDatabaseRoot())
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)).Options;
        }

        public MetaDataContext CreateDbContext() => new(_dbContextOptions);
    }

    public class TestMetadataContextFactoryTransactionless : ITestMetadataContextFactory
    {
        private DbContextOptions<MetaDataContext> _dbContextOptions;

        public async Task Initialise(string databaseName = "")
        {
            _dbContextOptions = new DbContextOptionsBuilder<MetaDataContext>()
                .UseInMemoryDatabase(nameof(TestMetadataContextFactoryInMemory), new InMemoryDatabaseRoot())
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options;
        }

        public MetaDataContext CreateDbContext() => new(_dbContextOptions);
    }
}
