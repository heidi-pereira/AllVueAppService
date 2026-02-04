namespace BrandVue.EntityFramework.Exceptions;

public class TooBusyException : Exception
{
    public TooBusyException(string userMessage) : base(userMessage)
    {
    }
}