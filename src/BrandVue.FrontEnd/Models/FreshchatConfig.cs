namespace BrandVue.Models
{
    public class FreshchatConfig
    {
        public bool Enabled { get; set; }
        public string ApiToken { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserId { get; set; }
        public bool IsTrial { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public string Role { get; set; }
        public string RestoreId { get; set; }
        public string Environment { get; set; }
        public string Company { get; set; }
        public string ProductName { get; set; }
    }
}