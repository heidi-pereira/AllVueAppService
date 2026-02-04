using System;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd.SurveyApi.Documentation
{
    [TestFixture]
    public class AllLinksValidTests
    {
        /// <summary>
        /// Make sure you run npm run build:docs before running this test, otherwise you won't be testing the latest docs.
        /// </summary>
        [Test]
        public void GivenUrlTestThatInternalLinksAreValid()
        {
            string url = $@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\BrandVue.FrontEnd\wwwroot\developers\docs\index.html";

            var doc = new HtmlWeb().Load(url);
            var internalPageLinks = doc.DocumentNode.Descendants("a")
                .Select(a => a.GetAttributeValue("href", null))
                .Where(u => !string.IsNullOrEmpty(u) && u[0] == '#');

            var sb = new StringBuilder();

            foreach (string internalPageLink in internalPageLinks)
            {
                if (doc.GetElementbyId(internalPageLink.Replace("#", "")) == null)
                {
                    sb.AppendLine(internalPageLink);
                }
            }

            Assert.That(sb.ToString(), Is.Empty, sb.ToString());
        }
    }
}
