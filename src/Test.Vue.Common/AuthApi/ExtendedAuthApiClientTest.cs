using AuthServer.GeneratedAuthApi;
using NSubstitute;
using Vue.Common.AuthApi;

namespace Test.Vue.Common.AuthApi
{
    [TestFixture]
    public class ExtendedAuthApiClientTest
    {
        const string parentShortCode = "parentShortCode";
        const string parentCompanyId = "parentCompanyId";
        const string childCompanyId = "childCompanyId";
        const string grandChildCompanyId = "grandChildCompanyId";
        const string greatGrandChildCompanyId = "greatGrandChildCompanyId";
        const string greatGreatGrandChildCompanyId = "greatGreatGrandChildCompanyId";
        const string unconnectedCompanyId = "unconnectedCompanyId";

        private List<CompanyModel> GetCompanies(int? levels = null, string? parentId = null, string? shortCode = null )
        {
            var parentCompany = new CompanyModel
                { Id = parentId ?? parentCompanyId, DisplayName = "Parent Company", ShortCode = shortCode ?? parentShortCode };
            var childCompany = new CompanyModel
                { Id = childCompanyId, DisplayName = "Child Company 1", ParentCompanyId = parentCompanyId, ShortCode = "C1" };
            var grandChildCompany = new CompanyModel
                { Id = grandChildCompanyId, DisplayName = "Grandchild Company 1", ParentCompanyId = childCompanyId, ShortCode = "GC1" };
            var greatGrandChildCompany = new CompanyModel
                { Id = greatGrandChildCompanyId, DisplayName = "Grandchild Company 2", ParentCompanyId = grandChildCompanyId, ShortCode = "GGC1" };
            var greatGreatGrandChildCompany = new CompanyModel
                { Id = greatGreatGrandChildCompanyId, DisplayName = "Grandchild Company 3", ParentCompanyId = greatGrandChildCompanyId, ShortCode = "GGGC1" };
            var unconnectedCompany = new CompanyModel
                { Id = unconnectedCompanyId, DisplayName = "Another Company", ParentCompanyId = null, ShortCode = "COMP" };
            var companies = new List<CompanyModel> { parentCompany, childCompany, grandChildCompany, greatGrandChildCompany, greatGreatGrandChildCompany, unconnectedCompany };
            if (levels.HasValue)
            {
                companies = companies.Take(levels.Value).ToList();
            }
            return companies;
        }

        [SetUp]
        public void SetUp()
        {
            // Setup code if needed
        }

        [Test]
        public void GetCompanyTree_ShouldReturnNull_WhenNoCompaniesExist()
        {
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(new List<CompanyModel>());
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);
            // Act
            var result = extendedAuthApiClient.GetCompanyTree(parentShortCode, CancellationToken.None).Result;
            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetCompanyTree_ShouldReturnNull_WhenCompaniesExistButRequiredCompanyIsNotInData()
        {
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();

            var companies = GetCompanies();
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(companies);
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);
            
            // Act
            var result = extendedAuthApiClient.GetCompanyTree("nonExistantShortCode", CancellationToken.None).Result;
            
            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetCompanyTree_ShouldReturnCompanyNodeWithNoChildren_WhenCompaniesHaveParentNotIncludedInData()
        {
            var shortCode = "ShortCode";
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();
            var companies = GetCompanies(2, "1", shortCode);
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(companies);
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);
            
            // Act
            var result = extendedAuthApiClient.GetCompanyTree(shortCode, CancellationToken.None).Result;
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result.Children);
        }

        [Test]
        public void GetCompanyTree_ShouldReturnParentAndChildNodes_WhenDataIncludesTheParent() 
        {
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();

            var companies = GetCompanies(2);
            companies.Add(new CompanyModel
                { Id = "2", DisplayName = "Child Company 2", ParentCompanyId = parentCompanyId, ShortCode = "C2" });
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(companies);
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);
            
            // Act
            var result = extendedAuthApiClient.GetCompanyTree(parentShortCode, CancellationToken.None).Result;
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Children.Count());
        }

        [Test]
        public void GetCompanyTree_ShouldReturnCompanyWithChildren_WhenDataIncludesTwoLevelsOfHierarchy()
        {
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();
            var companies = GetCompanies(3);
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(companies);
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);
            
            // Act
            var result = extendedAuthApiClient.GetCompanyTree(parentShortCode, CancellationToken.None).Result;
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Children.Count());
            Assert.AreEqual(1, result.Children.First().Children.Count());
        }
        [Test]
        public void GetCompanyTree_ShouldReturnCompanyWithChildren_WhenDataIncludesMultipleLevelsOfHierarchy()
        {
            var companies = GetCompanies();
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(companies);
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);

            // Act
            var result = extendedAuthApiClient.GetCompanyTree(parentShortCode, CancellationToken.None).Result;

            // Assert
            Assert.IsNotNull(result);
            var childNodes = result.Children;
            Assert.AreEqual(1, childNodes.Count());
            var grandChildNodes = childNodes.First().Children;
            Assert.AreEqual(1, grandChildNodes.Count());
            var greatGrandChildNodes = grandChildNodes.First().Children;
            Assert.AreEqual(1, greatGrandChildNodes.Count());
            var greatGreatGrandChildNodes = greatGrandChildNodes.First().Children;
            Assert.AreEqual(1, greatGreatGrandChildNodes.Count());
        }

        [Test]
        public void GetCompanyAndChildrenList_ShouldReturnEmptyList_WhenThereAreNoCompanies()
        {
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(new List<CompanyModel>());
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);
            // Act
            var result = extendedAuthApiClient.GetCompanyAndChildrenList(parentShortCode, CancellationToken.None).Result;
            // Assert
            Assert.IsEmpty(result);
        }

        [Test]
        public void GetCompanyAndChildrenList_ShouldReturnEmptyList_WhenTheRequestedShortCodeIsNotInData()
        {
            var companies = GetCompanies();
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(companies);
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);
            // Act
            var result = extendedAuthApiClient.GetCompanyAndChildrenList("nonExistantCode", CancellationToken.None).Result;
            // Assert
            Assert.IsEmpty(result);
        }

        [Test]
        public void GetCompanyAndChildrenList_ShouldReturnCompanyAndChildren_WhenDataIncludesTheParent()
        {
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();
            var companies = GetCompanies();
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(companies);
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);
            
            // Act
            var result = extendedAuthApiClient.GetCompanyAndChildrenList(parentShortCode, CancellationToken.None).Result;
            
            // Assert
            Assert.IsNotEmpty(result);
            Assert.AreEqual(5, result.Count());
        }

        [Test]
        public void GetCompanyAndChildrenList_ShouldReturnCompanyAndChildren_WhenDataIncludesTheParentAndMultipleChildren()
        {
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();
            var companies = GetCompanies(2);
            for (int i = 0; i < 3; i++)
            {
                var child = new CompanyModel
                {
                    Id = (i + 3).ToString(),
                    DisplayName = $"Child Company {i + 3}",
                    ParentCompanyId = parentCompanyId,
                    ShortCode = $"C{i + 3}"
                };
                companies.Add(child);
            }
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(companies);
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);

            // Act
            var result = extendedAuthApiClient.GetCompanyAndChildrenList(parentShortCode, CancellationToken.None).Result;

            // Assert
            Assert.IsNotEmpty(result);
            Assert.AreEqual(5, result.Count());
        }

        [Test]
        public void GetCompanyAncestorsById_ShouldReturnEmptyList_WhenNoAncestors()
        {
            var authApiClient = Substitute.For<IAuthApiClient>();
            var companies = GetCompanies();
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(companies);
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);

            var result = extendedAuthApiClient.GetCompanyAncestorsById(unconnectedCompanyId, false, CancellationToken.None).Result;

            Assert.IsEmpty(result);
        }

        [TestCase(greatGreatGrandChildCompanyId, false, 3)]
        [TestCase(greatGreatGrandChildCompanyId, true, 4)]
        [TestCase(unconnectedCompanyId, false, 0)]
        [TestCase( parentCompanyId, true, 0)]
        [TestCase(childCompanyId, true, 1)]
        [TestCase(childCompanyId, false, 0)]
        public void GetCompanyAncestorsById_ShouldReturnAncestors_WhenCompanyHasAncestors(string companyId, bool includeSavanta, int expectedAncestorCount)
        {
            // Arrange
            var authApiClient = Substitute.For<IAuthApiClient>();
            var companies = GetCompanies(6, null, "savanta");
            authApiClient.GetAllCompanies(Arg.Any<CancellationToken>()).Returns(companies);
            var extendedAuthApiClient = new ExtendedAuthApiClient(authApiClient);
            // Act
            var result = extendedAuthApiClient.GetCompanyAncestorsById(companyId, includeSavanta, CancellationToken.None).Result;
            // Assert
            Assert.AreEqual(expectedAncestorCount, result.Count());
        }
    }
}
