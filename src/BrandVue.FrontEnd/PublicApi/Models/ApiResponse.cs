namespace BrandVue.PublicApi.Models
{
    public class ApiResponse<T>
    {
        public T Value { get; }

        public ApiResponse(T value)
        {
            Value = value;
        }
    }
}
