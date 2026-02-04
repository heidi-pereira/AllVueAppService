using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Subsets;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Subsets
{
    public static class ExtensionMethods
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

    [TestFixture]
    public class SubsetConfigurationRepositorySqlTests
    {
        private IDbContextFactory<MetaDataContext> _dbContextFactory;
        private MetaDataContext _dbContext;
        private IProductContext _productContext;
        private IChoiceSetReader _choiceSetReader;
        private SubsetConfigurationRepositorySql _configurationRepositorySql;
        private ISubsetRepository _subsetRepository;

        private readonly Dictionary<int, IReadOnlyCollection<string>> _surveyIdToAllowedSegmentNames = new() { { 1, new List<string> { "Segment1" } } };

        [SetUp]
        public void Setup()
        {
            _dbContextFactory = Substitute.For<IDbContextFactory<MetaDataContext>>();
            _productContext = Substitute.For<IProductContext>();
            _dbContext = Substitute.For<MetaDataContext>();
            _choiceSetReader = Substitute.For<IChoiceSetReader>();
            _subsetRepository = Substitute.For<ISubsetRepository>();

            _dbContextFactory.CreateDbContext().Returns(_dbContext);

            _choiceSetReader.GetSegments(Arg.Any<IEnumerable<int>>()).Returns(new[] { new SurveySegment() { SurveyId = 1, SegmentName = "Segment1" } });

            _configurationRepositorySql = new SubsetConfigurationRepositorySql(_dbContextFactory, _productContext, _choiceSetReader, _subsetRepository);
        }

        [Test]
        public void Create_UniqueSubsetConfiguration_AddsSubsetConfigurationToDb()
        {
            // Arrange
            var subsetConfiguration = new SubsetConfiguration
            {
                Identifier = "Test subset",
                DisplayName = "Unique display name",
                ParentGroupName = "Test group",
                ProductShortCode = "Test",
                SubProductId = "1",
                SurveyIdToAllowedSegmentNames = _surveyIdToAllowedSegmentNames
            };

            _productContext.ShortCode.Returns("Test");
            _productContext.SubProductId.Returns("1");
            _productContext.NonMapFileSurveyIds.Returns([1]);

            _dbContext.SubsetConfigurations = ExtensionMethods.FakeDbSet(new List<SubsetConfiguration>());

            // Act
            _configurationRepositorySql.Create(subsetConfiguration, "Test subset");

            // Assert
            _dbContext.SubsetConfigurations.Received(1).Add(Arg.Is<SubsetConfiguration>(e =>
                e.Identifier == "Test subset" &&
                e.DisplayName == "Unique display name" &&
                e.ParentGroupName == "Test group" &&
                e.ProductShortCode == "Test" &&
                e.SubProductId == "1" &&
                e.SurveyIdToAllowedSegmentNames == _surveyIdToAllowedSegmentNames
            ));
            _dbContext.Received(1).SaveChanges();
        }

        [Test]
        public void Create_SameDisplayNameDifferentParentGroup_AddsSubsetConfigurationToDb()
        {
            // Arrange
            var existingSubsets = new List<SubsetConfiguration>
            {
                new()
                {
                    Id = 1,
                    Identifier = "Test subset",
                    DisplayName = "Same display name",
                    ParentGroupName = "Old group",
                    ProductShortCode = "Same product",
                    SubProductId = "1",
                    SurveyIdToAllowedSegmentNames = _surveyIdToAllowedSegmentNames
                }
            };

            var newSubsetConfiguration = new SubsetConfiguration
            {
                Identifier = "Test subset",
                DisplayName = "Same display name",
                ParentGroupName = "New group",
                ProductShortCode = "Test",
                SubProductId = "1",
                SurveyIdToAllowedSegmentNames = _surveyIdToAllowedSegmentNames
            };

            _productContext.ShortCode.Returns("Test");
            _productContext.SubProductId.Returns("1");
            _productContext.NonMapFileSurveyIds.Returns([1]);

            _dbContext.SubsetConfigurations = ExtensionMethods.FakeDbSet(existingSubsets);

            // Act
            _configurationRepositorySql.Create(newSubsetConfiguration, "Test subset");

            // Assert
            _dbContext.SubsetConfigurations.Received(1).Add(Arg.Is<SubsetConfiguration>(e =>
                e.Identifier == "Test subset" &&
                e.DisplayName == "Same display name" &&
                e.ParentGroupName == "New group" &&
                e.ProductShortCode == "Test" &&
                e.SubProductId == "1" &&
                e.SurveyIdToAllowedSegmentNames == _surveyIdToAllowedSegmentNames
            ));
            _dbContext.Received(1).SaveChanges();
        }

        [Test]
        public void ValidateModel_DisplayNameAlreadyInUse_ThrowsBadRequestException()
        {
            // Arrange
            var existingSubsets = new List<SubsetConfiguration>
            {
                new()
                {
                    Id = 1,
                    Identifier = "Same subset",
                    DisplayName = "Same display name",
                    ParentGroupName = "Same group",
                    ProductShortCode = "Same product",
                    SubProductId = "1",
                    SurveyIdToAllowedSegmentNames = _surveyIdToAllowedSegmentNames
                }
            };

            var newSubsetConfiguration = new SubsetConfiguration
            {
                Identifier = "Same subset",
                DisplayName = "Same display name",
                ParentGroupName = "Same group",
                ProductShortCode = "Same product",
                SubProductId = "1",
                SurveyIdToAllowedSegmentNames = _surveyIdToAllowedSegmentNames
            };

            _productContext.ShortCode.Returns("Same product");
            _productContext.SubProductId.Returns("1");
            _productContext.NonMapFileSurveyIds.Returns([1]);

            _dbContext.SubsetConfigurations = ExtensionMethods.FakeDbSet(existingSubsets);

            // Act & Assert
            var exception = Assert.Throws<BadRequestException>(() => _configurationRepositorySql.Create(newSubsetConfiguration, "Same subset"),
                "Should throw BadRequestException when displayName is already in use");
            Assert.That(exception.Message, Does.Contain("displayName already in use."));
        }

        [Test]
        public void GetAll_ReturnsAllSubsetConfigurations()
        {
            // Arrange
            var existingSubsets = new List<SubsetConfiguration>
            {
                new() { Id = 1, DisplayName = "Subset 1", ProductShortCode = "Test", SubProductId = "1" },
                new() { Id = 2, DisplayName = "Subset 2", ProductShortCode = "Test", SubProductId = "1" },
                new() { Id = 3, DisplayName = "Subset 3", ProductShortCode = "OtherProduct", SubProductId = "2" }
            };

            _productContext.ShortCode.Returns("Test");
            _productContext.SubProductId.Returns("1");

            _dbContext.SubsetConfigurations = ExtensionMethods.FakeDbSet(existingSubsets);

            // Act
            var result = _configurationRepositorySql.GetAll();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Select(s => s.DisplayName), Is.EquivalentTo(new[] { "Subset 1", "Subset 2" }));
        }

        [Test]
        public void Update_ExistingSubsetConfiguration_UpdatesSuccessfully()
        {
            // Arrange
            var existingSubset = new SubsetConfiguration
            {
                Id = 1,
                Identifier = "Test subset",
                DisplayName = "Old Name",
                ProductShortCode = "Test",
                SubProductId = "1",
                SurveyIdToAllowedSegmentNames = _surveyIdToAllowedSegmentNames
            };

            var updatedSubset = new SubsetConfiguration
            {
                Identifier = "Test subset",
                DisplayName = "New Name",
                ProductShortCode = "Test",
                SubProductId = "1",
                SurveyIdToAllowedSegmentNames = _surveyIdToAllowedSegmentNames
            };

            _productContext.ShortCode.Returns("Test");
            _productContext.SubProductId.Returns("1");
            _productContext.NonMapFileSurveyIds.Returns([1]);

            _dbContext.SubsetConfigurations = ExtensionMethods.FakeDbSet(new List<SubsetConfiguration> { existingSubset });

            // Act
            _configurationRepositorySql.Update(updatedSubset, 1);

            // Assert
            _dbContext.SubsetConfigurations.Received(1).Update(Arg.Is<SubsetConfiguration>(s =>
                s.Id == 1 &&
                s.DisplayName == "New Name" &&
                s.ProductShortCode == "Test" &&
                s.SubProductId == "1"
            ));
            _dbContext.Received(1).SaveChanges();
        }

        [Test]
        public void Delete_ExistingSubsetConfiguration_DeletesSuccessfully()
        {
            // Arrange
            var existingSubset = new SubsetConfiguration
            {
                Id = 1,
                DisplayName = "To Be Deleted",
                ProductShortCode = "Test",
                SubProductId = "1"
            };

            _productContext.ShortCode.Returns("Test");
            _productContext.SubProductId.Returns("1");

            _dbContext.SubsetConfigurations = ExtensionMethods.FakeDbSet(new List<SubsetConfiguration> { existingSubset });

            // Act
            _configurationRepositorySql.Delete(1);

            // Assert
            _dbContext.SubsetConfigurations.Received(1).Remove(Arg.Is<SubsetConfiguration>(s => s.Id == 1));
            _dbContext.Received(1).SaveChanges();
        }

        [Test]
        public void Delete_NonExistentSubsetConfiguration_ThrowsBadRequestException()
        {
            // Arrange
            _productContext.ShortCode.Returns("Test");
            _productContext.SubProductId.Returns("1");

            _dbContext.SubsetConfigurations = ExtensionMethods.FakeDbSet(new List<SubsetConfiguration>());

            // Act & Assert
            Assert.Throws<BadRequestException>(() => _configurationRepositorySql.Delete(999),
                "Should throw BadRequestException when trying to delete a non-existent subset configuration");
        }

        [Test]
        public void Create_InvalidSurveyId_ThrowsBadRequestException()
        {
            // Arrange
            var subsetConfiguration = new SubsetConfiguration
            {
                Identifier = "Test subset",
                DisplayName = "Invalid Survey ID",
                ParentGroupName = "Test group",
                ProductShortCode = "Test",
                SubProductId = "1",
                SurveyIdToAllowedSegmentNames = new Dictionary<int, IReadOnlyCollection<string>> { { 2, new List<string> { "Segment1" } } }
            };

            _productContext.ShortCode.Returns("Test");
            _productContext.SubProductId.Returns("1");
            _productContext.NonMapFileSurveyIds.Returns([1]);

            _dbContext.SubsetConfigurations = ExtensionMethods.FakeDbSet(new List<SubsetConfiguration>());

            // Act & Assert
            var exception = Assert.Throws<BadRequestException>(() => _configurationRepositorySql.Create(subsetConfiguration, "Test subset"));
            Assert.That(exception.Message, Does.Contain("Non-existent survey segment(s)  - Survey 2-Segment1"));
        }

        [Test]
        public void Create_InvalidSegmentName_ThrowsBadRequestException()
        {
            // Arrange
            var subsetConfiguration = new SubsetConfiguration
            {
                Identifier = "Test subset",
                DisplayName = "Invalid Segment",
                ParentGroupName = "Test group",
                ProductShortCode = "Test",
                SubProductId = "1",
                SurveyIdToAllowedSegmentNames = new Dictionary<int, IReadOnlyCollection<string>> { { 1, new List<string> { "InvalidSegment" } } }
            };

            _productContext.ShortCode.Returns("Test");
            _productContext.SubProductId.Returns("1");
            _productContext.NonMapFileSurveyIds.Returns([1]);

            _dbContext.SubsetConfigurations = ExtensionMethods.FakeDbSet(new List<SubsetConfiguration>());

            // Act & Assert
            var exception = Assert.Throws<BadRequestException>(() => _configurationRepositorySql.Create(subsetConfiguration, "Test subset"));
            Assert.That(exception.Message, Does.Contain("Non-existent survey segment(s)"));
        }

        [Test]
        public void Update_AttemptingToChangeIdentifier_ThrowsBadRequestException()
        {
            // Arrange
            var existingSubset = new SubsetConfiguration
            {
                Id = 1,
                Identifier = "Old identifier",
                DisplayName = "Test subset",
                ProductShortCode = "Test",
                SubProductId = "1",
                SurveyIdToAllowedSegmentNames = _surveyIdToAllowedSegmentNames
            };

            var updatedSubset = new SubsetConfiguration
            {
                Identifier = "New identifier",
                DisplayName = "Test subset",
                ProductShortCode = "Test",
                SubProductId = "1",
                SurveyIdToAllowedSegmentNames = _surveyIdToAllowedSegmentNames
            };

            _productContext.ShortCode.Returns("Test");
            _productContext.SubProductId.Returns("1");
            _productContext.NonMapFileSurveyIds.Returns([1]);

            _dbContext.SubsetConfigurations = ExtensionMethods.FakeDbSet(new List<SubsetConfiguration> { existingSubset });

            // Act & Assert
            var exception = Assert.Throws<BadRequestException>(() => _configurationRepositorySql.Update(updatedSubset, 1),
                "Should throw BadRequestException when displayName is already in use");
            Assert.That(exception.Message, Does.Contain("Not allowed to change Identifier here"));
        }
    }
}