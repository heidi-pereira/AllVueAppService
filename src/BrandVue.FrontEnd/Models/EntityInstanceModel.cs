namespace BrandVue.Models
{
    public class EntityInstanceModel
    {
        public int Id { get; set; }
        public int SurveyChoiceId { get; set; }
        public string Identifier { get; set; }
        public string DisplayName { get; set; }
        public bool Enabled { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public string ImageUrl { get; set; }
    }
}
