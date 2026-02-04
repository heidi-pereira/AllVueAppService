using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Linq;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.ResponseRepository;
using Microsoft.Extensions.Logging;
using static BrandVue.EntityFramework.ResponseRepository.EntityInstanceColumnLocation;

namespace BrandVue.EntityFramework.Answers.Model
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class Question
    {
        private string DebuggerDisplay => $"{VarCode}({string.Join(", ", new[]{SectionChoiceSet?.Name, PageChoiceSet?.Name, QuestionChoiceSet?.Name, AnswerChoiceSet?.Name}.YieldNonNullEntries())})";
        private const string RadioType = "RADIO";
        private static readonly string[] AllSingleChoiceTypes = new[] {"COMBO", "DROPZONE", RadioType};
        public int QuestionId { get; set; }
        [Required]
        public int SurveyId { get; set; }
        [Required, MaxLength(2000)]
        public string QuestionText { get; set; }
        [ForeignKey("SectionChoiceSetId")]
        public ChoiceSet SectionChoiceSet { get; set; }
        [ForeignKey("PageChoiceSetId")]
        public ChoiceSet PageChoiceSet { get; set; }
        [ForeignKey("QuestionChoiceSetId")]
        public ChoiceSet QuestionChoiceSet { get; set; }
        [ForeignKey("AnswerChoiceSetId")]
        public ChoiceSet AnswerChoiceSet { get; set; }
        [Required, MaxLength(100)]
        public string VarCode { get; set; }

        /// <remarks>Stop directly using this stringly typed property from outside the Question class. Prefer QuestionType or other helper properties</remarks>
        [MaxLength(50)]
        public string MasterType { get; set; }
        public int? ItemNumber { get; set; }
        public int? MaximumValue { get; set; }
        public int? MinimumValue { get; set; }
        public int? DontKnowValue { get; set; }
        public string NumberFormat { get; set; }
        [Required]
        public bool QuestionShownInSurvey { get; set; }
        public int EntityCount => AvailableQuestionChoiceSets().Count();
        public IEnumerable<(ChoiceSet ChoiceSet, DbLocation Location)> AvailableQuestionChoiceSets() => SupportsSingleEntryChoiceSets ? GetChoiceSetsWithAtLeastOneChoice(GetAllChoiceSets()) : GetChoiceSetsWithAtLeastTwoChoices(GetAllChoiceSets());
        public IEnumerable<(ChoiceSet choiceSet, ChoiceSetType type)> ValuedChoiceSetsWithType => new[] {(SectionChoiceSet, ChoiceSetType.SectionChoiceSet), (PageChoiceSet, ChoiceSetType.PageChoiceSet), (QuestionChoiceSet, ChoiceSetType.QuestionChoiceSet), (AnswerChoiceSet, ChoiceSetType.AnswerChoiceSet)}.Where(n => n.Item1 != null);
        public QuestionTypeSpecificData OptionalData { get; set; }

        /*
         * Currently this only handles single entity questions.
         * For multi-entity questions (e.g. carousel, grids, ranking) more thought will be needed
         * 
         * See https://github.com/Savanta-Tech/SurveyPlatform/blob/b5203c66ff1c05df5b62c623a18e13c80aa7effd/QServer/DataExtraction/MetaExtractor.cs#L533
         *
         * As of June 2023 from database
         * RADIO,DROPZONE or COMBO questions that have Numberformat are generally mistakes
         * There are no CHECKBOX questions with number formats
         * There are no TAG questions with specified number format
         * There are no TEXTMULTI questions
         */
        public MainQuestionType QuestionType => MasterType switch
        {
            "RADIO" or "DROPZONE" or "COMBO" => MainQuestionType.SingleChoice,
            "CHECKBOX" => MainQuestionType.MultipleChoice,
            "TEXTENTRY" or "TEXTMULTI" or "TAG" =>
                string.IsNullOrWhiteSpace(NumberFormat) ? MainQuestionType.Text : MainQuestionType.Value,
            "SLIDER" or "WHEEL" => MainQuestionType.Value,
            "HEATMAPIMAGE" => MainQuestionType.HeatmapImage,
            _ => MainQuestionType.Unknown,
        };

        public bool SupportsSingleEntryChoiceSets => QuestionType == MainQuestionType.HeatmapImage;
        public bool IsNumeric => QuestionType == MainQuestionType.Value;

        public bool MasterTypeIsText => MasterType switch
        {
            "TEXTENTRY" or "TEXTMULTI" or "TAG" or "HEATMAPIMAGE" => true,
            _ => false
        };

        public DbLocation GetDbLocationFromDataColumn(EntityInstanceColumnLocation location, ILogger logger,
            bool allowText = false)
        {
            if (allowText && location == text) return DbLocation.AnswerText;
            var matches = GetColumnMappings(allowText).Where(m => m.DataCol == location).Select(m => m.AnswerCol).Distinct();
            if (matches.OnlyOrDefault() is {} match) return match;
            logger.LogWarning($"For question with varcode {VarCode} could not get unique dblocation for {location} {LoggingTags.Question} {LoggingTags.Config}");
            return null;
        }

        public IEnumerable<(ChoiceSet ChoiceSet, DbLocation Location)> GetAllChoiceSets()
        {
            yield return (SectionChoiceSet, DbLocation.SectionEntity);
            yield return (PageChoiceSet, DbLocation.PageEntity);
            yield return (QuestionChoiceSet, DbLocation.QuestionEntity);
            yield return (AnswerChoiceSet, DbLocation.AnswerEntity);
        }

        private IEnumerable<(DbLocation AnswerCol, EntityInstanceColumnLocation DataCol)> GetColumnMappings(bool allowText)
        {
            // C# version of this SQL: https://github.com/MIG-Global/SurveyPlatform/blob/master/DatabaseSchema/vue/Stored%20Procedures/SyncSurveyData.sql#L117

            var singleChoiceOptValueTypes = new[] {"COMBO", "DROPZONE"};
            var textTypes = new[] {"TEXTENTRY", "TAG"};
            var pageFromCol = PageChoiceSet is null ? default : CH1;
            var questionFromCol = QuestionChoiceSet is null ? default :
                PageChoiceSet is {} ? CH2 :
                CH1;
            var answerChoiceFromCol = AnswerChoiceSet is null ? default :
                singleChoiceOptValueTypes.Contains(MasterType) ? optValue :
                QuestionChoiceSet is {} ? CH2 :
                PageChoiceSet is {} ? CH2 :
                CH1;
            var answerValueFromCol = textTypes.Contains(MasterType) ? text
                : !AllSingleChoiceTypes.Contains(MasterType) ? optValue
                : default;

            yield return (DbLocation.PageEntity, pageFromCol);
            yield return (DbLocation.QuestionEntity, questionFromCol);
            yield return (DbLocation.AnswerEntity, answerChoiceFromCol);
            yield return !allowText && answerValueFromCol == text
                ? (DbLocation.AnswerShort, optValue)
                : (DbLocation.AnswerShort, answerValueFromCol);

            // Special BrandVue case intentionally not covered in SQL:
            if (answerChoiceFromCol != default && answerChoiceFromCol != optValue && MasterType == RadioType)
            {
                yield return (DbLocation.ConstantOne, optValue);
            }
        }

        /// <summary>Use this method to make our treatment of empty named choices within choicesets consistent.</summary>
        /// <remarks>
        /// Not sure why 1% of our choice sets have multiple nameless choices. 
        /// SELECT choicesetid, max(surveyid) FROM [VueExport].[vue].[Choices] 
        /// WHERE Name is null or LTRIM(RTRIM(Name))=''
        /// GROUP BY choicesetid
        /// HAVING COUNT(1) &gt; 1
        /// ORDER BY MAX(surveyid) DESC
        /// </remarks>
        public static IEnumerable<(ChoiceSet ChoiceSet, DbLocation Location)> GetChoiceSetsWithAtLeastTwoChoices(IEnumerable<(ChoiceSet ChoiceSet, DbLocation Location)> canonicalChoiceSets) =>
            canonicalChoiceSets.Where(t => t.ChoiceSet is {} cs && cs.Choices.Count(c => !string.IsNullOrWhiteSpace(c.GetDisplayName())) > 1);
        public static IEnumerable<(ChoiceSet ChoiceSet, DbLocation Location)> GetChoiceSetsWithAtLeastOneChoice(IEnumerable<(ChoiceSet ChoiceSet, DbLocation Location)> canonicalChoiceSets) =>
            canonicalChoiceSets.Where(t => t.ChoiceSet is { } cs && cs.Choices.Count(c => !string.IsNullOrWhiteSpace(c.GetDisplayName())) > 0);
    }
}
