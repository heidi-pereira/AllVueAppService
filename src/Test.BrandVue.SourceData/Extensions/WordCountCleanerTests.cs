using System;
using System.Linq;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Utils;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Extensions
{
    [TestFixture]
    public class WordCountCleanerTests
    {
        [Test]
        public void Test_RemovesPunctuationAndWhiteSpace_GroupsResultsToOneResult()
        {
            var results = new[]
            {
                new WeightedWordCount { Text = "   It was amazing! ", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "It was, amazing.", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = " It was amazing", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "   %It,  was $amazing! ", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "  It*, ^$ w(as $amaz%ing! ", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "It, ? w{as $amaz]ing! ", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "IT, ? w{As $amAz]iNg! ", Result = 0.5, UnweightedResult = 1 }
            };

            var cleanedResults = results.CleanTextAndRegroup();

            Assert.That(cleanedResults.Count() == 1, Is.True);
            Assert.That(cleanedResults.Any(r => !r.Text.Equals("It was amazing")), Is.False);
            var result = cleanedResults.First();
            Assert.That(Equals(result.Text, "It was amazing"), Is.True);
            Assert.That(result.Result, Is.EqualTo(3.5).Within(0.0001));
            Assert.That(result.UnweightedResult, Is.EqualTo(7).Within(0.0001));
        }

        [Test]
        public void Test_RemoveExcludedWords_ShouldRemoveAllButOneResult()
        {
            var badWords = new[]
            {
                "badword",
                "horribleword",
                "hideousword"
            };

            var results = new[]
            {
                new WeightedWordCount { Text = "   It was badword! ", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "really badword", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "   real%ly ba*dword", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = " absolutely su&per( badw^ord real%ly ba*dword", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "horribleword terrible", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "    hor%rib^le*wo£rd   ", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "an awful really hideousword, terrible", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "  really     hideo%uswo)[rd   ter&rible  ", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "goodword", Result = 0.5, UnweightedResult = 1 },
            };

            var cleanedResults = results
                .CleanTextAndRegroup()
                .ApplyExclusionList(badWords);

            Assert.That(cleanedResults.Count() == 1, Is.True);
            Assert.That(cleanedResults.Any(r => badWords.Any(badWord => r.Text.IndexOf(badWord, StringComparison.CurrentCultureIgnoreCase) >= 0)), Is.False);
            var result = cleanedResults.First();
            Assert.That(Equals(result.Text, "goodword"), Is.True);
        }


        [Test]
        public void Test_UnwantedCharactersOnly_RemovesResults()
        {
            var results = new[]
            {
                new WeightedWordCount {Text = "   ! ", Result = 0.5, UnweightedResult = 1},
                new WeightedWordCount {Text = "?", Result = 0.5, UnweightedResult = 1},
                new WeightedWordCount {Text = " ", Result = 0.5, UnweightedResult = 1},
                new WeightedWordCount {Text = "", Result = 0.5, UnweightedResult = 1},
                new WeightedWordCount {Text = "👩🏽‍🚒", Result = 0.5, UnweightedResult = 1}
            };

            var cleanedResults = results.CleanTextAndRegroup();

            Assert.That(!cleanedResults.Any(), Is.True);
        }

        [Test]
        public void Test_Exactmatch_Removes_Only_Exact_Match()
        {
            var results = new[]
            {
                new WeightedWordCount { Text = "!Noodles", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "Noodles!", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "Noodles", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = " Noodles", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = " No!!!", Result = 0.5, UnweightedResult = 1 },
            };

            var cleanedResults = results.CleanTextAndRegroup().ApplyExclusionList(new []{"=No"});

            Assert.That(1, Is.EqualTo(cleanedResults.Count()));
            Assert.That(4, Is.EqualTo(cleanedResults.Single().UnweightedResult));
        }
        
        [Test]
        public void Test_EmojisAreRemoved()
        {
            var results = new[]
            {
                new WeightedWordCount { Text = "pizza👩🏽‍🚒🍕🐔🍆", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "pi🍕🐔🍆👩🏽‍🚒zza", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "🍕🐔🍆p👩🏽‍🚒izza", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "🍕🐔🍆👩🏽‍🚒 pizza", Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = "pizza 🍕👩🏽‍🚒🐔🍆", Result = 0.5, UnweightedResult = 1 }
            };

            var cleanedResults = results.CleanTextAndRegroup().ToArray();

            Assert.That(cleanedResults.Count, Is.EqualTo(1));
            Assert.That(cleanedResults.Single().Text, Is.EqualTo("pizza"));
            Assert.That(cleanedResults.Single().UnweightedResult, Is.EqualTo(5));
        }
        
        [Test]
        public void Test_MostSpecialCharactersAreSupported_SomeRareCharactersAreRemoved()
        {
            var supportedSpecialCharacters = "TheseAreSupported ÂÃÄÅÆÇLÈÉÊËÌÍÎÏÐÑÒÓÔÕÖØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõöLøùúûüýþÿ测试文本टेस्टटेक्स्टテストテキスト테스트 텍스트טקסט בדיקה";
            
            // From outside Basic Multilingual Plane, e.g. some ancient Turkish, traditional Chinese, emoji symbols.
            var unsupportedSpecialCharacters = "TheseAreRemoved 𐱃𨭎𠬠𩷶🍕🐔🍆";
            var results = new[]
            {
                new WeightedWordCount { Text = unsupportedSpecialCharacters, Result = 0.5, UnweightedResult = 1 },
                new WeightedWordCount { Text = supportedSpecialCharacters, Result = 0.5, UnweightedResult = 1 }
            };

            var cleanedResults = results.CleanTextAndRegroup().ToArray();

            Assert.That(cleanedResults.Count, Is.EqualTo(2));
            Assert.That(cleanedResults.Count(cr => cr.Text.Equals("TheseAreRemoved")), Is.EqualTo(1));
            Assert.That(cleanedResults.Count(cr => cr.Text.Equals(supportedSpecialCharacters)), Is.EqualTo(1));
        }
    }
}
