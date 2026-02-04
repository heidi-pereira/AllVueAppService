using System.Linq;
using System.Threading.Tasks;
using BrandVue.SourceData.Calculation.Expressions;
using Newtonsoft.Json;
using NUnit.Framework;
using VerifyNUnit;

namespace Test.BrandVue.SourceData.FieldExpressionParsing
{
    internal class FilterValueMappingVariableParserTests
    {
        [TestCase("1:-Value-", "-Value-")]
        [TestCase("1:-Va||lue-", "-Va||lue-")]
        [TestCase("1:Value", "Value")]
        [TestCase("1:Range: Based on Age", "Based on Age")]
        [TestCase("!1:Female", "Female")]
        [TestCase("!1:Female|Not something else", "else")]
        public void SingleValue(string testCase, string endsWith)
        {
            var parts = FilterValueMappingVariableParser.FilterMeasures(testCase);
            Assert.That(parts.Count, Is.EqualTo(1), "Did not get a single value");
            Assert.That(parts.First().Name, Does.EndWith(endsWith), "Incorrect ending");
        }


        [TestCase("0,2,3:Values", TestName = "Values")]
        [TestCase("0-3:Values", TestName = "SimpleRange")]
        [TestCase("0:Female|1:Male", TestName = "Gender")]
        [TestCase("!1:Female|1:Male", TestName = "GenderLogical")]
        [TestCase("0:Fem:ale|1:Ma:le", TestName = "GenderWithExtraColons")]
        [TestCase("1:Range: FF Max Diff", TestName = "Range")]
        [TestCase("1:Range: Including |2:New group 2|3:New group 3|4:New group 4|5:New group 5", TestName = "RangeExtra")]
        [TestCase("1,2,3,4,5,5,6,7,8:Yes|!1,2,3,4,5,6,7,8:No", TestName ="WGSN_Spontaneous_Example")]
        [TestCase("1-8:Yes|!1-8:No", TestName ="NegativeRange")]
        [TestCase("0:1-2 children|3-4 children|1:None", TestName ="MultiPipe")]
        public async Task VerifyStandardTestCases(string testCase)
        {
            var parts = FilterValueMappingVariableParser.FilterMeasures(testCase);

            await Verifier.Verify(parts);
        }

        [TestCase("Range:Range", false)]
        [TestCase("1:Range", true)]
        public void VerifyStandardTestCases(string testCase, bool expectedIsValid)
        {
            var isValid = FilterValueMappingVariableParser.IsValidFilterValueMapping(testCase);

            Assert.That(isValid, Is.EqualTo(expectedIsValid), $"{testCase} as incorrectly marked as valid");
        }
    }
}
