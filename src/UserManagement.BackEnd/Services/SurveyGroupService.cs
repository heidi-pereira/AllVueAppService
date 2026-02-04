using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using Microsoft.EntityFrameworkCore;

namespace UserManagement.BackEnd.Services
{
    public class SurveyGroupService : ISurveyGroupService
    {
        private readonly AnswersDbContext _answersDbContext;
        private readonly Lazy<Dictionary<int, string>> _lookup;
        private readonly Lazy<Dictionary<string, int>> _reverselookup;
        private readonly Lazy<List<SurveyGroup>> _surveyGroupData;

        public SurveyGroupService(
            AnswersDbContext answersDbContext)
        {
            _answersDbContext = answersDbContext ?? throw new ArgumentNullException(nameof(answersDbContext));
            _surveyGroupData = new Lazy<List<SurveyGroup>>(() =>
            {
                var validSurveyGroups = new[] { SurveyGroupType.AllVue, SurveyGroupType.BrandVue };
                var groups = _answersDbContext.SurveyGroups.AsNoTracking()
                    .Where(x => validSurveyGroups.Contains(x.Type))
                    .ToList();
                return groups;
            });
            _lookup = new Lazy<Dictionary<int, string>>(() =>
            {
                return _surveyGroupData.Value.ToDictionary(x => x.SurveyGroupId, x => x.UrlSafeName);
            });
            _reverselookup = new Lazy<Dictionary<string, int>>(() =>
            {
                return _surveyGroupData.Value.ToDictionary(x => x.UrlSafeName, x => x.SurveyGroupId);
            });
        }

        private IQueryable<SurveyGroup> GetSurveyGroups()
        {
            return _answersDbContext.SurveyGroups.AsNoTracking()
                .Include(g => g.Surveys)
                .ThenInclude(s => s.Survey);
        }

        public IEnumerable<SurveyGroup> GetSurveyGroupsForCompanies(List<string> companyIds)
        {
            return GetSurveyGroups()
                .Where(g => g.Type == SurveyGroupType.AllVue)
                .Where(g => g.Surveys.All(s => companyIds.Contains(s.Survey.AuthCompanyId))).ToList();
        }

        public IList<SurveyGroup> GetBrandVueSurveyGroups()
        {
            return GetSurveyGroups()
                .Where(g => g.Type == SurveyGroupType.BrandVue).ToList();

        }

        public async Task<SurveyGroup?> GetSurveyGroupByIdAsync(int surveyGroupId, CancellationToken token)
        {
            return await GetSurveyGroups()
                .SingleOrDefaultAsync(g => g.SurveyGroupId == surveyGroupId, token);
        }

        public async Task<IList<SurveySharedOwner>> GetSharedSurveysByIds(int[] surveyIds)
        {
            return await _answersDbContext.SurveySharedOwners.AsNoTracking()
                .Where(sso => surveyIds.Contains(sso.SurveyId))
                .ToListAsync();
        }

        public IDictionary<int, string> GetLookupOfSurveyGroupIdToSafeUrl()
        {
            return _lookup.Value;
        }

        public bool TryParse(string projectName, out int surveyGroupId)
        {
            return _reverselookup.Value.TryGetValue(projectName, out surveyGroupId);
        }
    }
}
