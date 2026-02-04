using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vue.Common.Auth.Permissions;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/savedbreaks/[action]")]
    public class SavedBreaksController : ApiController
    {
        private readonly ISavedBreaksService _savedBreaksService;

        public SavedBreaksController(ISavedBreaksService savedBreaksService)
        {
            _savedBreaksService = savedBreaksService;
        }

        [HttpGet]
        public SavedBreaksForSurveyAndUser GetSavedBreaks()
        {
            return _savedBreaksService.GetForCurrentSurveyAndUser();
        }

        [HttpGet]
        public IEnumerable<SavedBreakCombination> GetAllSavedBreaksForSubproduct()
        {
            return _savedBreaksService.GetAllSavedBreaksForSubProduct();
        }

        [HttpGet]
        public SavedBreakCombination GetBreakForSubproduct(string name)
        {
            return _savedBreaksService.GetBreakByName(name);
        }

        [HttpPost]
        [Authorize(Policy = nameof(PermissionFeaturesOptions.BreaksAdd))]
        public int SaveBreaks(string name, bool isShared, [FromBody] CrossMeasure[] breaks)
        {
            return _savedBreaksService.SaveBreaks(name, isShared, breaks);
        }

        [HttpPost]
        [Authorize(Policy = nameof(PermissionFeaturesOptions.BreaksEdit))]
        public void UpdateSaveBreaks(int savedBreaksId, string name, bool isShared)
        {
            _savedBreaksService.UpdateSavedBreak(savedBreaksId, name, isShared);
        }

        [HttpPost]
        [Authorize(Policy = nameof(PermissionFeaturesOptions.BreaksDelete))]
        public void RemoveSavedBreaks(int savedBreaksId)
        {
            _savedBreaksService.RemoveSavedBreak(savedBreaksId);
        }

        [HttpPost]
        public int SaveAudience([FromBody] SavedBreakCombination audience)
        {
            return _savedBreaksService.SaveAudience(audience);
        }
    }
}
