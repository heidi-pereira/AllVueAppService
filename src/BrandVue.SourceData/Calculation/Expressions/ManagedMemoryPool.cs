namespace BrandVue.SourceData.Calculation.Expressions;

/// <summary>
/// On very hot code paths, allocations add up very quickly.
/// This provides similar functionality to System.Buffers.MemoryPool (see MSDN)
/// With a standard MemoryPool (or ArrayPool), the caller of Rent must keep track of each bit of memory and return it afterwards.
/// With this managed memory pool, all memory allocated can be reused immediately after calling FreeAll.
/// This means you can call FreeAll at the start of each loop iteration or commonly executed function, and any of the methods called in the loop can allocate memory and pass it around as they please without any further concern.
/// So rather than using X bytes per iteration, you can use X bytes and share them amongst all iterations.
/// </summary>
public class ManagedMemoryPool<T> : IManagedMemoryPool<T>
{
    private const int InitialReservedMemory = 128;
    private T[] _buffer = new T[InitialReservedMemory];
    private int _firstFreeIndex;

    /// <summary>
    /// Be careful not to stash away this memory inside a field that could be accessed after a call to FreeAll
    /// </summary>
    public ManagedMemory Rent(int maxNeeded)
    {
        if (maxNeeded > _buffer.Length - _firstFreeIndex)
        {
            _buffer = new T[Math.Max(maxNeeded, _buffer.Length * 2)];
            _firstFreeIndex = 0;
        }
        var memory = _buffer.AsMemory(_firstFreeIndex, maxNeeded);
        _firstFreeIndex += maxNeeded;
        return new ManagedMemory(this, _firstFreeIndex, memory);
    }

    /// <summary>
    /// Call at the start of each loop iteration or commonly executed function, when you know the previous iteration's rented memory will be no longer read/written
    /// </summary>
    public void FreeAll() => _firstFreeIndex = 0;

    /// <summary>
    /// Nested the ManagedMemory struct so this could stay private since it would be dangerous if misused
    /// </summary>
    private void ReturnLast(int firstFreeIndex, int lengthToReturn)
    {
        if (_firstFreeIndex == firstFreeIndex)
        {
            _firstFreeIndex -= lengthToReturn;
        }
    }

    public readonly struct ManagedMemory
    {
        private readonly ManagedMemoryPool<T> _managedMemoryPool;
        private readonly int _firstFreeIndex;

        /// <summary>
        /// Get Memory object to hold on to this memory until FreeAll is called
        /// </summary>
        public Memory<T> TakeAll { get; }

        public Span<T> Span => TakeAll.Span;

        public static implicit operator Memory<T>(ManagedMemory managedMemory) => managedMemory.TakeAll;

        internal ManagedMemory(ManagedMemoryPool<T> managedMemoryPool, int firstFreeIndex, Memory<T> takeAll)
        {
            _managedMemoryPool = managedMemoryPool;
            _firstFreeIndex = firstFreeIndex;
            TakeAll = takeAll;
        }

        /// <summary>
        /// Call this when you allocated more memory than needed and only need the first n elements
        /// e.g. Rent the max needed for some operation, do the operation, then return the part actually used.
        /// Returns the remaining memory to the pool
        /// </summary>
        public Memory<T> Take(int nElements)
        {
            _managedMemoryPool.ReturnLast(_firstFreeIndex, TakeAll.Length - nElements);
            return TakeAll[..nElements];
        }
    }
}
