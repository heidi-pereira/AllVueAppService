namespace BrandVue.Models
{
    public class EntityInstanceConfigurationModel
    {
        public int SurveyChoiceId { get; set; }
        public string EntityTypeIdentifier { get; set; }
        public string DisplayName { get; set; }
        public bool Enabled { get; set; }
        public DateTimeOffset? StartDate { get; set; }
        public string ImageUrl{ get; set; }

        public EntityInstanceConfigurationModel()
        {
            // Default constructor needed for ASP.NET model
        }
    }
}
