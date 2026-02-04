namespace BrandVue.PublicApi.Models
{
    public class ErrorApiResponse
    {
        public string Message { get; }

        public ErrorDetails Error { get; set; }

        public ErrorApiResponse(string message)
        {
            Message = message;
        }
    }

    public class ErrorDetails
    {
        public string Message { get; set; }

        public string StackTrace { get; set; }
    }
}