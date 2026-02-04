using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.Services;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.FrontEnd.Services
{
    public class SavedBreaksServiceTests
    {
        private const string TestShortCode = "test-survey";
        private const string TestSubProductId = "good-sub-product";

        private const string TestUserId = "1";
        private const string TestCompanyId = "abcdef";

        private readonly ProductContext _productContext = new ProductContext(TestShortCode, TestSubProductId, true, "foo");
        private readonly IUserContext _userInformationProvider = Substitute.For<IUserContext>();

        [SetUp]
        public void Setup()
        {
            _userInformationProvider.UserId.Returns(TestUserId);
            _userInformationProvider.AuthCompany.Returns(TestCompanyId);
            _userInformationProvider.IsAdministrator.Returns(true);
        }

        [Test]
        public void ShouldGetBreaksForCurrentUser()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, _userInformationProvider);

            var service = new SavedBreaksService(repository, _userInformationProvider, _productContext);

            int expectedBreakCount = 2;
            AddBreaksForUser(dbContextFactory, expectedBreakCount, TestUserId);

            var breaks = service.GetForCurrentSurveyAndUser();
            Assert.That(breaks.SavedBreaks, Has.Length.EqualTo(expectedBreakCount));
        }

        [Test]
        public void ShouldNotGetPrivateBreaksForOtherUser()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, _userInformationProvider);

            var service = new SavedBreaksService(repository, _userInformationProvider, _productContext);

            int expectedBreakCount = 2;
            AddBreaksForUser(dbContextFactory, expectedBreakCount, TestUserId);

            const string otherUserId = "2";
            AddBreaksForUser(dbContextFactory, 1, otherUserId);

            var breaks = service.GetForCurrentSurveyAndUser();
            Assert.That(breaks.SavedBreaks, Has.Length.EqualTo(expectedBreakCount));
        }

        [Test]
        public void ShouldGetSharedBreaksForOtherUser()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, _userInformationProvider);

            var service = new SavedBreaksService(repository, _userInformationProvider, _productContext);

            const int currentUserBreakCount = 2;
            AddBreaksForUser(dbContextFactory, currentUserBreakCount, TestUserId);

            const string otherUserId = "2";
            AddBreaksForUser(dbContextFactory, 1, otherUserId);

            const int sharedBreakCount = 1;
            AddBreaksForUser(dbContextFactory, sharedBreakCount, otherUserId, isShared: true);

            var breaks = service.GetForCurrentSurveyAndUser();
            Assert.That(breaks.SavedBreaks, Has.Length.EqualTo(currentUserBreakCount + sharedBreakCount));
        }

        [Test]
        public void ShouldNotGetBreaksForSpecificOtherCompany()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, _userInformationProvider);

            var service = new SavedBreaksService(repository, _userInformationProvider, _productContext);

            int expectedBreakCount = 2;
            AddBreaksForUser(dbContextFactory, expectedBreakCount, TestUserId, TestCompanyId);

            const string otherCompanyShortCode = "qwerty";
            AddBreaksForUser(dbContextFactory, 1, TestUserId, otherCompanyShortCode);

            var breaks = service.GetForCurrentSurveyAndUser();
            Assert.That(breaks.SavedBreaks, Has.Length.EqualTo(expectedBreakCount));
        }

        [Test]
        public void ShouldGetBreaksForAllCompanies()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, _userInformationProvider);

            var service = new SavedBreaksService(repository, _userInformationProvider, _productContext);

            const int currentUserBreakCount = 2;
            AddBreaksForUser(dbContextFactory, currentUserBreakCount, TestUserId, TestCompanyId);

            const string allCompaniesShortcode = null;
            const int allCompaniesBreakCount = 1;
            AddBreaksForUser(dbContextFactory, allCompaniesBreakCount, TestUserId, allCompaniesShortcode);

            var breaks = service.GetForCurrentSurveyAndUser();
            Assert.That(breaks.SavedBreaks, Has.Length.EqualTo(currentUserBreakCount + allCompaniesBreakCount));
        }

        [Test]
        public void ShouldGetAllBreaksForSubProduct()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, _userInformationProvider);

            var service = new SavedBreaksService(repository, _userInformationProvider, _productContext);

            int expectedBreakCount = 2;
            AddBreaksForUser(dbContextFactory, expectedBreakCount, TestUserId);

            const string otherUserId = "2";
            const int otherUserBreakCount = 1;
            AddBreaksForUser(dbContextFactory, otherUserBreakCount, otherUserId);

            var breaks = service.GetAllSavedBreaksForSubProduct();
            Assert.That(breaks, Has.Length.EqualTo(expectedBreakCount + otherUserBreakCount));
        }

        [Test]
        public void SaveBreaks_ShouldThrowExceptionWhenTheNameExistInTheDb()
        {
            // Arrange
            const string breakName = "My break";
            var breakInDb = new SavedBreakCombination();
            var savedBreakRepository = Substitute.For<ISavedBreaksRepository>();
            savedBreakRepository
                .GetBreakByName(breakName)
                .Returns(breakInDb);

            var service = new SavedBreaksService(savedBreakRepository, _userInformationProvider, _productContext);

            // Act & Assert
            var ex = Assert.Throws<BadRequestException>(() => 
            service.SaveBreaks(breakName, false, new[] { new CrossMeasure { MeasureName = "TestMeasure" } }));


            Assert.That(ex.Message, Is.EqualTo("Name already exists."));
        }

        [Test]
        public void ShouldSaveBreaks()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, _userInformationProvider);

            var service = new SavedBreaksService(repository, _userInformationProvider, _productContext);

            const string expectedBreakName = "My break";
            service.SaveBreaks(expectedBreakName, false, new[] {new CrossMeasure {MeasureName = "TestMeasure"}});

            using var dbContext = dbContextFactory.CreateDbContext();
            var savedBreak = dbContext.SavedBreaks.First();

            Assert.That(savedBreak.Name, Is.EqualTo(expectedBreakName));
        }

        [Test]
        public void UpdateSavedBreak_ShouldThrowExceptionWhenTheNameExistInTheDb()
        {
            // Arrange
            const string breakName = "My break";
            var breakInDb = new SavedBreakCombination
            {
                Id = 1
            };
            var breakToUpdate = new SavedBreakCombination
            {
                Id = 2
            };

            var savedBreakRepository = Substitute.For<ISavedBreaksRepository>();
            savedBreakRepository
                .GetBreakByName(breakName)
                .Returns(breakInDb);
            savedBreakRepository
                .GetById(2)
                .Returns(breakToUpdate);

            var service = new SavedBreaksService(savedBreakRepository, _userInformationProvider, _productContext);

            // Act & Assert
            var ex = Assert.Throws<BadRequestException>(() =>
            service.UpdateSavedBreak(breakToUpdate.Id, breakName, false));


            Assert.That(ex.Message, Is.EqualTo("Name already exists."));
        }

        [Test]
        public void ShouldUpdateSavedBreakName()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, _userInformationProvider);

            var service = new SavedBreaksService(repository, _userInformationProvider, _productContext);

            var breaks = AddBreaksForUser(dbContextFactory, 1, TestUserId);
            var breakToUpdate = breaks.First();

            const string expectedUpdatedName = "Updated name";
            service.UpdateSavedBreak(breakToUpdate.Id, expectedUpdatedName, false);

            var updatedBreaks = service.GetAllSavedBreaksForSubProduct();
            var updatedBreak = updatedBreaks.First();

            Assert.That(updatedBreak.Name, Is.EqualTo(expectedUpdatedName));
        }

        [Test]
        public void ShouldUpdateSavedBreakIsShared()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, _userInformationProvider);

            var service = new SavedBreaksService(repository, _userInformationProvider, _productContext);

            var breaks = AddBreaksForUser(dbContextFactory, 1, TestUserId, isShared: true);
            var breakToUpdate = breaks.First();

            const bool expectedUpdatedIsShared = false;
            service.UpdateSavedBreak(breakToUpdate.Id, breakToUpdate.Name, expectedUpdatedIsShared);

            var updatedBreaks = service.GetAllSavedBreaksForSubProduct();
            var updatedBreak = updatedBreaks.First();

            Assert.That(updatedBreak.IsShared, Is.EqualTo(expectedUpdatedIsShared));
        }

        [Test]
        public void ShouldErrorIfNonAdminUpdatesOtherUsersBreak()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var nonAdminUserInformation = Substitute.For<IUserContext>();
            nonAdminUserInformation.IsAdministrator.Returns(false);
            nonAdminUserInformation.UserId.Returns(TestUserId);
            nonAdminUserInformation.AuthCompany.Returns(TestCompanyId);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, nonAdminUserInformation);

            var service = new SavedBreaksService(repository, nonAdminUserInformation, _productContext);

            var breaks = AddBreaksForUser(dbContextFactory, 1, "otherUserId");
            var breakToUpdate = breaks.First();

            Assert.Throws<BadRequestException>(() =>
            {
                service.UpdateSavedBreak(breakToUpdate.Id, "Foo", true);
            });
        }

        [Test]
        public void ShouldRemoveBreak()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, _userInformationProvider);

            var service = new SavedBreaksService(repository, _userInformationProvider, _productContext);

            int expectedBreakCount = 2;
            var initialBreaks = AddBreaksForUser(dbContextFactory, expectedBreakCount, TestUserId);
            var breakToRemove = initialBreaks.First();

            service.RemoveSavedBreak(breakToRemove.Id);

            var breaks = service.GetForCurrentSurveyAndUser();
            var gotDeletedBreak = breaks.SavedBreaks.Any(b => b.Id == breakToRemove.Id);

            Assert.That(gotDeletedBreak, Is.False);
        }

        [Test]
        public void ShouldErrorIfNonAdminRemovesOtherUsersBreak()
        {
            var dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var nonAdminUserInformation = Substitute.For<IUserContext>();
            nonAdminUserInformation.IsAdministrator.Returns(false);
            nonAdminUserInformation.UserId.Returns(TestUserId);
            nonAdminUserInformation.AuthCompany.Returns(TestCompanyId);
            var repository = new SavedBreaksRepository(_productContext, dbContextFactory, nonAdminUserInformation);

            var service = new SavedBreaksService(repository, nonAdminUserInformation, _productContext);

            int expectedBreakCount = 2;
            var initialBreaks = AddBreaksForUser(dbContextFactory, expectedBreakCount, "otherUserId");
            var breakToRemove = initialBreaks.First();

            Assert.Throws<BadRequestException>(() =>
            {
                service.RemoveSavedBreak(breakToRemove.Id);
            });
        }

        private static IEnumerable<SavedBreakCombination> AddBreaksForUser(
            IDbContextFactory<MetaDataContext> dbContextFactory,
            int breaksToAdd,
            string userId,
            string companyId = null,
            bool isShared = false
        )
        {
            using var dbContext = dbContextFactory.CreateDbContext();

            var breaks = new List<SavedBreakCombination>();

            for (int i = 1; i <= breaksToAdd; i++)
            {
                var savedBreak = new SavedBreakCombination()
                {
                    Name = $"Test break {userId}_{i}",
                    ProductShortCode = TestShortCode,
                    SubProductId = TestSubProductId,
                    IsShared = isShared,
                    Description = "",
                    AuthCompanyShortCode = companyId,
                    CreatedByUserId = userId,
                    Breaks = new List<CrossMeasure> {new() {MeasureName = "TestMeasure"}}
                };

                dbContext.SavedBreaks.Add(savedBreak);
                breaks.Add(savedBreak);
            }

            dbContext.SaveChanges();

            return breaks;
        }
    }
}
