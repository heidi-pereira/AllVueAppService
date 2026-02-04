using System;
using System.Linq;
using BrandVue.EntityFramework.MetaData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Test.BrandVue.EntityFramework.Migrations.MetaData
{
    [TestFixture]
    public class MigrationTests
    {
        private IConfigurationRoot _configuration;

        [SetUp]
        public void Setup()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();
        }

        [Test]
        public void ShouldHaveNoDifferencesInMigrations()
        {
            // Arrange
            string connectionString = _configuration.GetConnectionString("MetaConnectionString");
            var options = new DbContextOptionsBuilder<MetaDataContext>()
                .UseSqlServer(connectionString)
                .Options;

            var dbContextFactory = new PooledDbContextFactory<MetaDataContext>(options);
            using var dbContext = dbContextFactory.CreateDbContext();

            var migrationsAssembly = dbContext.Database.GetService<IMigrationsAssembly>();
            var differ = dbContext.GetService<IMigrationsModelDiffer>();
            var designTimeModel = dbContext.GetService<IDesignTimeModel>();

            // Get the snapshot model and current model
            var modelSnapshot = migrationsAssembly.ModelSnapshot.Model;
            var currentModel = designTimeModel.Model;

            // Initialize the models
            var initializer = dbContext.GetService<IModelRuntimeInitializer>();
            modelSnapshot = initializer.Initialize(modelSnapshot);
            currentModel = initializer.Initialize(currentModel);

            var relationalModelSnapshot = modelSnapshot.GetRelationalModel();
            var relationalModel = currentModel.GetRelationalModel();

            // Act
            var differences = differ.GetDifferences(relationalModelSnapshot, relationalModel)
                .Select(m =>
                {
                    return m switch
                    {
                        AddColumnOperation addCol => $"Add Column: {addCol.Table}.{addCol.Name} ({addCol.ClrType?.Name ?? "unknown type"})",
                        DropColumnOperation dropCol => $"Drop Column: {dropCol.Table}.{dropCol.Name}",
                        AlterColumnOperation alterCol => $"Alter Column: {alterCol.Table}.{alterCol.Name}",
                        CreateTableOperation createTable => $"Create Table: {createTable.Name}",
                        DropTableOperation dropTable => $"Drop Table: {dropTable.Name}",
                        RenameColumnOperation renameCol => $"Rename Column: {renameCol.Table}.{renameCol.Name} -> {renameCol.NewName}",
                        _ => $"Unknown operation: {m.GetType().Name}"
                    };
                });

            // Assert
            Assert.That(differences, Is.Empty, "The database schema has changed. Please run 'dotnet ef migrations add <MigrationName>' and 'dotnet ef database update' to update the migrations.");
        }
    }
}