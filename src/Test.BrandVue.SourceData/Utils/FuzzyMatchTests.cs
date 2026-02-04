using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.Utils;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Utils
{
    [TestFixture]
    [Parallelizable]
    public class FuzzyMatchTests
    {
        private static IEnumerable<TestCaseData> DemerauLevenshteinDistanceTestExamples()
        {
            yield return new TestCaseData("random string", "random string", 0).SetName("Matching strings - 0 difference");
            yield return new TestCaseData("random string", "gandom string", 1).SetName("Char change - 1 difference");
            yield return new TestCaseData("random string", "gangom string", 2).SetName("Two char change - 2 difference");
            yield return new TestCaseData("random string", "random strin", 1).SetName("Missing char at end - 1 difference");
            yield return new TestCaseData("random string", "andom string", 1).SetName("Missing char at beginning - 1 difference");
            yield return new TestCaseData("random string", "randomstring", 1).SetName("Missing char in middle - 1 difference");
            yield return new TestCaseData("random string", "rando'm string", 1).SetName("has extra char - 1 difference");
            yield return new TestCaseData("random string", "ranodm string", 1).SetName("chars wrong way round next too each other - 1 difference");
            yield return new TestCaseData("random string", "andomr string", 2).SetName("chars wrong way round further - 2 difference");
            yield return new TestCaseData("random string", "Random string", 1).SetName("Capitalization - 1 difference");
            yield return new TestCaseData("Don't know", "dont know", 2).SetName("Don't know example - 2 differences");
            yield return new TestCaseData("age", "ageBands", 5).SetName("age in ageBands - 5 differences");
        }
        
        [Test]
        [TestCaseSource("DemerauLevenshteinDistanceTestExamples")]
        [Parallelizable(ParallelScope.All)]
        public void FuzzyMatchDemerauLevenshteinDistance_WithGivenStrings_ExpectedNoOfDifferences(string str1, string str2, int expectedNoOfDifferences)
        {
            int noOfDifferences = FuzzyMatch.DamerauLevenshteinDistance(str1, str2);
            Assert.That(noOfDifferences, Is.EqualTo(expectedNoOfDifferences));
        }
        
        
        private static IEnumerable<TestCaseData> IsCloseFuzzyMatchWithNoOfDifferencesTestExamples()
        {
            yield return new TestCaseData("random string", "random string", 0, true).SetName("0 differences, with 0 tolerance - true");
            yield return new TestCaseData("random string", "random string", 1, true).SetName("0 differences, with 1 tolerance - true");
            yield return new TestCaseData("random string", "random strin", 0, false).SetName("1 differences, with 0 tolerance - false");
            yield return new TestCaseData("random string", "random strin", 1, true).SetName("1 differences, with 1 tolerance - true");
        }
        
        [Test]
        [TestCaseSource("IsCloseFuzzyMatchWithNoOfDifferencesTestExamples")]
        [Parallelizable(ParallelScope.All)]
        public void FuzzyMatchIsCloseFuzzyMatch_WithGivenStringsAndNoOfDifferences_ExpectedBoolean(string str1, string str2, int maxNoOfDifferences, bool expectedMatch)
        {
            bool isMatch = FuzzyMatch.IsFuzzyCloseMatch(str1, str2, maxNoOfDifferences);
            Assert.That(isMatch, Is.EqualTo(expectedMatch));
        }
        
        
        private static IEnumerable<TestCaseData> IsCloseFuzzyMatchWithThresholdTestExamples()
        {
            yield return new TestCaseData("random string", "random string", 1, true).SetName("0 differences, with 1 threshold - true");
            yield return new TestCaseData("random string", "random string", 0.999, true).SetName("0 differences, with 0.999 threshold - true");
            yield return new TestCaseData("random string", "random strin", 1, false).SetName("1 differences, with 1 threshold - false");
            yield return new TestCaseData("random string", "random strin", 0.9, true).SetName("1 differences, with 0.9 threshold - true");
            yield return new TestCaseData("random string", "random strin", 0.999, false).SetName("1 differences, with 0.999 threshold - false");
            yield return new TestCaseData("random string", "Not even close what a terrible example", 0, true).SetName("completely different with 0 threshold - true");
        }
        
        [Test]
        [TestCaseSource("IsCloseFuzzyMatchWithThresholdTestExamples")]
        [Parallelizable(ParallelScope.All)]
        public void FuzzyMatchIsCloseFuzzyMatch_WithGivenStringsAndThreshold_ExpectedBoolean(string str1, string str2, double minThreshold, bool expectedMatch)
        {
            bool isMatch = FuzzyMatch.IsFuzzyCloseMatch(str1, str2, minThreshold);
            Assert.That(isMatch, Is.EqualTo(expectedMatch));
        }
        
        
        private static IEnumerable<TestCaseData> NGramTestExamplesNoFuzzyMatching()
        {
            // simple tests to check logic
            yield return new TestCaseData("apple", "apple", 2, (double) 6/6).SetName("apple/apple with 2Gram size - 100% similarity");
            yield return new TestCaseData("apple", "apple", 3, (double) 5/5).SetName("apple/apple with 3Gram size - 100% similarity");
            yield return new TestCaseData("apple", "appel", 2, (double) 2/6).SetName("apple/appel with 2Gram size - 33% similarity");
            yield return new TestCaseData("apple", "appel", 3, (double) 1/5).SetName("apple/appel with 3Gram size - 20% similarity");
            yield return new TestCaseData("banana", "appel", 2, (double) 0/7).SetName("banana/appel with 2Gram size - 0% similarity");
            yield return new TestCaseData("banana", "appel", 3, (double) 0/6).SetName("banana/appel with 3Gram size - 0% similarity");
            yield return new TestCaseData("pineapple", "appel", 2, (double) 2/10).SetName("pineapple/appel with 2Gram size - 20% similarity");
            yield return new TestCaseData("pineapple", "appel", 3, (double) 1/9).SetName("pineapple/appel with 3Gram size - 11% similarity");
            yield return new TestCaseData("pineapple", "apple", 2, (double) 4/8).SetName("pineapple/apple with 2Gram size - 50% similarity");
            yield return new TestCaseData("pineapple", "apple", 3, (double) 3/7).SetName("pineapple/apple with 3Gram size - 42% similarity");
            
            // more real world tests, 2 chars seems to increase percent for both valid and invalid
            yield return new TestCaseData("reg_aus", "reg", 2, (double) 2/6).SetName("reg_aus/reg with 2Gram size - 33% similarity");
            yield return new TestCaseData("reg_aus", "reg", 3, (double) 1/5).SetName("reg_aus/reg with 3Gram size - 20% similarity");
            yield return new TestCaseData("individual_vs_regular", "reg", 2, (double) 2/20).SetName("individual_vs_regular/reg with 2Gram size - 10% similarity");
            yield return new TestCaseData("individual_vs_regular", "reg", 3, (double) 1/19).SetName("individual_vs_regular/reg with 3Gram size - 5% similarity");
            yield return new TestCaseData("regular_giving", "reg", 2, (double) 2/13).SetName("regular_giving/reg with 2Gram size - 15% similarity");
            yield return new TestCaseData("regular_giving", "reg", 3, (double) 1/12).SetName("regular_giving/reg with 3Gram size - 8% similarity");
            yield return new TestCaseData("german_region_code", "region", 2, (double) 5/16).SetName("german_region_code/region with 2Gram size - 31% similarity");
            yield return new TestCaseData("german_region_code", "region", 3, (double) 4/16).SetName("german_region_code/region with 3Gram size - 25% similarity");
            
            // Seems to fail on spelling mistakes/typos
            yield return new TestCaseData("i don't know", "don't", 2, (double) 4/11).SetName("i don't know/don't with 2Gram size - 36% similarity");
            yield return new TestCaseData("i don't know", "don't", 3, (double) 3/10).SetName("i don't know/don't with 3Gram size - 33% similarity");
            yield return new TestCaseData("i dont know", "don't", 2, (double) 2/12).SetName("i dont know/don't with 2Gram size - 16% similarity");
            yield return new TestCaseData("i dont know", "don't", 3, (double) 1/11).SetName("i dont know/don't with 3Gram size - 9% similarity");
            yield return new TestCaseData("i don't know", "i don't know", 2, (double) 11/11).SetName("i don't know/i don't know with 2Gram size - 100% similarity");
            yield return new TestCaseData("i don't know", "i don't know", 3, (double) 10/10).SetName("i don't know/i don't know with 3Gram size - 100% similarity");
            yield return new TestCaseData("i dont know", "i dont know", 2, (double) 10/10).SetName("i dont know/i dont know with 2Gram size - 100% similarity");
            yield return new TestCaseData("i dont know", "i dont know", 3, (double) 9/9).SetName("i dont know/i dont know with 3Gram size - 100% similarity");
        }
        
        [Test]
        [TestCaseSource("NGramTestExamplesNoFuzzyMatching")]
        [Parallelizable(ParallelScope.All)]
        public void FuzzyMatchNGramPatternMatching_WithGivenStringsAndSimilarityThresholdButNoFuzzyDifference_ExpectedMatch(string textInput, string matchPattern, int nGramSize, double expectedSimilarity)
        {
            const int fuzzyNoOfDifferences = 0;
            double similarity = FuzzyMatch.FuzzySearchNGram(textInput, matchPattern, nGramSize, fuzzyNoOfDifferences);
            Assert.That(similarity, Is.EqualTo(expectedSimilarity).Within(1).Ulps);
        }
        
        
        private static IEnumerable<TestCaseData> NGramTestExamplesWithFuzzyMatching()
        {
            // Simple examples to compare with above
            yield return new TestCaseData("apple", "apple", (double) 5/5).SetName("apple/apple with 3Gram size - 100% similarity");
            yield return new TestCaseData("apple", "appel", (double) 3/5).SetName("apple/appel with 3Gram size - 60% similarity");
            yield return new TestCaseData("banana", "appel", (double) 1/6).SetName("banana/appel with 3Gram size - 16% similarity");
            yield return new TestCaseData("pineapple", "appel", (double) 3/9).SetName("pineapple/appel with 3Gram size - 33% similarity");
            yield return new TestCaseData("pineapple", "apple", (double) 3/7).SetName("pineapple/apple with 3Gram size - 42% similarity");
            
            // doesn't handle smaller examples as well but handles most cases fine
            yield return new TestCaseData("reg_aus", "reg", (double) 1/5).SetName("reg_aus/reg with 3Gram size - 20% similarity");
            yield return new TestCaseData("individual_vs_regular", "reg", (double) 1/19).SetName("individual_vs_regular/reg with 3Gram size - 5% similarity");
            yield return new TestCaseData("regular_giving", "reg", (double) 1/12).SetName("regular_giving/reg with 3Gram size - 8% similarity");
            yield return new TestCaseData("german_region_code", "region", (double) 4/16).SetName("german_region_code/region with 3Gram size - 25% similarity");
            
            // Handles typos/spelling mistakes better
            yield return new TestCaseData("i don't know", "don't", (double) 3/10).SetName("i don't know/don't with 3Gram size - 30% similarity");
            yield return new TestCaseData("i dont know", "don't", (double) 3/11).SetName("i dont know/don't with 3Gram size - 27% similarity");
            yield return new TestCaseData("i don't know", "i don't know", (double) 10/10).SetName("i don't know/i don't know with 3Gram size - 100% similarity");
            yield return new TestCaseData("i dont know", "i dont know", (double) 9/9).SetName("i dont know/i dont know with 3Gram size - 100% similarity");
        }
        
        [Test]
        [TestCaseSource("NGramTestExamplesWithFuzzyMatching")]
        [Parallelizable(ParallelScope.All)]
        public void FuzzyMatchNGramPatternMatching_WithGivenStringsAndSimilarityWithFuzzy_ExpectedMatch(string textInput, string matchPattern, double expectedSimilarity)
        {
            const int fuzzyNoOfDifferences = 2;
            const int nGramSize = 3;
            double similarity = FuzzyMatch.FuzzySearchNGram(textInput, matchPattern, nGramSize, fuzzyNoOfDifferences);
            Assert.That(similarity, Is.EqualTo(expectedSimilarity).Within(1).Ulps);
        }

        
        private static IEnumerable<TestCaseData> RealWorldAgeNumericExamples()
        {
            yield return new TestCaseData("age", "Age", true);
            yield return new TestCaseData("Age", "Age", true);
            yield return new TestCaseData("AgeCheck", "Age", false);
            yield return new TestCaseData("age_check", "Age", false);
            yield return new TestCaseData("AgeCheck", "Age_check", true);
            yield return new TestCaseData("age_check", "Age_check", true);
            yield return new TestCaseData("How old are you?", "How old are you?", true);
            yield return new TestCaseData("About: How old are you?", "How old are you?", true);
            yield return new TestCaseData("About: How old are you", "How old are you?", true);
            yield return new TestCaseData("About: What is your age?", "What is your age?", true);
            yield return new TestCaseData("About: What is your age?", "What is your age", true);
            yield return new TestCaseData("How old is your car?", "How old are you?", false);
            yield return new TestCaseData("What is your name?", "What is your age", true);
            yield return new TestCaseData("Logo_image", "Age", false);
            yield return new TestCaseData("Image", "Age", false);
            yield return new TestCaseData("Firstly, how old are you?", "How old are you?", true);
            yield return new TestCaseData("DO NOT READ OUT: How old are you?", "How old are you?", false); //We probs want this true, but I think the difference is too large and is an acceptable edge case
            yield return new TestCaseData("About you: Please can you tell me your age at your last birthday?", "How old are you?", false);
            yield return new TestCaseData("About you: Please can you tell me your age at your last birthday?", "Please can you tell me your age at your last birthday?", true);
        }
        
        [Test]
        [TestCaseSource("RealWorldAgeNumericExamples")]
        [Parallelizable(ParallelScope.All)]
        public void AdaptiveFuzzyMatching_WithRealWorldExamples_WorksAsExpected(string textInput, string matchPattern, bool expectedIsMatch)
        {
            const double threshold = 0.6;
            bool isMatch = FuzzyMatch.AdaptiveFuzzySimilarityMatch(textInput, matchPattern, threshold);
            Assert.That(isMatch, Is.EqualTo(expectedIsMatch));
        }
    }
}