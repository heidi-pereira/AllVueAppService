using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.Utils;

namespace BrandVue.SourceData.AutoGeneration;

public class NumericFuzzyMatchers
{
    private readonly List<string> _ageQuestionVarCodes = new (){"Age", "Age_check"};
    private readonly List<string> _ageQuestionTexts = new (){
        "How old are you?", 
        "What is your age?", 
        "Please can you tell me your age at your last birthday?",
    };

    private readonly List<string> _noOfChildrenQuestionVarCodes = new (){"No_of_kids"};
    private readonly List<string> _noOfChildrenQuestionTexts = new (){
        "How many children live in your household?", 
        "Please enter the number of girls and boys in your household under the age of 18", 
        "How many children do you have living with you at home?",
        "How many children under the age of 18 do you have living with you at home?",
        "How many people under the age of 18 live in your household?",
        "How many children do you have that are school aged?",
        "How many children do you have in your household?",
        };
    
    public bool AgeFuzzyMatcher(Question numericQuestion)
    {
        return _ageQuestionVarCodes.Any(varCode => FuzzyMatch.IsFuzzyCloseMatch(numericQuestion.VarCode.ToLower(), varCode.ToLower(), 0.9)) || 
               _ageQuestionTexts.Any(helpText => FuzzyMatch.AdaptiveFuzzySimilarityMatch(numericQuestion.QuestionText, helpText, 0.5));
    }
    
    public bool NoOfChildrenFuzzyMatcher(Question numericQuestion)
    {
        return _noOfChildrenQuestionVarCodes.Any(varCode => FuzzyMatch.IsFuzzyCloseMatch(numericQuestion.VarCode.ToLower(), varCode.ToLower(), 0.9)) || 
               _noOfChildrenQuestionTexts.Any(helpText => FuzzyMatch.AdaptiveFuzzySimilarityMatch(numericQuestion.QuestionText, helpText, 0.5));
    }
}