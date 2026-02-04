namespace BrandVue.SourceData.Calculation.Expressions;

public interface IManagedMemoryPool<T>
{
    public ManagedMemoryPool<T>.ManagedMemory Rent(int maxNeeded);
}