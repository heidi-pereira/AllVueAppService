using System.ComponentModel.DataAnnotations;

namespace OpenEnds.BackEnd.Model
{
    public class SurveyResponse
    {
        [Key]
        public int ResponseId { get; set; }
        public int SurveyId { get; set; }
        public int SegmentId { get; set; }
        public int Status { get; set; }
        public int RespondentId { get; set; }
        public Boolean Archived { get; set; }
    }
}
