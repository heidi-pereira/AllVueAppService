namespace BrandVue.SourceData.Calculation.Expressions
{
    /// <remarks>PERF: Careful of using this interface in inner loops except via generics, or you'll inevitably be allocating memory for the boxed struct, and devirtualizing for the method call</remarks>
    public interface IReducer
    {
        public bool IsConstant { get; }
    }

    /// <remarks>PERF: Careful of using this interface in inner loops except via generics, or you'll inevitably be allocating memory for the boxed struct, and devirtualizing for the method call</remarks>
    public interface IReducer<out TValue> : IReducer
    {
        public TValue Value { get; }
    }

    /// <remarks>PERF: Careful of using this interface in inner loops except via generics, or you'll inevitably be allocating memory for the boxed struct, and devirtualizing for the method call</remarks>
    public interface ICurriedReducer : IReducer
    {
        public bool IsConstantFunc { get; }
    }
}