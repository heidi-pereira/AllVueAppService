using BrandVue.Controllers.Api;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Breaks;

namespace BrandVue.Services
{
    public interface ISavedBreaksService
    {
        SavedBreaksForSurveyAndUser GetForCurrentSurveyAndUser();
        IEnumerable<SavedBreakCombination> GetAllSavedBreaksForSubProduct();
        SavedBreakCombination GetBreakByName(string name);
        int SaveBreaks(string name, bool isShared, CrossMeasure[] breaks);
        int SaveAudience(SavedBreakCombination audience);
        void UpdateSavedBreak(int savedBreaksId, string name, bool isShared);
        void RemoveSavedBreak(int savedBreaksId);
    }
}
