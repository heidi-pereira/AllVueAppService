using System;
using System.IO;
using DashboardBuilder.AsposeHelper;
using DashboardMetadataBuilder.MapProcessing;
using NUnit.Framework;

namespace DashboardBuilder.Tests
{
    class MetadataValidatorFindSpecificErrorTests
    {
        private static readonly string AssemblyDir = Path.GetDirectoryName(new Uri(typeof(MetadataValidatorFindSpecificErrorTests).Assembly.Location).AbsolutePath);

        [Test]
        public void MapFileWithKnownErrorsTest()
        {
            string mapFile = $"{AssemblyDir}\\TestData\\FinanceMapWithKnownErrors.xlsx";

            var validationResult = StaticAnalysisMapValidator.Validate(AsposeCellsHelper.OpenWorkbook(mapFile));


            var expectedIssueDescriptions = new string[]
            {
                "DashPages found multiple definition for 'About'",
                "Metrics.BaseField contains 'Complaint_Experience_The_Clarity_of_Information_Around_the_Process_and_the_Status_of_Your_Claim', the closest valid option in Fields.Name is 'Claim_Experience_The_Clarity_of_Information_Around_the_Process_and_the_Status_of_Your_Claim'",
                "Metrics.Field contains 'Complaint_Experience_The_Clarity_of_Information_Around_the_Process_and_the_Status_of_Your_Claim', the closest valid option in Fields.Name is 'Claim_Experience_The_Clarity_of_Information_Around_the_Process_and_the_Status_of_Your_Claim'",
                "Metrics.Field contains 'Spontaneous_Awareness_Banks', the closest valid option in Fields.Name is '//Spontaneous_Awareness_Banks'",
                "Metrics.Field contains 'Spontaneous_Awareness_Credit_Cards', the closest valid option in Fields.Name is '//Spontaneous_Awareness_Credit_Cards'",
                "Metrics.Field contains 'Spontaneous_Awareness_Insurance_Providers', the closest valid option in Fields.Name is '//Spontaneous_Awareness_Insurance_Providers'",
                "Metrics.Field contains 'Spontaneous_Awareness_Life_Insurance_and_Protection', the closest valid option in Fields.Name is '//Spontaneous_Awareness_Life_Insurance_and_Protection'",
                "Metrics.Field contains 'Spontaneous_Awareness_Loans', the closest valid option in Fields.Name is '//Spontaneous_Awareness_Loans'",
                "Metrics.Field contains 'Spontaneous_Awareness_Mortgages', the closest valid option in Fields.Name is '//Spontaneous_Awareness_Mortgages'",
                "Metrics.Field contains 'Spontaneous_Awareness_Pensions_and_Investments', the closest valid option in Fields.Name is '//Spontaneous_Awareness_Pensions_and_Investments'",
                "Metrics.Field contains 'Spontaneous_Awareness_Price_Comparison_Websites', the closest valid option in Fields.Name is '//Spontaneous_Awareness_Price_Comparison_Websites'"
            };

            Assert.That(validationResult.Errors, Is.EquivalentTo(expectedIssueDescriptions));
        }
    }
}
