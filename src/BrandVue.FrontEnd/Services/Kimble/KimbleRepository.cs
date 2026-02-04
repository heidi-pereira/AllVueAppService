using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.AnswersMetadata;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.Answers.Model
{
    public record KimbleProposal(
        string KimbleProposalId,
        string Name,
        string Team,
        string Account,
        string Audience,
        string Methodology,
        string BusinessUnit,
        string OwnerEmail,
        string EngagementType,
        string EngagementSubType,
        string Summary,
        string Description,
        string OtherComments,
        string BUandTeam,
        string AccountOwner,
        string ProposalOwner,
        string EngagementOwner);

    public interface IKimbleRepository
    {
        KimbleProposal GetProposal(string kimbleProposalId);
    }

    public class KimbleRepository : IKimbleRepository
    {
        private readonly IAnswerDbContextFactory _dbContextFactory;

        public KimbleRepository(IAnswerDbContextFactory responseDataContextFactory)
        {
            _dbContextFactory = responseDataContextFactory;
        }

        public KimbleProposal GetProposal(string kimbleProposalId)
        {
            if (!string.IsNullOrWhiteSpace(kimbleProposalId))
            {
                using var dbContext = _dbContextFactory.CreateDbContext();

                var proposal = dbContext.KimbleProposals.SingleOrDefault(x => x.KimbleProposalId == kimbleProposalId);
                if (proposal != null)
                {
                    return new KimbleProposal(proposal.KimbleProposalId,
                        proposal.Name,
                        proposal.Team,
                        proposal.Account,
                        proposal.Audience,
                        proposal.Methodology,
                        proposal.BusinessUnit,
                        proposal.OwnerEmail,
                        proposal.EngagementType,
                        proposal.EngagementSubType,
                        proposal.Summary,
                        proposal.Description,
                        proposal.OtherComments,
                        proposal.BUandTeam,
                        proposal.AccountOwner,
                        proposal.ProposalOwner,
                        proposal.EngagementOwner);
                }
            }

            return null;
        }
    }
}

