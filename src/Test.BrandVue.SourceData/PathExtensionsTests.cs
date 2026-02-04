using BrandVue.SourceData.Utils;
using NUnit.Framework;

namespace Test.BrandVue.SourceData
{
    public class PathExtensionsTests
    {
        [TestCase(@"\some.\text\", ExpectedResult = "some.text")]
        [TestCase(@"/some/ text/", ExpectedResult = "some text")]
        public string Matches(string input)
        {
            return input.ReplaceInvalidFilenameCharacters();
        }
    }
}
