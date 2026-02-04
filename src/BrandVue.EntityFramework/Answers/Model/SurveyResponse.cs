using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.Answers.Model
{
    [Table("SurveyResponse", Schema = "dbo")]
    public partial class SurveyResponse
    {
        [Column("surveyId")]
        public int SurveyId { get; set; }
        [Key]
        [Column("responseId")]
        public int ResponseId { get; set; }
        [Column("timestamp", TypeName = "datetime")]
        public DateTime Timestamp { get; set; }
        [Column("segmentId")]
        public int SegmentId { get; set; }
        [Column("archived")]
        public bool Archived { get; set; }
        [Column("status")]
        public SurveyCompletionStatus Status { get; set; }
        [Column("lastChangeTime", TypeName = "datetime")]
        public DateTime LastChangeTime { get; set; }

    }

    public enum SurveyCompletionStatus
    {
        Not_Started = 0,
        Entered = 1,
        In_Progress = 2,
        Past_Checkpoint = 3,
        Screened_Out = 4,
        Quota_Out = 5,
        Completed = 6,
        EmailBounce_Undeliverable = 7,
        EmailBounce_OutOfOffice = 8,
        Unsubscribed = 9,
        EnteredLandingPage = 10,
        EmailBounce_SMTPAuthentication = 11,
        EmailBounce_SMTPConnection = 12,
        EmailBounce_SMTPProtocol = 13,
        Archived = 14,
        Num = 15,
        RelevantIdFail = 16,
        WithinExclusionPeriod = 17,
    }
}