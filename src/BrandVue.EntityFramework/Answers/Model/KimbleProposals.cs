using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.Answers.Model
{
    [Table("Proposals", Schema = "kimble")]
    public partial class KimbleProposals
    {
        [Key] public string KimbleProposalId { get; set; }
        public string Name { get; set; }
        public string Team { get; set; }
        public string Account { get; set; }
        public string Audience { get; set; }
        public string Methodology { get; set; }
        public string BusinessUnit { get; set; }
        public string OwnerEmail { get; set; }
        public string EngagementType { get; set; }
        public string EngagementSubType { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }

        public string OtherComments { get; set; }
        public string BUandTeam { get; set; }
        public string AccountOwner { get; set; }
        public string ProposalOwner { get; set; }
        public string EngagementOwner { get; set; }
    }
}