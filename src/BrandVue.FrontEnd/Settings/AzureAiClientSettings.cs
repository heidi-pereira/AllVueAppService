namespace BrandVue
{
    public class AzureAiClientSettings()
    {

        public float Temperature { get; set; }
        public int MaxRetries { get; set; }
        public int DefaultTimeout { get; set; } 
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string Deployment { get; set; }
    }
}