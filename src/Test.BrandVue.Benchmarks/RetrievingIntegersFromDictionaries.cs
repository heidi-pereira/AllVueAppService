using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using NUnit.Framework;

namespace Test.BrandVue.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80, warmupCount: 1, iterationCount: 5)]
    [MemoryDiagnoser]
    [MarkdownExporterAttribute.GitHub]
    public class RetrievingIntegersFromDictionaries
    {
        [Params(1000 * 1000)]
        public int NumberToParse { get; set; }

        //  TODO: getting integers directly from a generic <string, object> dictionary with a cast

        //  TODO: getting integers from a generic <string, int> dictionary

        //  TODO: getting integers from a generic <string, int> dictionary where 90% of keys aren't present, using TryGetValue
        
        
        [Benchmark(Baseline = true)] //Fastest
        public void GenericStringObjectDictionaryWithCast()
        {
            var dictionary = new Dictionary<string, object>();
            var keys = "abcdefghij";
            int value;

            for (value = 0; value < 10; ++value)
            {
                dictionary[keys.Substring(value, 1)] = value;
            }

            for (var index = 0; index < NumberToParse; ++index)
            {
                foreach (var key in dictionary.Keys)
                {
                    value += (int) dictionary[key];
                }
            }

            Console.WriteLine(value);
        }
        
        [Benchmark]
        public void GenericStringIntegerDictionary()
        {
            var dictionary = new Dictionary<string, int>();
            var keys = "abcdefghij";
            int value;

            for (value = 0; value < 10; ++value)
            {
                dictionary[keys.Substring(value, 1)] = value;
            }

            for (var index = 0; index < NumberToParse; ++index)
            {
                foreach (var key in dictionary.Keys)
                {
                    value += dictionary[key];
                }
            }

            Console.WriteLine(value);
        }

        [Benchmark]
        public void GenericStringObjectDictionary()
        {
            var dictionary = new Dictionary<string, object>();
            var keys = "abcdefghij";
            int value;

            for (value = 0; value < 10; ++value)
            {
                dictionary[keys.Substring(value, 1)] = value.ToString();
            }

            for (var index = 0; index < NumberToParse; ++index)
            {
                foreach (var key in dictionary.Keys)
                {
                    var str = dictionary[key].ToString();
                    value += int.Parse(str);
                }
            }

            Console.WriteLine(value);
        }

        
        [Benchmark]
        public void Parse_StringStringDictionary()
        {
            var dictionary = new Dictionary<string, string>();
            var keys = "abcdefghij";
            int value;

            for (value = 0; value < 10; ++value)
            {
                dictionary[keys.Substring(value, 1)] = value.ToString();
            }

            for (var index = 0; index < NumberToParse; ++index)
            {
                foreach (var key in dictionary.Keys)
                {
                    var str = dictionary[key];
                    value += int.Parse(str);
                }
            }

            Console.WriteLine(value);
        }
        
        [Benchmark] // So slow that I've adjusted this one to parse 1k times fewer...
        public void ParseCatch_1kTimesFewer_StringObjectDictionary_90PercentOfStringsAreEmpty()
        {
            var smallerNumberToParseBecauseThisIsSloSlow = NumberToParse / 1000;

            var dictionary = new Dictionary<string, object>();
            var keys = "abcdefghij";
            int value;

            for (value = 0; value < 10; ++value)
            {
                dictionary[keys.Substring(value, 1)] = value == 0 ? value.ToString() : "";
            }

            for (var index = 0; index < smallerNumberToParseBecauseThisIsSloSlow; ++index)
            {
                foreach (var key in dictionary.Keys)
                {
                    var str = dictionary[key].ToString();
                    try
                    {
                        value += int.Parse(str);
                    }
                    catch (FormatException)
                    {
                    }
                }
            }

            Console.WriteLine(value);
        }
        
        [Benchmark]
        public void TryParse_StringObjectDictionary_90PercentOfStringsAreEmpty()
        {

            var dictionary = new Dictionary<string, object>();
            var keys = "abcdefghij";
            int value;

            for (value = 0; value < 10; ++value)
            {
                dictionary[keys.Substring(value, 1)] = value == 0 ? value.ToString() : "";
            }

            for (var index = 0; index < NumberToParse; ++index)
            {
                foreach (var key in dictionary.Keys)
                {
                    var str = dictionary[key].ToString();
                    if (int.TryParse(str, out int addition))
                    {
                        value += addition;
                    }
                }
            }

            Console.WriteLine(value);
        }
        
        [Benchmark]
        public void TryParse_StringStringDictionary_90PercentOfStringsAreEmpty()
        {
            var dictionary = new Dictionary<string, string>();
            var keys = "abcdefghij";
            int value;

            for (value = 0; value < 10; ++value)
            {
                dictionary[keys.Substring(value, 1)] = value == 0 ? value.ToString() : "";
            }

            for (var index = 0; index < NumberToParse; ++index)
            {
                foreach (var key in dictionary.Keys)
                {
                    var str = dictionary[key];
                    if (int.TryParse(str, out int addition))
                    {
                        value += addition;
                    }
                }
            }

            Console.WriteLine(value);
        }
        
        [Benchmark]
        public void TryGet_StringIntegerDictionary_90PercentOfKeysAreNotPresent()
        {
            var dictionary = new Dictionary<string, int>();
            var keys = new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j" };

            dictionary["a"] = 1;

            var total = 0;
            for (var index = 0; index < NumberToParse; ++index)
            {
                foreach (var key in keys)
                {
                    if (dictionary.TryGetValue(key, out int retrieved))
                    {
                        total += retrieved;
                    }
                }
            }

            Console.WriteLine(total);
        }
    }
}
