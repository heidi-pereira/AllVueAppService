using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue;
using BrandVue.SourceData.CommonMetadata;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Dashboard
{
    [TestFixture]
    public class FieldExtractorTests
    {
        private static readonly string[] Headers = new[]
        {
            "paneId", "partType", "spec1", "spec2", "spec3", "helpText", "disabled", "environment", "autometrics",
            "autopanes", "ordering", "orderingDirection", "colours", "filters", "xRange", "yRange", "sections"
        };

        [TestCase(
            arg: new string[] {"Brand_advantage_vs_Familiarity1", "ScatterPlot", "Familiarity|Advantage", "", "", "", "", "", "", "", "", "", "", "", "0|1", "0.15|0.6", "NICH LEADER+FUTURE STAR+ICON|NICHE INTEREST+CONTENDER+HIGH PERFORMER|WEAK+SECOND TIER+FADING STARS"}, 
            ExpectedResult = new[] {0, 1}
        )]
        [TestCase(
            arg: new string[] { "Adoption2", "Funnel", "Penetration (L3M)|Consideration|Awareness", "", "", "", "", "", "", "", "", "", "", "", "", "", ""},
            ExpectedResult = null
        )]
        public double[] Test_ExtractDoubleArrayFromStringIntArray(string[] currentRecord)
        {
            return FieldExtractor.ExtractDoubleArray(CommonMetadataFields.XRange, Headers, currentRecord, true);
        }

        [TestCase(
            arg: new string[] {"Brand_advantage_vs_Familiarity1", "ScatterPlot", "Familiarity|Advantage", "", "", "", "", "", "", "", "", "", "", "", "0|1", "0.15|0.6", "NICH LEADER+FUTURE STAR+ICON|NICHE INTEREST+CONTENDER+HIGH PERFORMER|WEAK+SECOND TIER+FADING STARS"}, 
            ExpectedResult = new[] {0.15, 0.6}
        )]
        [TestCase(
            arg: new string[] { "Adoption2", "Funnel", "Penetration (L3M)|Consideration|Awareness", "", "", "", "", "", "", "", "", "", "", "", "", "", ""},
            ExpectedResult = null
        )]
        public double[] Test_ExtractDoubleArrayFromStringDoubleArray(string[] currentRecord)
        {
            return FieldExtractor.ExtractDoubleArray(CommonMetadataFields.YRange, Headers, currentRecord, true);
        }
    }
}
