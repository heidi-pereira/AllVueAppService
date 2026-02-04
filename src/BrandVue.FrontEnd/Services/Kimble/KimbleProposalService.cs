using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;

namespace BrandVue.Services.Kimble
{
    public class KimbleProposalService 
    {
        private readonly IProductContext _productContext;
        private readonly IKimbleRepository _repository;

        public KimbleProposalService(IProductContext productContext, IKimbleRepository repository)
        {
            _productContext = productContext;
            _repository = repository;
        }

        public KimbleProposal GetProposal()
        {
            if (!string.IsNullOrWhiteSpace(_productContext.KimbleProposalId))
            {
                return _repository.GetProposal(_productContext.KimbleProposalId);
            }
            return null;
        }
    }
}
