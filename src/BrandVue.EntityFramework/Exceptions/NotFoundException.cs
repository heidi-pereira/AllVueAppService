namespace BrandVue.EntityFramework.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string userMessage) : base(userMessage)
        {
        }
    }
}
