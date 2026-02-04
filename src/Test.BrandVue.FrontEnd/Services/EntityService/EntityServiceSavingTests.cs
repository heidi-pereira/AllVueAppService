using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Subsets;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using BrandVue.SourceData.Entity;

namespace Test.BrandVue.FrontEnd.Services.EntityService
{
    public static class ExtentionMethods
    {
        public static DbSet<T> FakeDbSet<T>(List<T> data) where T : class
        {
            var _data = data.AsQueryable();
            var fakeDbSet = Substitute.For<DbSet<T>, IQueryable<T>>();
            ((IQueryable<T>)fakeDbSet).Provider.Returns(_data.Provider);
            ((IQueryable<T>)fakeDbSet).Expression.Returns(_data.Expression);
            ((IQueryable<T>)fakeDbSet).ElementType.Returns(_data.ElementType);
            ((IQueryable<T>)fakeDbSet).GetEnumerator().Returns((x) => _data.GetEnumerator());
            return fakeDbSet;
        }
    }

    internal class EntityServiceSavingTests
    {
        private IDbContextFactory<MetaDataContext> _dbContextFactory;
        private IProductContext _productContext;
        private MetaDataContext _dbContext;
        private EntityInstanceRepositorySql _repository;
        private IEntityRepository _entityRepository;

        [Test]
        public void Save_ShouldAddNewEntityInstanceConfiguration_WhenNoExistingConfiguration()
        {
            // Arrange
            _dbContextFactory = Substitute.For<IDbContextFactory<MetaDataContext>>();
            _productContext = Substitute.For<IProductContext>();
            _dbContext = Substitute.For<MetaDataContext>();
            _entityRepository = Substitute.For<IEntityRepository>();

            _dbContextFactory.CreateDbContext().Returns(_dbContext);

            _repository = new EntityInstanceRepositorySql(_productContext, _dbContextFactory, _entityRepository);

            var selectedSubset = new Subset { Id = "subset1" };
            var entityTypeIdentifier = "entityType1";
            var surveyChoiceId = 1;
            var name = "name";
            var enabled = true;
            var startDate = DateTimeOffset.Now;
            string imageUrl = "imageUrl";
            _dbContext.EntityInstanceConfigurations = ExtentionMethods.FakeDbSet(new List<EntityInstanceConfiguration>());

            // Act
            _repository.Save(selectedSubset, entityTypeIdentifier, surveyChoiceId, name, enabled, startDate, imageUrl);

            // Assert
            _dbContext.EntityInstanceConfigurations.Received(1).Add(Arg.Is<EntityInstanceConfiguration>(e =>
                e.ProductShortCode == _productContext.ShortCode &&
                e.SubProductId == _productContext.SubProductId &&
                e.EntityTypeIdentifier == entityTypeIdentifier &&
                e.SurveyChoiceId == surveyChoiceId &&
                e.DisplayNameOverrideBySubset[selectedSubset.Id] == name &&
                e.EnabledBySubset[selectedSubset.Id] == enabled &&
                e.StartDateBySubset[selectedSubset.Id] == startDate
            ));
            _dbContext.Received(1).SaveChanges();
        }

        [Test]
        public void Save_ShouldUpdateExistingEntityInstanceConfiguration_WhenExistingConfigurationFoundAndDisplayDictionaryIsNull()
        {
            // Arrange
            _dbContextFactory = Substitute.For<IDbContextFactory<MetaDataContext>>();
            _productContext = Substitute.For<IProductContext>();
            _dbContext = Substitute.For<MetaDataContext>();
            _entityRepository = Substitute.For<IEntityRepository>();

            _dbContextFactory.CreateDbContext().Returns(_dbContext);

            var selectedSubset = new Subset { Id = "subset1" };
            var entityTypeIdentifier = "entityType1";
            var surveyChoiceId = 1;
            string imageUrl = "imageUrl";

            var existingConfig = new EntityInstanceConfiguration
            {
                SurveyChoiceId = surveyChoiceId,
                EntityTypeIdentifier = entityTypeIdentifier,
                DisplayNameOverrideBySubset = null,
                EnabledBySubset = new Dictionary<string, bool>(),
                StartDateBySubset = new Dictionary<string, DateTimeOffset>(),
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
            };

            _dbContext.EntityInstanceConfigurations = ExtentionMethods.FakeDbSet(new List<EntityInstanceConfiguration> { existingConfig });
            _repository = new EntityInstanceRepositorySql(_productContext, _dbContextFactory, _entityRepository);
            var newName = Guid.NewGuid().ToString("N");

            // Act & Assert
            Assert.DoesNotThrow(() => _repository.Save(selectedSubset, entityTypeIdentifier, surveyChoiceId, newName, true, null, imageUrl));

            _dbContext.EntityInstanceConfigurations.Received(0).Add(Arg.Any<EntityInstanceConfiguration>());
            _dbContext.Received(1).SaveChanges();
            Assert.That(existingConfig.DisplayNameOverrideBySubset[selectedSubset.Id], Is.EqualTo(newName));
        }

        [Test]
        public void Save_ShouldUpdateExistingEntityInstanceConfiguration_WhenExistingConfigurationFound()
        {
            // Arrange
            _dbContextFactory = Substitute.For<IDbContextFactory<MetaDataContext>>();
            _productContext = Substitute.For<IProductContext>();
            _dbContext = Substitute.For<MetaDataContext>();
            _entityRepository = Substitute.For<IEntityRepository>();
            string imageUrl = "imageUrl";

            _dbContextFactory.CreateDbContext().Returns(_dbContext);

            var selectedSubset = new Subset { Id = "subset1" };
            var entityTypeIdentifier = "entityType1";
            var surveyChoiceId = 1;

            var existingConfig = new EntityInstanceConfiguration
            {
                SurveyChoiceId = surveyChoiceId,
                EntityTypeIdentifier = entityTypeIdentifier,
                DisplayNameOverrideBySubset = new Dictionary<string, string>(),
                EnabledBySubset = new Dictionary<string, bool>(),
                StartDateBySubset = new Dictionary<string, DateTimeOffset>(),
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
            };

            var dbSet = ExtentionMethods.FakeDbSet(new List<EntityInstanceConfiguration> { existingConfig });
            _dbContext.EntityInstanceConfigurations = dbSet;
            _repository = new EntityInstanceRepositorySql(_productContext, _dbContextFactory, _entityRepository);
            var newName = Guid.NewGuid().ToString("N");

            // Act & Assert
            Assert.DoesNotThrow(() => _repository.Save(selectedSubset, entityTypeIdentifier, surveyChoiceId, newName, true, null, imageUrl));

            _dbContext.EntityInstanceConfigurations.Received(0).Add(Arg.Any<EntityInstanceConfiguration>());
            _dbContext.Received(1).SaveChanges();
            Assert.That(existingConfig.DisplayNameOverrideBySubset[selectedSubset.Id], Is.EqualTo(newName));
        }
    }
}
