using Microsoft.AspNetCore.Mvc;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.Services.Kimble;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/meta")]
    public class KimbleProposalController : ApiController
    {
        private readonly IUserContext _userContext;
        private IProductContext _productContext;
        private IKimbleRepository _kimbleRepository;

        public KimbleProposalController(
            IUserContext userContext, IProductContext productContext,
            IKimbleRepository factory)
        {
            _userContext = userContext;
            _productContext = productContext;
            _kimbleRepository = factory;
        }

        [HttpGet]
        [Route("kimbleproposal")]
        public KimbleProposal GetKimbleProposal()
        {
            if (_userContext.IsAuthorizedSavantaUser)
            {
                var service = new KimbleProposalService(_productContext, _kimbleRepository);
                return service.GetProposal();
            }
            return null;
        }
    }
}
