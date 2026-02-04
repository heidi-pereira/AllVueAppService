using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using System.Linq;
using UserManagement.BackEnd.Domain.UserDataPermissions.Entities;
using UserManagement.BackEnd.Models;
using Vue.Common.BrandVueApi;
using Vue.Common.BrandVueApi.Models;
using SurveySegment = UserManagement.BackEnd.Models.SurveySegment;

namespace UserManagement.BackEnd.Services
{
    public interface IQuestionService
    {
        public Task<VariablesAvailable> GetProjectQuestionsAvailable(string authCompany, ProjectIdentifier projectId,
            CancellationToken token);

        public Task<int> GetProjectResponseCountFromFilter(string companyId, ProjectIdentifier projectId,
            List<AllVueFilter> filters, CancellationToken token);

    }

    public class QuestionService : IQuestionService
    {
        private readonly ISurveyGroupService _surveyGroupService;
        private readonly IQuestionRepository _questionRepository;
        private readonly IVariableService _variableService;
        private readonly IBrandVueApiClient _apiClient;


        public QuestionService(ISurveyGroupService surveyGroupService, IQuestionRepository questionRepository, IVariableService variableService, IBrandVueApiClient apiClient)
        {
            _surveyGroupService = surveyGroupService ?? throw new ArgumentNullException(nameof(surveyGroupService));
            _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
            _variableService = variableService ?? throw new ArgumentNullException(nameof(variableService));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        private record VarCode(string AllVue, string Survey);

        private record CountStats(int AnswerCount, IList<ChoiceStat> ChoiceStats);
        private async Task<IDictionary<VarCode, CountStats>> LookupQuestionsCount(ProjectIdentifier projectId, IList<VarCode> varCodes, CancellationToken token)
        {
            var surveyIds = new List<SurveyDescriptor>();
            switch (projectId.Type)
            {
                case ProjectType.AllVueSurvey:
                {
                    surveyIds.Add(new (projectId.Id, []));
                    break;
                }
                case ProjectType.AllVueSurveyGroup:
                {
                    var result = await _surveyGroupService.GetSurveyGroupByIdAsync(projectId.Id, token);
                    if (result != null)
                    {
                        surveyIds = result.Surveys.Select(x => new SurveyDescriptor(x.SurveyId, [])).ToList();
                    }
                    break;
                }
            }
            var varCodeLookup = varCodes.Select(x => x.Survey).ToHashSet();
            var questionCount = await _questionRepository.GetQuestionsWithAnswerCount(varCodes.Select(x=>x.Survey).ToArray(), surveyIds, token);
            var lookupVarCodeToCount = questionCount
                .Where(x => varCodeLookup.Contains(x.Question.VarCode))
                .GroupBy(q => q.Question.VarCode)
                .ToDictionary(
                    g => varCodes.Single(x => x.Survey == g.Key),
                    g => new CountStats(
                        g.Sum(x => x.AnswerCount), 
                        g.SelectMany(x => x.ChoiceStats).ToList())
                );
            return lookupVarCodeToCount;
        }

        private async Task<IEnumerable<MetricConfiguration>> GetMetricsForProject(ProjectIdentifier projectId, CancellationToken token)
        {
            var lookup = _surveyGroupService.GetLookupOfSurveyGroupIdToSafeUrl();
            var legacySubProductId = projectId.ToLegacyAuthName(lookup);
            var legacyProductShortCode = projectId.ToLegacyProductShortCode();
            return await _variableService.GetMetricsForProject(legacyProductShortCode, legacySubProductId, token);
        }

        private async Task<IList<MetricConfiguration>> GetQuestionsForProject(ProjectIdentifier projectId, CancellationToken token)
        {
            var metrics = await GetMetricsForProject(projectId, token);
            return metrics.Where(m => m.VariableConfiguration?.Definition is QuestionVariableDefinition)
                .ToList();
        }

        private VariableOption? ApiChoiceToVariableOption(QuestionChoice choice, IList<ChoiceStat> stats)
        {
            if (int.TryParse(choice.Id, out var id))
            {
                var stat = stats.SingleOrDefault(s => s.ChoiceId == id);
                return new VariableOption(id, choice.Value, stat?.AnswerCount??0);
            }

            return null;
        }

        private List<VariableOption> ApiChoicesToVariableOptions(List<QuestionChoice>? choices, IList<ChoiceStat> stats )
        {
            if (choices == null)
            {
                return new List<VariableOption>();
            }

            var options = new List<VariableOption>();
            foreach (var choice in choices)
            {
                var option = ApiChoiceToVariableOption(choice, stats);
                if (option != null)
                {
                    options.Add(option);
                }
            }
            return options;
        }

        private List<Variable> CreateListOfVariables(List<QuestionWithSurveySets> questions1, 
            IEnumerable<MetricConfiguration> metricQuestions,
            IDictionary<VarCode,CountStats> varCodeToCount)
        {

            var questions = questions1.Join(
                metricQuestions,
                apiQuestion => apiQuestion.QuestionId,
                metricQuestion => metricQuestion.Field,
                (apiQuestion, metricQuestion) =>
                {
                    var varCode = varCodeToCount.SingleOrDefault(x => x.Key.AllVue == metricQuestion.VarCode);
                    return new Variable(
                        metricQuestion.VariableConfigurationId ?? 0,
                        metricQuestion.VarCode,
                        apiQuestion.QuestionText,
                        ApiChoicesToVariableOptions(apiQuestion.AnswerSpec?.Choices, varCode.Value?.ChoiceStats ?? []),
                        apiQuestion.SurveySets.Select(x => new SurveySegment(x.SurveySetId, x.Name)).ToList(),
                        apiQuestion?.AnswerSpec?.AnswerType ?? "Unknown",
                        metricQuestion.CalcType,
                        !metricQuestion.EligibleForCrosstabOrAllVue,
                         varCode.Value?.AnswerCount??0
                    );
                });

            return questions
                .Where(question => question.Id > 0)
                .OrderBy(question => question.Name)
                .ToList();
        }

        public async Task<VariablesAvailable> GetProjectQuestionsAvailable(string authCompany,ProjectIdentifier projectId,
            CancellationToken token)
        {
            var lookup = _surveyGroupService.GetLookupOfSurveyGroupIdToSafeUrl();
            var apiQuestions = await _apiClient.GetProjectQuestionsAvailableAsync(authCompany, projectId.ToLegacyProductShortCode(),
                projectId.ToLegacyAuthName(lookup), token);

            if (!apiQuestions.UnionOfQuestions.Any())
                return new VariablesAvailable(new List<SurveySegment>(), new List<Variable>());

            var metricQuestions = await GetQuestionsForProject(projectId, token);

            if (!metricQuestions.Any())
                return new VariablesAvailable(new List<SurveySegment>(), new List<Variable>());

            var lookupVarCodesToCount = await LookupQuestionsCount(projectId, metricQuestions.Select(x =>
                {
                    if (x.VariableConfiguration?.Definition is QuestionVariableDefinition config)
                    {
                        return new VarCode(x.VarCode, config.QuestionVarCode);
                    }
                    return new VarCode(x.VarCode, x.VarCode);
                }
                ).ToList(), token);
            return new VariablesAvailable(apiQuestions.SurveySets.Select(s => new SurveySegment(s.SurveySetId, s.Name)).ToList(),
                CreateListOfVariables(apiQuestions.UnionOfQuestions, metricQuestions, lookupVarCodesToCount));
        }

        public async Task<int> GetProjectResponseCountFromFilter(string companyId, ProjectIdentifier projectId,
            List<AllVueFilter> filters, CancellationToken token)
        {
            var surveyIds = new List<SurveyDescriptor>();
            switch (projectId.Type)
            {
                case ProjectType.AllVueSurvey:
                {
                    surveyIds.Add(new(projectId.Id, []));
                    break;
                }
                case ProjectType.AllVueSurveyGroup:
                {
                    var result = await _surveyGroupService.GetSurveyGroupByIdAsync(projectId.Id, token);
                    if (result != null)
                    {
                        surveyIds = result.Surveys.Select(x => new SurveyDescriptor(x.SurveyId, [])).ToList();
                    }
                    break;
                }
            }
            var metricQuestions = await GetQuestionsForProject(projectId, token);
            var questionsToFilterBy = filters
                .Where(filter=> metricQuestions.Any(m=> m.VariableConfigurationId == filter.VariableConfigurationId))
                .Select(filter =>
                {
                    var metricConfiguration =
                        metricQuestions.Single(m => m.VariableConfigurationId == filter.VariableConfigurationId);
                        var varCode = metricConfiguration.VarCode;
                    if (metricConfiguration.VariableConfiguration?.Definition is QuestionVariableDefinition config)
                    {
                        varCode = config.QuestionVarCode;
                    }
                    return new FilterVarCode(varCode, surveyIds.ToArray(), filter.EntitySetId, filter.EntityIds);
                });
            var questionFilters = questionsToFilterBy.Select(x => (FilterVarCode)x).ToList();
            return (await _questionRepository.GetRespondentsWithFilter(questionFilters, token)).Count;
        }
    }
}
