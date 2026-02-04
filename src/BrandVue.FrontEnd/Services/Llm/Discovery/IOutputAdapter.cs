namespace BrandVue.Services.Llm.Discovery
{
    public interface IOutputAdapter<out T>
    {
        IEnumerable<T> CreateFromFunctionCalls(IEnumerable<IDiscoveryFunctionToolInvocation> toolInvocations);
    }
}