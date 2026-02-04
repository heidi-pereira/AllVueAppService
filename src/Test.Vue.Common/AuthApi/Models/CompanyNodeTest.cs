using AuthServer.GeneratedAuthApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vue.Common.AuthApi.Models;

namespace Test.Vue.Common.AuthApi.Models
{
    [TestFixture]
    public class CompanyNodeTest
    {
        [Test]
        public void FromCompanyModel_ShouldReturnNull_WhenCompanyModelIsNull()
        {
            var companyNode = CompanyNode.FromCompanyModel(null);
            Assert.IsNull(companyNode);
        }

        [Test]
        public void FromCompanyModel_ShouldReturnCompanyNodeWithCorrectProperties_WhenModelIsNotNull()
                {
            var companyModel = new CompanyModel
            {
                Id = "123",
                ShortCode = "SC123",
                DisplayName = "Test Company",
                Url = "http://testcompany.com"
            };
            var companyNode = CompanyNode.FromCompanyModel(companyModel);
            Assert.IsNotNull(companyNode);
            Assert.AreEqual(companyModel.Id, companyNode.Id);
            Assert.AreEqual(companyModel.ShortCode, companyNode.ShortCode);
            Assert.AreEqual(companyModel.DisplayName, companyNode.DisplayName);
            Assert.AreEqual(companyModel.Url, companyNode.Url);
            Assert.IsEmpty(companyNode.Children);
        }
    }
}
