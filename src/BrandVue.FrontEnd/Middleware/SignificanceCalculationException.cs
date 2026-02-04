namespace BrandVue.Middleware
{
    public class SignificanceCalculationException: Exception
    {
        public SignificanceCalculationException()
        {
        }

        public SignificanceCalculationException(string message) : base(message)
        {
        }
    }
}
