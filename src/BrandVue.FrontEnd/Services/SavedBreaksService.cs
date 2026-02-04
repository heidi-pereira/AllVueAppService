using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Breaks;

namespace BrandVue.Services
{
    public class SavedBreaksService : ISavedBreaksService
    {
        private readonly ISavedBreaksRepository _savedBreaksRepository;
        private readonly IUserContext _userContext;
        private readonly IProductContext _productContext;

        public SavedBreaksService(
            ISavedBreaksRepository savedBreaksRepository,
            IUserContext userInformationProvider,
            IProductContext productContext
        )
        {
            _savedBreaksRepository = savedBreaksRepository;
            _userContext = userInformationProvider;
            _productContext = productContext;
        }

        public SavedBreaksForSurveyAndUser GetForCurrentSurveyAndUser()
        {
            return new SavedBreaksForSurveyAndUser
            {
                SavedBreaks = _savedBreaksRepository.GetForCurrentUser(),
            };
        }

        public IEnumerable<SavedBreakCombination> GetAllSavedBreaksForSubProduct()
        {
            return _savedBreaksRepository.GetAllForSubProduct();
        }

        public SavedBreakCombination GetBreakByName(string name)
        {
            return _savedBreaksRepository.GetBreakByName(name.Trim()) ?? new SavedBreakCombination();
        }

        public int SaveBreaks(string name, bool isShared, CrossMeasure[] breaks)
        {
            if (IsDuplicatedName(name, null))
            {
                throw new BadRequestException("Name already exists.");
            }

            var savedBreaks = new SavedBreakCombination
            {
                Name = name,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                IsShared = isShared,
                CreatedByUserId = _userContext.UserId,
                Breaks = breaks.ToList(),
            };

            _savedBreaksRepository.Create(savedBreaks);
            return savedBreaks.Id;
        }

        public int SaveAudience(SavedBreakCombination audience)
        {
            if (audience.Id <= 0)
            {
                _savedBreaksRepository.Create(audience);
            } else
            {
                _savedBreaksRepository.Update(audience);
            }
            return audience.Id;
        }

        public void UpdateSavedBreak(int savedBreaksId, string name, bool isShared)
        {
            var savedBreaks = _savedBreaksRepository.GetById(savedBreaksId);
            if (!_userContext.IsAdministrator)
            {
                if (savedBreaks.CreatedByUserId != _userContext.UserId)
                {
                    throw new BadRequestException("Only administrators can update someone else saved breaks");
                }
            }

            if (IsDuplicatedName(name, savedBreaksId))
            {
               throw new BadRequestException("Name already exists.");
            }

            savedBreaks.Name = name;
            savedBreaks.IsShared = isShared;
            _savedBreaksRepository.Update(savedBreaks);
        }

        public void RemoveSavedBreak(int savedBreaksId)
        {
            if (!_userContext.IsAdministrator)
            {
                var breaks = _savedBreaksRepository.GetById(savedBreaksId);
                var isThisUsersBreaks = breaks.CreatedByUserId == _userContext.UserId;

                if (!isThisUsersBreaks)
                {
                    throw new BadRequestException("Only administrators can remove saved breaks other than your own");
                }
            }

            _savedBreaksRepository.Delete(savedBreaksId);
        }

        private bool IsDuplicatedName(string name, int? savedBreaksId)
        {
            var breakRecord = _savedBreaksRepository.GetBreakByName(name.Trim());

            if (savedBreaksId == null)
            {
               return breakRecord != null;
            } 
            else
            {
                return breakRecord != null && breakRecord.Id != savedBreaksId;
            }
        }
    }
}
