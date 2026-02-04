using BrandVue.EntityFramework.Answers.Model;
using BrandVue.PublicApi.Extensions;
using BrandVue.PublicApi.Models;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;

namespace BrandVue.PublicApi.Services
{
    public class QuestionConverterAnswersTable
    {
        public static IEnumerable<QuestionDescriptor> CreateApiQuestionDescriptorsFromResponseFieldDescriptors(
            IOrderedEnumerable<ResponseFieldDescriptor> responseFieldDescriptors, Subset subset)
        {
            var fieldDescriptorsForThisSubset = responseFieldDescriptors.Where(rfd => rfd.GetDataAccessModel(subset.Id).QuestionModel != null);
            var questionDescriptors = fieldDescriptorsForThisSubset.Select(rfd => new QuestionDescriptor
            {
                QuestionId = rfd.Name,
                QuestionText = rfd.GetDataAccessModel(subset.Id).QuestionModel.QuestionText,
                AnswerSpec = CreateAnswer(rfd, subset),
                Classes = rfd.EntityCombination.ToOrderedEntityNames().ToArray()
            });
            return questionDescriptors;
        }

        private static QuestionAnswer CreateAnswer(ResponseFieldDescriptor rfd, Subset subset)
        {
            if (rfd.GetValueChoiceSetOrNull(subset.Id) is {} choiceSet)
            {
                return CreateMultipleChoiceAnswer(choiceSet);
            }

            var fieldDefinitionModel = rfd.GetDataAccessModel(subset.Id);
            return fieldDefinitionModel.ValueIsOpenText ? CreateQuestionTextAnswer() : CreateValueAnswer(fieldDefinitionModel);
        }

        private static QuestionAnswer CreateQuestionTextAnswer() => new QuestionTextAnswer();

        private static QuestionAnswer CreateValueAnswer(FieldDefinitionModel fieldDefinitionModel) =>
            new QuestionValueAnswer { Multiplier = fieldDefinitionModel.ScaleFactor ?? 1 };

        private static QuestionAnswer CreateMultipleChoiceAnswer(ChoiceSet choiceSet) =>
            new QuestionMultipleChoiceAnswer { Choices = choiceSet.Choices.Select(ch => new QuestionChoice { Id = $"{ch.SurveyChoiceId}", Value = ch.GetDisplayName() }) };
    }
}