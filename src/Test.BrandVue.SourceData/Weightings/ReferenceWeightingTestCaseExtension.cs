using BrandVue.EntityFramework;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test.BrandVue.SourceData.Weightings
{
    internal static class ReferenceWeightingTestCaseExtension
    {
        private static int _profileRepsonseId = 0;

        internal static (ProfileResponseAccessor accessor, IGroupedQuotaCells groupedQuotaCells) SetupProfileDistrubution(this ReferenceWeightingTestCase testCase)
        {
            var subset = GetSubset(testCase.SubsetName);
            var surveyId = testCase.SurveyId;

            var entityTypeRepository = new EntityTypeRepository();

            var definitionModels = new List<FieldDefinitionModel>();
            foreach (var item in testCase.Descriptors)
            {
                var responseType = new EntityType(item.QuestionName, item.SingleName, item.PluralName);
                definitionModels.Add(CreateFieldDefinitionModel(item.QuestionName, new EntityFieldDefinitionModel(item.QuestionName, responseType, responseType.Identifier).Yield()));
                entityTypeRepository.TryAdd(item.TypeName, responseType);
            }

            var responseFieldManager = new ResponseFieldManager(entityTypeRepository);
            responseFieldManager.Load(definitionModels.Select(model => (subset.Id, model)).ToArray());

            var definitionModelsArray = definitionModels.ToArray();
            int quotaCellId = QuotaCell.UnweightedQuotaCell(subset).Id + 1;
            var cellResponses = new List<CellResponse>();
            const int AnswerOK = 1;
            var quotaCells = new List<QuotaCell>();
            foreach (var data in testCase.DataDistribution)
            {
                if (data.NumberOfResponses > 0)
                {
                    var cellByString = data.QuotaCellAsString;
                    var cellByParts = cellByString.Split(':');
                    var dictionaryOfCells = new Dictionary<string, int>();
                    for (int fieldIndex = 0; fieldIndex < cellByParts.Length; fieldIndex++)
                    {
                        dictionaryOfCells[definitionModelsArray[fieldIndex].Name] = int.Parse(cellByParts[fieldIndex]);
                    }
                    var quotaCell = new QuotaCell(quotaCellId++, subset, dictionaryOfCells);
                    quotaCells.Add(quotaCell);
                    cellResponses.AddRange(CreateCellResponse(surveyId, responseFieldManager, AnswerOK, quotaCell, data.NumberOfResponses));
                }
            }

            var accessor = new ProfileResponseAccessor(cellResponses, subset);
            var groupedQuotaCells = GroupedQuotaCells.CreateUnfiltered(quotaCells);

            return (accessor, groupedQuotaCells);
        }
        private static FieldDefinitionModel CreateFieldDefinitionModel(string fieldName, IEnumerable<EntityFieldDefinitionModel> entityFieldDefinitionModels)
        {
            return new FieldDefinitionModel(fieldName, string.Empty, string.Empty, string.Empty, string.Empty, null, string.Empty,
                EntityInstanceColumnLocation.Unknown, string.Empty, false, null, entityFieldDefinitionModels, null);
        }

        private static Subset GetSubset(string id)
        {
            return new Subset
            {
                Id = id,
                DisplayName = id,
                Iso2LetterCountryCode = "gb",
                Alias = id,
                EnableRawDataApiAccess = true,
                ProductId = 1
            };
        }
        
        private static IEnumerable<CellResponse> CreateCellResponse(int surveyId, ResponseFieldManager responseFieldManager, int AnswerOK, QuotaCell forCell, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var myProfile = new ProfileResponseEntity(_profileRepsonseId++, DateTimeOffset.UtcNow, surveyId);

                foreach (var item in forCell.FieldGroupToKeyPart)
                {
                    var field = responseFieldManager.Get(item.Key);

                    field.EnsureLoadOrderIndexInitialized_ThreadUnsafe();
                    myProfile.AddFieldValue(field, EntityIds.From(new[] {
                            new EntityValue(field.EntityCombination.Single(), int.Parse(item.Value)) }), AnswerOK, forCell.Subset);
                }

                yield return new CellResponse(myProfile, forCell);

            }
        }
    }
}
