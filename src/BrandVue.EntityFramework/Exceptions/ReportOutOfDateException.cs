namespace BrandVue.EntityFramework.Exceptions
{
    public class ReportOutOfDateException : Exception
    {
        public ReportOutOfDateException(string userMessage) : base(userMessage)
        {
        }
    }
}
