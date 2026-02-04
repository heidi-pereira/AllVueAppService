namespace BrandVue.SourceData.Utils;


public static class FuzzyMatch
{
    // Works out how many times a string has to be manipulated to get a match. Unlike the normal Levenshtein, position matters and anagrams won't match
    public static int DamerauLevenshteinDistance(string str1, string str2)
    {
        int len1 = str1.Length;
        int len2 = str2.Length;

        if (len1 == 0) return len2;
        if (len2 == 0) return len1;

        int[,] d = new int[len1 + 1, len2 + 1];

        for (int i = 0; i <= len1; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= len2; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                int cost = (str1[i - 1] == str2[j - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(
                        d[i - 1, j] + 1, // deletion
                        d[i, j - 1] + 1 // insertion
                    ),
                    d[i - 1, j - 1] + cost // substitution
                );

                if (i > 1 && j > 1 && str1[i - 1] == str2[j - 2] && str1[i - 2] == str2[j - 1])
                {
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + cost); // transposition
                }
            }
        }

        return d[len1, len2];
    }
    
    public static bool IsFuzzyCloseMatch(string str1, string str2, int noOfDifferences)
    {
        return DamerauLevenshteinDistance(str1, str2) <= noOfDifferences;
    }
    
    public static bool IsFuzzyCloseMatch(string str1, string str2, double threshold)
    {
        return (double) DamerauLevenshteinDistance(str1, str2) / Math.Max(str1.Length, str2.Length) <= (double) 1 - threshold;
    }

    // Can split strings of different lengths and compares the substrings, if the fuzzyDifference isn't 0 it will use the above fuzzy logic to compare.
    public static double FuzzySearchNGram(string textInput, string matchPattern, int nGramSize, int fuzzyNoOfDifferences)
    {
        ValidateInputs(textInput, matchPattern, nGramSize, fuzzyNoOfDifferences);
        
        var textNGrams = GetNGrams(textInput, nGramSize);
        var patternNGrams = GetNGrams(matchPattern, nGramSize);
        
        int total = textNGrams.Concat(patternNGrams).Distinct().Count();
        int count = patternNGrams.Count(patterNGram => textNGrams.Exists(textNGram => HasFuzzySimilarity(textNGram, patterNGram, fuzzyNoOfDifferences)));
        
        return (double) count / total;
    }

    public static bool IsNGramSimilarityMatch(string textInput, string matchPattern, int nGramSize, int fuzzyNoOfDifferences, double similarityThreshold)
    {
        return FuzzySearchNGram(textInput, matchPattern, nGramSize, fuzzyNoOfDifferences) >= similarityThreshold;
    }

    // NGram algorithm isn't as reliable with small matchPatterns and nGrams, so i have tuned the parameters depending on the input
    // this may need fine tuning as we add more real world examples (and maybe make the scaling smarter than trial and error).
    // - nGramSize is the size of each substring, the smaller it is the more chance parts of the pattern and textInput can match, can lead to similer examples matching when you don't want to e.g. `Whats your age?` and `Whats your nane?`
    // - fuzzyNoOfDifferences is the number of spelling mistakes allowed, if it is too high compared to the nGram it will allow garbage to match. Helps with examples like: `age_check` and `AgeCheck`
    // - threshold is the max percent that it can find similar, in the two strings, used for finding matches in a longer string.
    public static bool AdaptiveFuzzySimilarityMatch(string textInput, string matchPattern, double threshold)
    {
        int nGramSize = GetAdaptiveNGram(textInput, matchPattern);
        int fuzzyNoOfDifferences = GetAdaptiveFuzzyDifference(nGramSize); 
        return IsNGramSimilarityMatch(textInput.ToLower(), matchPattern.ToLower(), nGramSize, fuzzyNoOfDifferences, threshold);
    }

    private static int GetAdaptiveNGram(string textInput, string matchPattern)
    {
        int shortestAllowableTextLength = 10;
        int minLength = Math.Min(textInput.Length, matchPattern.Length);
        int lengthDifference = Math.Abs(textInput.Length - matchPattern.Length);
        if (minLength < shortestAllowableTextLength && lengthDifference <= 5)
        {
            return minLength;
        }

        return minLength switch
        {
            > 15 => minLength / 5,
            > 8 => minLength / 3,
            < 4 => minLength,
            _ => minLength / 2
        };
    }

    private static int GetAdaptiveFuzzyDifference(int nGramSize)
    {
        if (nGramSize < 4) return 1;
        if (nGramSize < 6) return 2;
        return 3;
    }

    private static bool HasFuzzySimilarity(string textNGram, string patterNGram, int fuzzyNoOfDifferences)
    {
        if (fuzzyNoOfDifferences == 0)
        {
            return textNGram.Equals(patterNGram);
        }
        return IsFuzzyCloseMatch(textNGram, patterNGram, fuzzyNoOfDifferences);
    }
    
    private static void ValidateInputs(string textInput, string matchPattern, int nGramSize, int fuzzyNoOfDifferences)
    {
        int minLength = Math.Min(textInput.Length, matchPattern.Length);
        if (nGramSize < 0 || nGramSize > minLength)
        {
            throw new Exception($"nGram ({nGramSize}) size has to be higher than 0 and smaller than the text inputs minimum lengths ({minLength})");
        }
        if (fuzzyNoOfDifferences < 0 || fuzzyNoOfDifferences >= nGramSize)
        {
            throw new Exception($"fuzzyNoOfDifferences ({fuzzyNoOfDifferences}) cant be negative and has to be less than nGramSize ({nGramSize})");
        }
        if (textInput.Length < nGramSize || matchPattern.Length < nGramSize)
        {
            throw new Exception($"textInput length ({textInput.Length}) and matchPattern length ({matchPattern.Length}) has to be equal to or greater than nGramSize ({nGramSize})");
        }
    }
    
    private static List<string> GetNGrams(string text, int n)
    {
        var nGrams = new List<string>();
        for (int i = 0; i < text.Length - n + 1; i++)
        {
            nGrams.Add(text.Substring(i, n));
        }
        return nGrams;
    }
}
