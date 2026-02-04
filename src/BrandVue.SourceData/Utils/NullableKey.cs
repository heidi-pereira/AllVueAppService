namespace BrandVue.SourceData.Utils
{
    /// <summary>
    /// Wrap a ToDictionary key selector in this if you want to use null as a key.
    /// Warning: Do not use default(NullableKey) or new NullableKey() with no parameters
    /// </summary>
    public readonly struct NullableKey<T> : IEquatable<NullableKey<T>>
    {
#pragma warning disable CS0414 // Intentionally non zero field so that this struct doesn't compare equal to null, hence can be used in dictionaries
        private readonly bool _initializedCorrectly;
#pragma warning restore CS0414
        private readonly T _nullable;
        public NullableKey(T nullable)
        {
            _initializedCorrectly = true;
            _nullable = nullable;
        }

        public static implicit operator T(NullableKey<T> wrapped) => wrapped._nullable;
        public static implicit operator NullableKey<T>(T wrapped) => new(wrapped);

        public bool Equals(NullableKey<T> other) => EqualityComparer<T>.Default.Equals(_nullable, other._nullable);

        public override bool Equals(object obj) => obj is NullableKey<T> other && Equals(other);

        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(_nullable);
    }
}