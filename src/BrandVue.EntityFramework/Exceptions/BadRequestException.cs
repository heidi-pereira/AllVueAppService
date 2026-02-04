namespace BrandVue.EntityFramework.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string userMessage) : base(userMessage)
        {
        }
    }
}
